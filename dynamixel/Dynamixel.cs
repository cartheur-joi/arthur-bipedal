using System.Runtime.InteropServices;

namespace Cartheur.Animals.Robot
{
    /// <summary>
    /// The wrapper class for the Dynamixel C library.
    /// </summary>
    public class Dynamixel
    {
        const string LibraryPath = "lib/dxl_x64_c.dll";

        #region PortHandler        
        /// <summary>
        /// The port handler containing the functions needed for port communication.
        /// </summary>
        /// <param name="port_name">Name of the port.</param>
        /// <returns></returns>
        [DllImport(LibraryPath)]
        public static extern int portHandler(string port_name);
        [DllImport(LibraryPath)]
        public static extern bool openPort(int port_num);
        [DllImport(LibraryPath)]
        public static extern void closePort(int port_num);
        [DllImport(LibraryPath)]
        public static extern void clearPort(int port_num);

        [DllImport(LibraryPath)]
        public static extern void setPortName(int port_num, string port_name);
        [DllImport(LibraryPath)]
        public static extern string getPortName(int port_num);
        /// <summary>
        /// Sets the baud rate.
        /// </summary>
        /// <param name="port_num">The port number.</param>
        /// <param name="baudrate">The baudrate.</param>
        /// <returns></returns>
        [DllImport(LibraryPath)]
        public static extern bool setBaudRate(int port_num, int baudrate);
        /// <summary>
        /// Gets the baud rate.
        /// </summary>
        /// <param name="port_num">The port number.</param>
        /// <returns></returns>
        [DllImport(LibraryPath)]
        public static extern int getBaudRate(int port_num);

        [DllImport(LibraryPath)]
        public static extern int readPort(int port_num, byte[] packet, int length);
        [DllImport(LibraryPath)]
        public static extern int writePort(int port_num, byte[] packet, int length);

        [DllImport(LibraryPath)]
        public static extern void setPacketTimeout(int port_num, UInt16 packet_length);
        [DllImport(LibraryPath)]
        public static extern void setPacketTimeoutMSec(int port_num, double msec);
        [DllImport(LibraryPath)]
        public static extern bool isPacketTimeout(int port_num);
        #endregion

        #region PacketHandler        
        /// <summary>
        /// The packet handler for data across the port.
        /// </summary>
        [DllImport(LibraryPath)]
        public static extern void packetHandler();

        [DllImport(LibraryPath)]
        public static extern IntPtr getTxRxResult(int protocol_version, int result);
        [DllImport(LibraryPath)]
        public static extern IntPtr getRxPacketError(int protocol_version, byte error);

        [DllImport(LibraryPath)]
        public static extern int getLastTxRxResult(int port_num, int protocol_version);
        [DllImport(LibraryPath)]
        public static extern byte getLastRxPacketError(int port_num, int protocol_version);

        [DllImport(LibraryPath)]
        public static extern void setDataWrite(int port_num, int protocol_version, UInt16 data_length, UInt16 data_pos, UInt32 data);
        [DllImport(LibraryPath)]
        public static extern UInt32 getDataRead(int port_num, int protocol_version, UInt16 data_length, UInt16 data_pos);

        [DllImport(LibraryPath)]
        public static extern void txPacket(int port_num, int protocol_version);

        [DllImport(LibraryPath)]
        public static extern void rxPacket(int port_num, int protocol_version);

        [DllImport(LibraryPath)]
        public static extern void txRxPacket(int port_num, int protocol_version);

        [DllImport(LibraryPath)]
        public static extern void ping(int port_num, int protocol_version, byte id);

        [DllImport(LibraryPath)]
        public static extern UInt16 pingGetModelNum(int port_num, int protocol_version, byte id);

        [DllImport(LibraryPath)]
        public static extern void broadcastPing(int port_num, int protocol_version);
        [DllImport(LibraryPath)]
        public static extern bool getBroadcastPingResult(int port_num, int protocol_version, int id);

        [DllImport(LibraryPath)]
        public static extern void reboot(int port_num, int protocol_version, byte id);

        [DllImport(LibraryPath)]
        public static extern void factoryReset(int port_num, int protocol_version, byte id, byte option);

        [DllImport(LibraryPath)]
        public static extern void readTx(int port_num, int protocol_version, byte id, UInt16 address, UInt16 length);
        [DllImport(LibraryPath)]
        public static extern void readRx(int port_num, int protocol_version, UInt16 length);
        [DllImport(LibraryPath)]
        public static extern void readTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt16 length);

        [DllImport(LibraryPath)]
        public static extern void read1ByteTx(int port_num, int protocol_version, byte id, UInt16 address);
        [DllImport(LibraryPath)]
        public static extern byte read1ByteRx(int port_num, int protocol_version);
        [DllImport(LibraryPath)]
        public static extern byte read1ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address);

        [DllImport(LibraryPath)]
        public static extern void read2ByteTx(int port_num, int protocol_version, byte id, UInt16 address);
        [DllImport(LibraryPath)]
        public static extern UInt16 read2ByteRx(int port_num, int protocol_version);
        [DllImport(LibraryPath)]
        public static extern UInt16 read2ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address);

        [DllImport(LibraryPath)]
        public static extern void read4ByteTx(int port_num, int protocol_version, byte id, UInt16 address);
        [DllImport(LibraryPath)]
        public static extern UInt32 read4ByteRx(int port_num, int protocol_version);
        [DllImport(LibraryPath)]
        public static extern UInt32 read4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address);

        [DllImport(LibraryPath)]
        public static extern void writeTxOnly(int port_num, int protocol_version, byte id, UInt16 address, UInt16 length);
        [DllImport(LibraryPath)]
        public static extern void writeTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt16 length);

        [DllImport(LibraryPath)]
        public static extern void write1ByteTxOnly(int port_num, int protocol_version, byte id, UInt16 address, byte data);
        /// <summary>
        /// Writes the byte tx rx.
        /// </summary>
        /// <param name="port_num">The port number.</param>
        /// <param name="protocol_version">The protocol version.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="address">The address.</param>
        /// <param name="data">The data.</param>
        [DllImport(LibraryPath)]
        public static extern void write1ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, byte data);

        [DllImport(LibraryPath)]
        public static extern void write2ByteTxOnly(int port_num, int protocol_version, byte id, UInt16 address, UInt16 data);
        [DllImport(LibraryPath)]
        public static extern void write2ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt16 data);

        [DllImport(LibraryPath)]
        public static extern void write4ByteTxOnly(int port_num, int protocol_version, byte id, UInt16 address, UInt32 data);
        [DllImport(LibraryPath)]
        public static extern void write4ByteTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt32 data);

        [DllImport(LibraryPath)]
        public static extern void regWriteTxOnly(int port_num, int protocol_version, byte id, UInt16 address, UInt16 length);
        [DllImport(LibraryPath)]
        public static extern void regWriteTxRx(int port_num, int protocol_version, byte id, UInt16 address, UInt16 length);

        [DllImport(LibraryPath)]
        public static extern void syncReadTx(int port_num, int protocol_version, UInt16 start_address, UInt16 data_length, UInt16 param_length);
        // syncReadRx   -> GroupSyncRead
        // syncReadTxRx -> GroupSyncRead

        [DllImport(LibraryPath)]
        public static extern void syncWriteTxOnly(int port_num, int protocol_version, UInt16 start_address, UInt16 data_length, UInt16 param_length);

        [DllImport(LibraryPath)]
        public static extern void bulkReadTx(int port_num, int protocol_version, UInt16 param_length);
        // bulkReadRx   -> GroupBulkRead
        // bulkReadTxRx -> GroupBulkRead

        [DllImport(LibraryPath)]
        public static extern void bulkWriteTxOnly(int port_num, int protocol_version, UInt16 param_length);
        #endregion

        #region GroupBulkRead
        [DllImport(LibraryPath)]
        public static extern int groupBulkRead(int port_num, int protocol_version);

        [DllImport(LibraryPath)]
        public static extern bool groupBulkReadAddParam(int group_num, byte id, UInt16 start_address, UInt16 data_length);
        [DllImport(LibraryPath)]
        public static extern void groupBulkReadRemoveParam(int group_num, byte id);
        [DllImport(LibraryPath)]
        public static extern void groupBulkReadClearParam(int group_num);

        [DllImport(LibraryPath)]
        public static extern void groupBulkReadTxPacket(int group_num);
        [DllImport(LibraryPath)]
        public static extern void groupBulkReadRxPacket(int group_num);
        [DllImport(LibraryPath)]
        public static extern void groupBulkReadTxRxPacket(int group_num);

        [DllImport(LibraryPath)]
        public static extern bool groupBulkReadIsAvailable(int group_num, byte id, UInt16 address, UInt16 data_length);
        [DllImport(LibraryPath)]
        public static extern UInt32 groupBulkReadGetData(int group_num, byte id, UInt16 address, UInt16 data_length);
        #endregion

        #region GroupBulkWrite
        [DllImport(LibraryPath)]
        public static extern int groupBulkWrite(int port_num, int protocol_version);

        [DllImport(LibraryPath)]
        public static extern bool groupBulkWriteAddParam(int group_num, byte id, UInt16 start_address, UInt16 data_length, UInt32 data, UInt16 input_length);
        [DllImport(LibraryPath)]
        public static extern void groupBulkWriteRemoveParam(int group_num, byte id);
        [DllImport(LibraryPath)]
        public static extern bool groupBulkWriteChangeParam(int group_num, byte id, UInt16 start_address, UInt16 data_length, UInt32 data, UInt16 input_length, UInt16 data_pos);
        [DllImport(LibraryPath)]
        public static extern void groupBulkWriteClearParam(int group_num);

        [DllImport(LibraryPath)]
        public static extern void groupBulkWriteTxPacket(int group_num);
        #endregion

        #region GroupSyncRead
        [DllImport(LibraryPath)]
        public static extern int groupSyncRead(int port_num, int protocol_version, UInt16 start_address, UInt16 data_length);

        [DllImport(LibraryPath)]
        public static extern bool groupSyncReadAddParam(int group_num, byte id);
        [DllImport(LibraryPath)]
        public static extern void groupSyncReadRemoveParam(int group_num, byte id);
        [DllImport(LibraryPath)]
        public static extern void groupSyncReadClearParam(int group_num);

        [DllImport(LibraryPath)]
        public static extern void groupSyncReadTxPacket(int group_num);
        [DllImport(LibraryPath)]
        public static extern void groupSyncReadRxPacket(int group_num);
        [DllImport(LibraryPath)]
        public static extern void groupSyncReadTxRxPacket(int group_num);

        [DllImport(LibraryPath)]
        public static extern bool groupSyncReadIsAvailable(int group_num, byte id, UInt16 address, UInt16 data_length);
        [DllImport(LibraryPath)]
        public static extern UInt32 groupSyncReadGetData(int group_num, byte id, UInt16 address, UInt16 data_length);
        #endregion

        #region GroupSyncWrite
        [DllImport(LibraryPath)]
        public static extern int groupSyncWrite(int port_num, int protocol_version, UInt16 start_address, UInt16 data_length);

        [DllImport(LibraryPath)]
        public static extern bool groupSyncWriteAddParam(int group_num, byte id, UInt32 data, UInt16 data_length);
        [DllImport(LibraryPath)]
        public static extern void groupSyncWriteRemoveParam(int group_num, byte id);
        [DllImport(LibraryPath)]
        public static extern bool groupSyncWriteChangeParam(int group_num, byte id, UInt32 data, UInt16 data_length, UInt16 data_pos);
        [DllImport(LibraryPath)]
        public static extern void groupSyncWriteClearParam(int group_num);

        [DllImport(LibraryPath)]
        public static extern void groupSyncWriteTxPacket(int group_num);
        #endregion
    }
}
