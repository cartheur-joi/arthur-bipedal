using System.Text;
using System.Runtime.InteropServices;

#if USBCAMERA_WPF
using System.Windows;               // Size
using System.Windows.Media;         // PixelFormats
using System.Windows.Media.Imaging; // BitmapSource
using Bitmap = System.Windows.Media.Imaging.BitmapSource;
#else
#endif

namespace DynamixelWizard.Controls
{
    // [How to use]
    // string[] devices = UsbCamera.FindDevices();
    // if (devices.Length == 0) return; // no camera.
    //
    // check format.
    // int cameraIndex = 0;
    // UsbCamera.VideoFormat[] formats = UsbCamera.GetVideoFormat(cameraIndex);
    // for(int i=0; i<formats.Length; i++) Console.WriteLine("{0}:{1}", i, formats[i]);
    //
    // create usb camera and start.
    // var camera = new UsbCamera(cameraIndex, formats[0]);
    // camera.Start();
    //
    // get image.
    // Immediately after starting the USB camera,
    // GetBitmap() fails because image buffer is not prepared yet.
    // var bmp = camera.GetBitmap();
    //
    // adjust properties.
    // UsbCamera.PropertyItems.Property prop;
    // prop = camera.Properties[DirectShow.CameraControlProperty.Exposure];
    // if (prop.Available)
    // {
    //     prop.SetValue(DirectShow.CameraControlFlags.Manual, prop.Default);
    // }
    // 
    // prop = camera.Properties[DirectShow.VideoProcAmpProperty.WhiteBalance];
    // if (prop.Available && prop.CanAuto)
    // {
    //     prop.SetValue(DirectShow.CameraControlFlags.Auto, 0);
    // }

    // [Note]
    // By default, GetBitmap() returns image of System.Drawing.Bitmap.
    // If WPF, define 'USBCAMERA_WPF' symbol that makes GetBitmap() returns image of BitmapSource.

    public class UsbCamera
    {
        public Size Size { get; private set; }
        public Action Start { get; private set; }
        public Action Stop { get; private set; }
        public void Release()
        {
            Releasing();
            Released();
        }

        private Action Releasing;
        private Action Released;

        /// <summary>Get image.</summary>
        /// <remarks>Immediately after starting, fails because image buffer is not prepared yet.</remarks>
        public Func<Bitmap> GetBitmap { get; private set; }

        /// <summary>
        /// camera support still image or not.
        /// some cameras can produce a still image and often higher quality than capture stream.
        /// </summary>
        public bool StillImageAvailable { get; private set; }

        public Action StillImageTrigger { get; private set; } = () => { }; // null guard.

        /// <summary>called when still image captured by hardware button or software trigger.</summary>
        public Action<Bitmap> StillImageCaptured
        {
            get { return StillSampleGrabberCallback.Buffered; }
            set { StillSampleGrabberCallback.Buffered = value; }
        }

        private SampleGrabberCallback StillSampleGrabberCallback;

        private Action<IntPtr, Size> SetPreviewControlMain;

        public void SetPreviewControl(IntPtr handle, Size size) { SetPreviewControlMain(handle, size); }
        public void SetPreviewSize(Size size) { SetPreviewControlMain(IntPtr.Zero, size); }

        /// <summary>
        /// Get available USB camera list.
        /// </summary>
        /// <returns>Array of camera name, or if no device found, zero length array.</returns>
        public static string[] FindDevices()
        {
            return DirectShow.GetFiltes(DirectShow.DsGuid.CLSID_VideoInputDeviceCategory).ToArray();
        }

        /// <summary>
        /// Get video formats.
        /// </summary>
        public static VideoFormat[] GetVideoFormat(int cameraIndex)
        {
            var filter = DirectShow.CreateFilter(DirectShow.DsGuid.CLSID_VideoInputDeviceCategory, cameraIndex);
            var pin = DirectShow.FindPin(filter, 0, DirectShow.PIN_DIRECTION.PINDIR_OUTPUT);
            return GetVideoOutputFormat(pin);
        }

        /// <summary>
        /// Create USB Camera. If device do not support the size, default size will applied.
        /// </summary>
        /// <param name="cameraIndex">Camera index in FindDevices() result.</param>
        /// <param name="size">
        /// Size you want to create. Normally use Size property of VideoFormat in GetVideoFormat() result.
        /// </param>
        public UsbCamera(int cameraIndex, Size size) : this(cameraIndex, new VideoFormat() { Size = size })
        {
        }

        /// <summary>
        /// Create USB Camera. If device do not support the format, default format will applied.
        /// </summary>
        /// <param name="cameraIndex">Camera index in FindDevices() result.</param>
        /// <param name="format">
        /// Normally use GetVideoFormat() result.
        /// You can change TimePerFrame value from Caps.MinFrameInterval to Caps.MaxFrameInterval.
        /// TimePerFrame = 10,000,000 / frame duration. (ex: 333333 in case 30fps).
        /// You can change Size value in case Caps.MaxOutputSize > Caps.MinOutputSize and OutputGranularityX/Y is not zero.
        /// Size = any value from Caps.MinOutputSize to Caps.MaxOutputSize step with OutputGranularityX/Y.
        /// </param>
        public UsbCamera(int cameraIndex, VideoFormat format)
        {
            var camera_list = FindDevices();
            if (cameraIndex >= camera_list.Length) throw new ArgumentException("USB camera is not available.", "cameraIndex");
            Init(cameraIndex, format);
        }

        private void Init(int index, VideoFormat format)
        {
            //----------------------------------
            // Create Filter Graph
            //----------------------------------

            var graph = DirectShow.CreateGraph();
            var builder = DirectShow.CoCreateInstance(DirectShow.DsGuid.CLSID_CaptureGraphBuilder2) as DirectShow.ICaptureGraphBuilder2;
            builder.SetFiltergraph(graph);

            //----------------------------------
            // VideoCaptureSource
            //----------------------------------
            var vcap_source = CreateVideoCaptureSource(index, format);
            graph.AddFilter(vcap_source, "VideoCapture");

            // PIN_CATEGORY_CAPTURE
            //
            // [Video Capture Source]
            // +--------------------+  +----------------+  +---------------+
            // |         capture pin|→| Sample Grabber |→| Null Renderer |
            // +--------------------+  +----------------+  +---------------+
            //                                 ↓GetBitmap()
            {
                var sample = ConnectSampleGrabberAndRenderer(graph, builder, vcap_source, DirectShow.DsGuid.PIN_CATEGORY_CAPTURE);
                if (sample != null)
                {
                    // release when finish.
                    Released += () => { var i_grabber = sample.Grabber; DirectShow.ReleaseInstance(ref i_grabber); };

                    Size = new Size(sample.Width, sample.Height);

                    // fix screen tearing problem(issue #2)
                    // you can use previous method if you swap the comment line below.
                    // GetBitmap = () => GetBitmapFromSampleGrabberBuffer(sample.Grabber, sample.Width, sample.Height, sample.Stride);
                    GetBitmap = GetBitmapFromSampleGrabberCallback(sample.Grabber, sample.Width, sample.Height, sample.Stride);
                }
            }

            // PIN_CATEGORY_STILL
            //
            // [Video Capture Source]
            // +--------------------+  +----------------+  +---------------+
            // |           still pin|→| Sample Grabber |→| Null Renderer |
            // |                    |  +----------------+  +---------------+
            // |                    |  +----------------+  +---------------+
            // |         capture pin|→| Sample Grabber |→| Null Renderer |
            // +--------------------+  +----------------+  +---------------+
            //                                 ↓GetBitmap()
            {
                // support still image (issue #16)
                // https://learn.microsoft.com/en-us/windows/win32/directshow/capturing-an-image-from-a-still-image-pin
                // Some cameras can produce a still image separate from the capture stream,
                // and often the still image is of higher quality than the images produced by the capture stream.
                // The camera may have a button that acts as a hardware trigger, or it may support software triggering.
                // A camera that supports still images will expose a still image pin, which is pin category PIN_CATEGORY_STILL.
                var sample = ConnectSampleGrabberAndRenderer(graph, builder, vcap_source, DirectShow.DsGuid.PIN_CATEGORY_STILL);
                if (sample != null)
                {
                    // release when finish.
                    Released += () => { var i_grabber = sample.Grabber; DirectShow.ReleaseInstance(ref i_grabber); };

                    var still_pin = DirectShow.FindPin(vcap_source, 0, DirectShow.PIN_DIRECTION.PINDIR_OUTPUT, DirectShow.DsGuid.PIN_CATEGORY_STILL);
                    var video_con = vcap_source as DirectShow.IAMVideoControl;
                    if (video_con != null)
                    {
                        StillImageAvailable = true;

                        //var dummp = GetBitmapFromSampleGrabberCallback(grabber.Sampler, grabber.Width, grabber.Height, grabber.Stride);
                        StillSampleGrabberCallback = new SampleGrabberCallback(sample.Width, sample.Height, sample.Stride);
                        sample.Grabber.SetCallback(StillSampleGrabberCallback, 1); // WhichMethodToCallback = BufferCB

                        // To trigger the still pin, use the IAMVideoControl::SetMode method when the graph is running, as follows:
                        StillImageTrigger = () =>
                        {
                            video_con.SetMode(still_pin, DirectShow.VideoControlFlags.Trigger | DirectShow.VideoControlFlags.ExternalTriggerEnable);
                        };
                    }
                }
            }

            // PIN_CATEGORY_PREVIEW
            //
            // [Video Capture Source]
            // +--------------------+  +----------------+
            // |         preview pin|→| Video Renderer |
            // |                    |  +----------------+
            // |                    |  +----------------+  +---------------+
            // |           still pin|→| Sample Grabber |→| Null Renderer |
            // |                    |  +----------------+  +---------------+
            // |                    |  +----------------+  +---------------+
            // |         capture pin|→| Sample Grabber |→| Null Renderer |
            // +--------------------+  +----------------+  +---------------+
            //                                 ↓GetBitmap()
            var setPreviewHandle = IntPtr.Zero;
            SetPreviewControlMain = (controlHandle, clientSize) =>
            {
                var vw = graph as DirectShow.IVideoWindow;
                if (vw == null) return;

                if (setPreviewHandle == IntPtr.Zero)
                {
                    setPreviewHandle = controlHandle;
                    var pinCategory = DirectShow.DsGuid.PIN_CATEGORY_PREVIEW;
                    var mediaType = DirectShow.DsGuid.MEDIATYPE_Video;
                    builder.RenderStream(ref pinCategory, ref mediaType, vcap_source, null, null);
                    vw.put_Owner(controlHandle);
                }

                // calc window size and position with keep aspect.
                var w = clientSize.Width;
                var h = Size.Height * w / Size.Width;
                if (h > clientSize.Height)
                {
                    h = clientSize.Height;
                    w = Size.Width * h / Size.Height;
                }
                var x = (clientSize.Width - w) / 2;
                var y = (clientSize.Height - h) / 2;

                // set window owner.
                const int WS_CHILD = 0x40000000; // cannot have a menu bar. 
                const int WS_CLIPSIBLINGS = 0x04000000; // clips child windows relative to each other when receives a WM_PAINT.
                vw.put_WindowStyle(WS_CHILD | WS_CLIPSIBLINGS);
                vw.SetWindowPosition((int)x, (int)y, (int)w, (int)h);
            };

            // Start, Stop, Release, the filter graph.
            Start = () => DirectShow.PlayGraph(graph, DirectShow.FILTER_STATE.Running);
            Stop = () => DirectShow.PlayGraph(graph, DirectShow.FILTER_STATE.Stopped);
            Releasing += () => Stop();
            Released += () =>
            {
                DirectShow.ReleaseInstance(ref builder);
                DirectShow.ReleaseInstance(ref graph);
            };

            // Properties.
            Properties = new PropertyItems(vcap_source);
        }

        private class SampleGrabberInfo
        {
            public DirectShow.ISampleGrabber Grabber { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Stride { get; set; }
        }

        private SampleGrabberInfo ConnectSampleGrabberAndRenderer(DirectShow.IFilterGraph graph, DirectShow.ICaptureGraphBuilder2 builder, DirectShow.IBaseFilter vcap_source, Guid pinCategory)
        {
            //------------------------------
            // SampleGrabber
            //------------------------------
            var grabber = CreateSampleGrabber();
            graph.AddFilter(grabber, "SampleGrabber");
            var i_grabber = (DirectShow.ISampleGrabber)grabber;
            i_grabber.SetBufferSamples(true);

            //---------------------------------------------------
            // Null Renderer
            //---------------------------------------------------
            var renderer = DirectShow.CoCreateInstance(DirectShow.DsGuid.CLSID_NullRenderer) as DirectShow.IBaseFilter;
            graph.AddFilter(renderer, "NullRenderer");

            //---------------------------------------------------
            // Connects vcap_source to SampleGrabber and Renderer
            //---------------------------------------------------
            try
            {
                //var pinCategory = DirectShow.DsGuid.PIN_CATEGORY_CAPTURE;
                var mediaType = DirectShow.DsGuid.MEDIATYPE_Video;
                builder.RenderStream(ref pinCategory, ref mediaType, vcap_source, grabber, renderer);
            }
            catch (Exception)
            {
                // if camera does not support pin, an exception was raised.
                // some camera do not support still pin.
                return null;
            }

            // SampleGrabber Format.
            {
                var mt = new DirectShow.AM_MEDIA_TYPE();
                i_grabber.GetConnectedMediaType(mt);
                var header = (DirectShow.VIDEOINFOHEADER)Marshal.PtrToStructure(mt.pbFormat, typeof(DirectShow.VIDEOINFOHEADER));
                var width = header.bmiHeader.biWidth;
                var height = header.bmiHeader.biHeight;
                var stride = width * (header.bmiHeader.biBitCount / 8);
                DirectShow.DeleteMediaType(ref mt);

                return new SampleGrabberInfo() { Grabber = i_grabber, Width = width, Height = height, Stride = stride };
            }
        }

        /// <summary>Properties user can adjust.</summary>
        public PropertyItems Properties { get; private set; }
        

        private class SampleGrabberCallback : DirectShow.ISampleGrabberCB
        {
            private byte[] Buffer;
            private object BufferLock = new object();

            public Action<Bitmap> Buffered { get; set; }

            private System.Threading.AutoResetEvent BufferedEvent;

            public int Width { get; private set; }
            public int Height { get; private set; }
            public int Stride { get; private set; }

            public SampleGrabberCallback(int width, int height, int stride)
            {
                this.Width = width;
                this.Height = height;
                this.Stride = stride;

                // create Buffered.Invoke thread.
                BufferedEvent = new System.Threading.AutoResetEvent(false);
                System.Threading.ThreadPool.QueueUserWorkItem(x =>
                {
                    while (true)
                    {
                        BufferedEvent.WaitOne(); // wait event.
                        Buffered?.Invoke(GetBitmap()); // fire!
                    }
                });
            }

            public Bitmap GetBitmap()
            {
                if (Buffer == null) return EmptyBitmap(Width, Height);

                lock (BufferLock)
                {
                    return BufferToBitmap(Buffer, Width, Height, Stride);
                }
            }

            // called when each sample completed.
            // The data processing thread blocks until the callback method returns. If the callback does not return quickly, it can interfere with playback.
            public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
            {
                if (Buffer == null || Buffer.Length != BufferLen)
                {
                    Buffer = new byte[BufferLen];
                }

                // replace lock statement to Monitor.TryEnter. (issue #14)
                var locked = false;
                try
                {
                    System.Threading.Monitor.TryEnter(BufferLock, 0, ref locked);
                    if (locked)
                    {
                        Marshal.Copy(pBuffer, Buffer, 0, BufferLen);
                    }
                }
                finally
                {
                    if (locked) System.Threading.Monitor.Exit(BufferLock);
                }

                // notify buffered to worker thread. (issue #16)
                if (Buffered != null) BufferedEvent.Set();

                return 0;
            }

            // never called.
            public int SampleCB(double SampleTime, DirectShow.IMediaSample pSample)
            {
                throw new NotImplementedException();
            }
        }

        private Func<Bitmap> GetBitmapFromSampleGrabberCallback(DirectShow.ISampleGrabber i_grabber, int width, int height, int stride)
        {
            var sampler = new SampleGrabberCallback(width, height, stride);
            i_grabber.SetCallback(sampler, 1); // WhichMethodToCallback = BufferCB
            return () => sampler.GetBitmap();
        }

        /// <summary>Get Bitmap from Sample Grabber Current Buffer</summary>
        private Bitmap GetBitmapFromSampleGrabberBuffer(DirectShow.ISampleGrabber i_grabber, int width, int height, int stride)
        {
            try
            {
                return GetBitmapFromSampleGrabberBufferMain(i_grabber, width, height, stride);
            }
            catch (COMException ex)
            {
                const uint VFW_E_WRONG_STATE = 0x80040227;
                if ((uint)ex.ErrorCode == VFW_E_WRONG_STATE)
                {
                    // image data is not ready yet. return empty bitmap.
                    return EmptyBitmap(width, height);
                }

                throw;
            }
        }

        /// <summary>Get Bitmap from Sample Grabber Current Buffer</summary>
        private Bitmap GetBitmapFromSampleGrabberBufferMain(DirectShow.ISampleGrabber i_grabber, int width, int height, int stride)
        {
            // サンプルグラバから画像を取得するためには
            // まずサイズ0でGetCurrentBufferを呼び出しバッファサイズを取得し
            // バッファ確保して再度GetCurrentBufferを呼び出す。
            // 取得した画像は逆になっているので反転させる必要がある。
            int sz = 0;
            i_grabber.GetCurrentBuffer(ref sz, IntPtr.Zero); // IntPtr.Zeroで呼び出してバッファサイズ取得
            if (sz == 0) return null;

            // メモリ確保し画像データ取得
            var ptr = Marshal.AllocCoTaskMem(sz);
            i_grabber.GetCurrentBuffer(ref sz, ptr);

            // 画像データをbyte配列に入れなおす
            var data = new byte[sz];
            Marshal.Copy(ptr, data, 0, sz);

            // 画像を作成
            var result = BufferToBitmap(data, width, height, stride);

            Marshal.FreeCoTaskMem(ptr);

            return result;
        }

#if USBCAMERA_WPF
        private static BitmapSource BufferToBitmap(byte[] buffer, int width, int height, int stride)
        {
            const double dpi = 96.0;
            var result = new WriteableBitmap(width, height, dpi, dpi, PixelFormats.Bgr24, null);

            var lenght = height * stride;
            var pixels = new byte[lenght];

            // copy from last row.
            for (int y = 0; y < height; y++)
            {
                var src_idx = buffer.Length - (stride * (y + 1));
                Buffer.BlockCopy(buffer, src_idx, pixels, stride * y, stride);
            }

            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            // if no Freeze(), StillImageCaptured image is not displayed in WPF.
            result.Freeze();

            return result;
        }

        private static BitmapSource EmptyBitmap(int width, int height)
        {
            return new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgr24, null);
        }
#else
        private static Bitmap BufferToBitmap(byte[] buffer, int width, int height, int stride)
        {
            var result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bmp_data = result.LockBits(new Rectangle(Point.Empty, result.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // copy from last row.
            for (int y = 0; y < height; y++)
            {
                var src_idx = buffer.Length - (stride * (y + 1));
                var dst = IntPtr.Add(bmp_data.Scan0, stride * y);
                Marshal.Copy(buffer, src_idx, dst, stride);
            }
            result.UnlockBits(bmp_data);

            return result;
        }

        private static Bitmap EmptyBitmap(int width, int height)
        {
            return new Bitmap(width, height);
        }
#endif

        /// <summary>
        /// サンプルグラバを作成する
        /// </summary>
        private DirectShow.IBaseFilter CreateSampleGrabber()
        {
            var filter = DirectShow.CreateFilter(DirectShow.DsGuid.CLSID_SampleGrabber);
            var ismp = filter as DirectShow.ISampleGrabber;

            // サンプル グラバを最初に作成したときは、優先メディア タイプは設定されていない。
            // これは、グラフ内のほぼすべてのフィルタに接続はできるが、受け取るデータ タイプを制御できないとうことである。
            // したがって、残りのグラフを作成する前に、ISampleGrabber::SetMediaType メソッドを呼び出して、
            // サンプル グラバに対してメディア タイプを設定すること。

            // サンプル グラバは、接続した時に他のフィルタが提供するメディア タイプとこの設定されたメディア タイプとを比較する。
            // 調べるフィールドは、メジャー タイプ、サブタイプ、フォーマット タイプだけである。
            // これらのフィールドでは、値 GUID_NULL は "あらゆる値を受け付ける" という意味である。
            // 通常は、メジャー タイプとサブタイプを設定する。

            // https://msdn.microsoft.com/ja-jp/library/cc370616.aspx
            // https://msdn.microsoft.com/ja-jp/library/cc369546.aspx
            // サンプル グラバ フィルタはトップダウン方向 (負の biHeight) のビデオ タイプ、または
            // FORMAT_VideoInfo2 のフォーマット タイプのビデオ タイプはすべて拒否する。

            var mt = new DirectShow.AM_MEDIA_TYPE();
            mt.MajorType = DirectShow.DsGuid.MEDIATYPE_Video;
            mt.SubType = DirectShow.DsGuid.MEDIASUBTYPE_RGB24;
            ismp.SetMediaType(mt);
            return filter;
        }

        /// <summary>
        /// Video Capture Sourceフィルタを作成する
        /// </summary>
        private DirectShow.IBaseFilter CreateVideoCaptureSource(int index, VideoFormat format)
        {
            var filter = DirectShow.CreateFilter(DirectShow.DsGuid.CLSID_VideoInputDeviceCategory, index);
            var pin = DirectShow.FindPin(filter, 0, DirectShow.PIN_DIRECTION.PINDIR_OUTPUT);
            SetVideoOutputFormat(pin, format);
            return filter;
        }

        /// <summary>
        /// ビデオキャプチャデバイスの出力形式を選択する。
        /// </summary>
        private static void SetVideoOutputFormat(DirectShow.IPin pin, VideoFormat format)
        {
            var formats = GetVideoOutputFormat(pin);

            // 仕様ではVideoCaptureDeviceはメディア タイプごとに一定範囲の出力フォーマットをサポートできる。例えば以下のように。
            // [0]:YUY2 最小:160x120, 最大:320x240, X軸4STEP, Y軸2STEPごと
            // [1]:RGB8 最小:640x480, 最大:640x480, X軸0STEP, Y軸0STEPごと
            // SetFormatで出力サイズとフレームレートをこの範囲内で設定可能。
            // ただし試した限り、手持ちのUSBカメラはすべてサイズ固定(最大・最小が同じ)で返してきた。

            // https://msdn.microsoft.com/ja-jp/windows/dd407352(v=vs.80)
            // VIDEO_STREAM_CONFIG_CAPSの以下を除くほとんどのメンバーはdeprecated(非推奨)である。
            // アプリケーションはその他のメンバーの利用を避けること。かわりにIAMStreamConfig::GetFormatを利用すること。
            // - Guid:FORMAT_VideoInfo or FORMAT_VideoInfo2など。
            // - VideoStandard:アナログTV信号のフォーマット(NTSC, PALなど)をAnalogVideoStandard列挙体で指定する。
            // - MinFrameInterval, MaxFrameInterval:ビデオキャプチャデバイスがサポートするフレームレートの範囲。100ナノ秒単位。

            // 上記によると、VIDEO_STREAM_CONFIG_CAPSは現在はdeprecated(非推奨)であるらしい。かわりにIAMStreamConfig::GetFormatを使用することらしい。
            // 上記仕様を守ったデバイスは出力サイズを固定で返すが、守ってない古いデバイスは出力サイズを可変で返す、と考えられる。
            // 参考までに、VIDEO_STREAM_CONFIG_CAPSで解像度・クロップサイズ・フレームレートなどを変更する手順は以下の通り。

            // ①フレームレート(これは非推奨ではない)
            // VIDEO_STREAM_CONFIG_CAPS のメンバ MinFrameInterval と MaxFrameInterval は各ビデオ フレームの最小の長さと最大の長さである。
            // 次の式を使って、これらの値をフレーム レートに変換できる。
            // frames per second = 10,000,000 / frame duration

            // 特定のフレーム レートを要求するには、メディア タイプにある構造体 VIDEOINFOHEADER か VIDEOINFOHEADER2 の AvgTimePerFrame の値を変更する。
            // デバイスは最小値と最大値の間で可能なすべての値はサポートしていないことがあるため、ドライバは使用可能な最も近い値を使う。

            // ②Cropping(画像の一部切り抜き)
            // MinCroppingSize = (160, 120) // Cropping最小サイズ。
            // MaxCroppingSize = (320, 240) // Cropping最大サイズ。
            // CropGranularityX = 4         // 水平方向細分度。
            // CropGranularityY = 8         // 垂直方向細分度。
            // CropAlignX = 2               // the top-left corner of the source rectangle can sit.
            // CropAlignY = 4               // the top-left corner of the source rectangle can sit.

            // ③出力サイズ
            // https://msdn.microsoft.com/ja-jp/library/cc353344.aspx
            // https://msdn.microsoft.com/ja-jp/library/cc371290.aspx
            // VIDEO_STREAM_CONFIG_CAPS 構造体は、このメディア タイプに使える最小と最大の幅と高さを示す。
            // また、"ステップ" サイズ"も示す。ステップ サイズは、幅または高さを調整できるインクリメントの値を定義する。
            // たとえば、デバイスは次の値を返すことがある。
            // MinOutputSize: 160 × 120
            // MaxOutputSize: 320 × 240
            // OutputGranularityX:8 ピクセル (水平ステップ サイズ)
            // OutputGranularityY:8 ピクセル (垂直ステップ サイズ)
            // これらの数値が与えられると、幅は範囲内 (160、168、176、... 304、312、320) の任意の値に、
            // 高さは範囲内 (120、128、136、... 224、232、240) の任意の値に設定できる。

            // 出力サイズの可変のUSBカメラがないためデバッグするには以下のコメントを外す。
            // I have no USB camera of variable output size, uncomment below to debug.
            //size = new Size(168, 126);
            //vformat[0].Caps = new DirectShow.VIDEO_STREAM_CONFIG_CAPS()
            //{
            //    Guid = DirectShow.DsGuid.FORMAT_VideoInfo,
            //    MinOutputSize = new DirectShow.SIZE() { cx = 160, cy = 120 },
            //    MaxOutputSize = new DirectShow.SIZE() { cx = 320, cy = 240 },
            //    OutputGranularityX = 4,
            //    OutputGranularityY = 2
            //};

            // VIDEO_STREAM_CONFIG_CAPSは現在では非推奨。まずは固定サイズを探す
            // VIDEO_STREAM_CONFIG_CAPS is deprecated. First, find just the fixed size.
            for (int i = 0; i < formats.Length; i++)
            {
                var item = formats[i];

                // VideoInfoのみ対応する。(VideoInfo2はSampleGrabber未対応のため)
                // VideoInfo only... (SampleGrabber do not support VideoInfo2)
                // https://msdn.microsoft.com/ja-jp/library/cc370616.aspx
                if (item.MajorType != DirectShow.DsGuid.GetNickname(DirectShow.DsGuid.MEDIATYPE_Video)) continue;
                if (string.IsNullOrEmpty(format.SubType) == false && format.SubType != item.SubType) continue;
                if (item.Caps.Guid != DirectShow.DsGuid.FORMAT_VideoInfo) continue;

                if (item.Size.Width == format.Size.Width && item.Size.Height == format.Size.Height)
                {
                    SetVideoOutputFormat(pin, i, format.Size, format.TimePerFrame);
                    return;
                }
            }

            // 固定サイズが見つからなかった。可変サイズの範囲を探す。
            // Not found fixed size, search for variable size.
            for (int i = 0; i < formats.Length; i++)
            {
                var item = formats[i];

                // VideoInfoのみ対応する。(VideoInfo2はSampleGrabber未対応のため)
                // VideoInfo only... (SampleGrabber do not support VideoInfo2)
                // https://msdn.microsoft.com/ja-jp/library/cc370616.aspx
                if (item.MajorType != DirectShow.DsGuid.GetNickname(DirectShow.DsGuid.MEDIATYPE_Video)) continue;
                if (string.IsNullOrEmpty(format.SubType) == false && format.SubType != item.SubType) continue;
                if (item.Caps.Guid != DirectShow.DsGuid.FORMAT_VideoInfo) continue;

                if (item.Caps.OutputGranularityX == 0) continue;
                if (item.Caps.OutputGranularityY == 0) continue;

                for (int w = item.Caps.MinOutputSize.cx; w < item.Caps.MaxOutputSize.cx; w += item.Caps.OutputGranularityX)
                {
                    for (int h = item.Caps.MinOutputSize.cy; h < item.Caps.MaxOutputSize.cy; h += item.Caps.OutputGranularityY)
                    {
                        if (w == format.Size.Width && h == format.Size.Height)
                        {
                            SetVideoOutputFormat(pin, i, format.Size, format.TimePerFrame);
                            return;
                        }
                    }
                }
            }

            // サイズが見つかなかった場合はデフォルトサイズとする。
            // Not found, use default size.
            SetVideoOutputFormat(pin, 0, Size.Empty, 0);
        }


        /// <summary>
        /// ビデオキャプチャデバイスがサポートするメディアタイプ・サイズを取得する。
        /// </summary>
        private static VideoFormat[] GetVideoOutputFormat(DirectShow.IPin pin)
        {
            // IAMStreamConfigインタフェース取得
            var config = pin as DirectShow.IAMStreamConfig;
            if (config == null)
            {
                throw new InvalidOperationException("no IAMStreamConfig interface.");
            }

            // フォーマット個数取得
            int cap_count = 0, cap_size = 0;
            config.GetNumberOfCapabilities(ref cap_count, ref cap_size);
            if (cap_size != Marshal.SizeOf(typeof(DirectShow.VIDEO_STREAM_CONFIG_CAPS)))
            {
                throw new InvalidOperationException("no VIDEO_STREAM_CONFIG_CAPS.");
            }

            // 返却値の確保
            var result = new VideoFormat[cap_count];

            // データ用領域確保
            var cap_data = Marshal.AllocHGlobal(cap_size);

            // 列挙
            for (int i = 0; i < cap_count; i++)
            {
                var entry = new VideoFormat();

                // x番目のフォーマット情報取得
                DirectShow.AM_MEDIA_TYPE mt = null;
                config.GetStreamCaps(i, ref mt, cap_data);
                entry.Caps = PtrToStructure<DirectShow.VIDEO_STREAM_CONFIG_CAPS>(cap_data);

                // フォーマット情報の読み取り
                entry.MajorType = DirectShow.DsGuid.GetNickname(mt.MajorType);
                entry.SubType = DirectShow.DsGuid.GetNickname(mt.SubType);

                if (mt.FormatType == DirectShow.DsGuid.FORMAT_VideoInfo)
                {
                    var vinfo = PtrToStructure<DirectShow.VIDEOINFOHEADER>(mt.pbFormat);
                    entry.Size = new Size(vinfo.bmiHeader.biWidth, vinfo.bmiHeader.biHeight);
                    entry.TimePerFrame = vinfo.AvgTimePerFrame;
                }
                else if (mt.FormatType == DirectShow.DsGuid.FORMAT_VideoInfo2)
                {
                    var vinfo = PtrToStructure<DirectShow.VIDEOINFOHEADER2>(mt.pbFormat);
                    entry.Size = new Size(vinfo.bmiHeader.biWidth, vinfo.bmiHeader.biHeight);
                    entry.TimePerFrame = vinfo.AvgTimePerFrame;
                }

                // 解放
                DirectShow.DeleteMediaType(ref mt);

                result[i] = entry;
            }

            // 解放
            Marshal.FreeHGlobal(cap_data);

            return result;
        }

        /// <summary>
        /// ビデオキャプチャデバイスの出力形式を選択する。
        /// 事前にGetVideoOutputFormatでメディアタイプ・サイズを得ておき、その中から希望のindexを指定する。
        /// 同時に出力サイズとフレームレートを変更することができる。
        /// </summary>
        /// <param name="index">希望のindexを指定する</param>
        /// <param name="size">Empty以外を指定すると出力サイズを変更する。事前にVIDEO_STREAM_CONFIG_CAPSで取得した可能範囲内を指定すること。</param>
        /// <param name="timePerFrame">0以上を指定するとフレームレートを変更する。事前にVIDEO_STREAM_CONFIG_CAPSで取得した可能範囲内を指定すること。</param>
        private static void SetVideoOutputFormat(DirectShow.IPin pin, int index, Size size, long timePerFrame)
        {
            // IAMStreamConfigインタフェース取得
            var config = pin as DirectShow.IAMStreamConfig;
            if (config == null)
            {
                throw new InvalidOperationException("no IAMStreamConfig interface.");
            }

            // フォーマット個数取得
            int cap_count = 0, cap_size = 0;
            config.GetNumberOfCapabilities(ref cap_count, ref cap_size);
            if (cap_size != Marshal.SizeOf(typeof(DirectShow.VIDEO_STREAM_CONFIG_CAPS)))
            {
                throw new InvalidOperationException("no VIDEO_STREAM_CONFIG_CAPS.");
            }

            // データ用領域確保
            var cap_data = Marshal.AllocHGlobal(cap_size);

            // idx番目のフォーマット情報取得
            DirectShow.AM_MEDIA_TYPE mt = null;
            config.GetStreamCaps(index, ref mt, cap_data);
            var cap = PtrToStructure<DirectShow.VIDEO_STREAM_CONFIG_CAPS>(cap_data);

            if (mt.FormatType == DirectShow.DsGuid.FORMAT_VideoInfo)
            {
                var vinfo = PtrToStructure<DirectShow.VIDEOINFOHEADER>(mt.pbFormat);
                if (!size.IsEmpty) { vinfo.bmiHeader.biWidth = (int)size.Width; vinfo.bmiHeader.biHeight = (int)size.Height; }
                if (timePerFrame > 0) { vinfo.AvgTimePerFrame = timePerFrame; }
                Marshal.StructureToPtr(vinfo, mt.pbFormat, true);
            }
            else if (mt.FormatType == DirectShow.DsGuid.FORMAT_VideoInfo2)
            {
                var vinfo = PtrToStructure<DirectShow.VIDEOINFOHEADER2>(mt.pbFormat);
                if (!size.IsEmpty) { vinfo.bmiHeader.biWidth = (int)size.Width; vinfo.bmiHeader.biHeight = (int)size.Height; }
                if (timePerFrame > 0) { vinfo.AvgTimePerFrame = timePerFrame; }
                Marshal.StructureToPtr(vinfo, mt.pbFormat, true);
            }

            // フォーマットを選択
            config.SetFormat(mt);

            // 解放
            if (cap_data != System.IntPtr.Zero) Marshal.FreeHGlobal(cap_data);
            if (mt != null) DirectShow.DeleteMediaType(ref mt);
        }

        private static T PtrToStructure<T>(IntPtr ptr)
        {
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }

        public class VideoFormat
        {
            public string MajorType { get; set; }  // [Video]
            public string SubType { get; set; }    // [YUY2], [MJPG]
            public Size Size { get; set; }         
            public long TimePerFrame { get; set; } // 30fps
            public DirectShow.VIDEO_STREAM_CONFIG_CAPS Caps { get; set; }

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}, {3}, {4}", MajorType, SubType, Size, TimePerFrame, CapsString());
            }

            private string CapsString()
            {
                var sb = new StringBuilder();
                sb.AppendFormat("{0}, ", DirectShow.DsGuid.GetNickname(Caps.Guid));
                foreach (var info in Caps.GetType().GetFields())
                {
                    sb.AppendFormat("{0}={1}, ", info.Name, info.GetValue(Caps));
                }
                return sb.ToString();
            }
        }
    }
}
