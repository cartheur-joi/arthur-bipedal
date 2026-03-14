namespace DynamixelWizard.SubForms
{
    partial class TemplaterForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TemplaterForm));
            torqueOnButton = new Button();
            logWindow = new RichTextBox();
            closeButton = new Button();
            torqueOffButton = new Button();
            raiseArmButton = new Button();
            liftLegButton = new Button();
            turnHeadButton = new Button();
            motorSelectionList = new ComboBox();
            getPositionButton = new Button();
            areaMotorSelection = new ComboBox();
            torqueGroupBox = new GroupBox();
            motorControlGroupBox = new GroupBox();
            notification = new Label();
            viewTableButton = new Button();
            granularTorqueOffButton = new Button();
            granularTorqueOnButton = new Button();
            label3 = new Label();
            basicAnimationsGroupBox = new GroupBox();
            limbComboBox = new ComboBox();
            positionNowFlashButton = new Controls.FlashButton();
            storePositionButton = new Controls.FlashButton();
            sideComboBox = new ComboBox();
            engageDictionaryRoutine = new Button();
            threeStepsBackwardButton = new Button();
            threeStepsForwardButton = new Button();
            viewMotorsButton = new Button();
            label1 = new Label();
            monitorSerialButton = new Button();
            ceaseSerialMonitorButton = new Button();
            xcPositionLabel = new Label();
            zcPositionLabel = new Label();
            ycPositionLabel = new Label();
            clearMonitorButton = new Button();
            controlTableGroupBox = new GroupBox();
            ax18Button = new Button();
            mxButton = new Button();
            ax12Button = new Button();
            clearLogButton = new Button();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            xsPositionLabel = new Label();
            zsPositionLabel = new Label();
            ysPositionLabel = new Label();
            groupBox3 = new GroupBox();
            xwPositionLabel = new Label();
            zwPositionLabel = new Label();
            ywPositionLabel = new Label();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            torqueStatusIndicator = new Controls.FlashButton();
            label4 = new Label();
            groupBox4 = new GroupBox();
            torqueGroupBox.SuspendLayout();
            motorControlGroupBox.SuspendLayout();
            basicAnimationsGroupBox.SuspendLayout();
            controlTableGroupBox.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            statusStrip1.SuspendLayout();
            groupBox4.SuspendLayout();
            SuspendLayout();
            // 
            // torqueOnButton
            // 
            torqueOnButton.Location = new Point(111, 32);
            torqueOnButton.Margin = new Padding(4, 3, 4, 3);
            torqueOnButton.Name = "torqueOnButton";
            torqueOnButton.Size = new Size(88, 27);
            torqueOnButton.TabIndex = 0;
            torqueOnButton.Text = "Torque ON";
            torqueOnButton.UseVisualStyleBackColor = true;
            torqueOnButton.Click += TorqueOnButtonClick;
            // 
            // logWindow
            // 
            logWindow.Location = new Point(21, 300);
            logWindow.Margin = new Padding(4, 3, 4, 3);
            logWindow.Name = "logWindow";
            logWindow.Size = new Size(319, 243);
            logWindow.TabIndex = 2;
            logWindow.Text = "";
            // 
            // closeButton
            // 
            closeButton.Location = new Point(602, 557);
            closeButton.Margin = new Padding(4, 3, 4, 3);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(88, 27);
            closeButton.TabIndex = 3;
            closeButton.Text = "Close";
            closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += CloseButtonClick;
            // 
            // torqueOffButton
            // 
            torqueOffButton.Location = new Point(205, 32);
            torqueOffButton.Margin = new Padding(4, 3, 4, 3);
            torqueOffButton.Name = "torqueOffButton";
            torqueOffButton.Size = new Size(88, 27);
            torqueOffButton.TabIndex = 4;
            torqueOffButton.Text = "Torque OFF";
            torqueOffButton.UseVisualStyleBackColor = true;
            torqueOffButton.Click += TorqueOffButtonClick;
            // 
            // raiseArmButton
            // 
            raiseArmButton.Location = new Point(8, 24);
            raiseArmButton.Margin = new Padding(4, 3, 4, 3);
            raiseArmButton.Name = "raiseArmButton";
            raiseArmButton.Size = new Size(103, 27);
            raiseArmButton.TabIndex = 6;
            raiseArmButton.Text = "Arm animation";
            raiseArmButton.UseVisualStyleBackColor = true;
            raiseArmButton.Click += RaiseArmClick;
            // 
            // liftLegButton
            // 
            liftLegButton.Location = new Point(191, 24);
            liftLegButton.Margin = new Padding(4, 3, 4, 3);
            liftLegButton.Name = "liftLegButton";
            liftLegButton.Size = new Size(102, 27);
            liftLegButton.TabIndex = 7;
            liftLegButton.Text = "Leg animation";
            liftLegButton.UseVisualStyleBackColor = true;
            liftLegButton.Click += LiftLegButtonClick;
            // 
            // turnHeadButton
            // 
            turnHeadButton.Location = new Point(4, 267);
            turnHeadButton.Margin = new Padding(4, 3, 4, 3);
            turnHeadButton.Name = "turnHeadButton";
            turnHeadButton.Size = new Size(88, 27);
            turnHeadButton.TabIndex = 8;
            turnHeadButton.Text = "Turn head";
            turnHeadButton.UseVisualStyleBackColor = true;
            turnHeadButton.Click += TurnHeadButtonClick;
            // 
            // motorSelectionList
            // 
            motorSelectionList.FormattingEnabled = true;
            motorSelectionList.Items.AddRange(new object[] { "l_hip_x", "l_hip_z", "l_hip_y", "l_knee_y", "l_ankle_y", "r_hip_x", "r_hip_z", "r_hip_y", "r_knee_y", "r_ankle_y", "abs_y", "abs_x", "abs_z", "bust_y", "bust_x", "head_z", "head_y", "l_shoulder_y", "l_shoulder_x", "l_arm_z", "l_elbow_y", "r_shoulder_y", "r_shoulder_x", "r_arm_z", "r_elbow_y" });
            motorSelectionList.Location = new Point(7, 44);
            motorSelectionList.Margin = new Padding(4, 3, 4, 3);
            motorSelectionList.Name = "motorSelectionList";
            motorSelectionList.Size = new Size(110, 23);
            motorSelectionList.TabIndex = 10;
            // 
            // getPositionButton
            // 
            getPositionButton.Location = new Point(139, 57);
            getPositionButton.Margin = new Padding(4, 3, 4, 3);
            getPositionButton.Name = "getPositionButton";
            getPositionButton.Size = new Size(102, 27);
            getPositionButton.TabIndex = 11;
            getPositionButton.Text = "Get positiion";
            getPositionButton.UseVisualStyleBackColor = true;
            getPositionButton.Click += GetPositionButtonClick;
            // 
            // areaMotorSelection
            // 
            areaMotorSelection.FormattingEnabled = true;
            areaMotorSelection.Items.AddRange(new object[] { "all", "upper", "lower" });
            areaMotorSelection.Location = new Point(15, 32);
            areaMotorSelection.Margin = new Padding(4, 3, 4, 3);
            areaMotorSelection.Name = "areaMotorSelection";
            areaMotorSelection.Size = new Size(87, 23);
            areaMotorSelection.TabIndex = 12;
            // 
            // torqueGroupBox
            // 
            torqueGroupBox.Controls.Add(torqueOnButton);
            torqueGroupBox.Controls.Add(areaMotorSelection);
            torqueGroupBox.Controls.Add(torqueOffButton);
            torqueGroupBox.Location = new Point(364, 14);
            torqueGroupBox.Margin = new Padding(4, 3, 4, 3);
            torqueGroupBox.Name = "torqueGroupBox";
            torqueGroupBox.Padding = new Padding(4, 3, 4, 3);
            torqueGroupBox.Size = new Size(307, 70);
            torqueGroupBox.TabIndex = 13;
            torqueGroupBox.TabStop = false;
            torqueGroupBox.Text = "Control torque at motors";
            // 
            // motorControlGroupBox
            // 
            motorControlGroupBox.Controls.Add(notification);
            motorControlGroupBox.Controls.Add(viewTableButton);
            motorControlGroupBox.Controls.Add(granularTorqueOffButton);
            motorControlGroupBox.Controls.Add(granularTorqueOnButton);
            motorControlGroupBox.Controls.Add(label3);
            motorControlGroupBox.Controls.Add(getPositionButton);
            motorControlGroupBox.Controls.Add(motorSelectionList);
            motorControlGroupBox.Location = new Point(14, 14);
            motorControlGroupBox.Margin = new Padding(4, 3, 4, 3);
            motorControlGroupBox.Name = "motorControlGroupBox";
            motorControlGroupBox.Padding = new Padding(4, 3, 4, 3);
            motorControlGroupBox.Size = new Size(327, 159);
            motorControlGroupBox.TabIndex = 14;
            motorControlGroupBox.TabStop = false;
            motorControlGroupBox.Text = "Granular motor control";
            // 
            // notification
            // 
            notification.AutoSize = true;
            notification.Location = new Point(15, 89);
            notification.Margin = new Padding(4, 0, 4, 0);
            notification.Name = "notification";
            notification.Size = new Size(17, 15);
            notification.TabIndex = 29;
            notification.Text = "--";
            // 
            // viewTableButton
            // 
            viewTableButton.Location = new Point(139, 89);
            viewTableButton.Margin = new Padding(4, 3, 4, 3);
            viewTableButton.Name = "viewTableButton";
            viewTableButton.Size = new Size(88, 27);
            viewTableButton.TabIndex = 14;
            viewTableButton.Text = "View table";
            viewTableButton.UseVisualStyleBackColor = true;
            viewTableButton.Click += ViewRelationalTableButtonClick;
            // 
            // granularTorqueOffButton
            // 
            granularTorqueOffButton.Location = new Point(232, 23);
            granularTorqueOffButton.Margin = new Padding(4, 3, 4, 3);
            granularTorqueOffButton.Name = "granularTorqueOffButton";
            granularTorqueOffButton.Size = new Size(88, 27);
            granularTorqueOffButton.TabIndex = 13;
            granularTorqueOffButton.Text = "Torque OFF";
            granularTorqueOffButton.UseVisualStyleBackColor = true;
            granularTorqueOffButton.Click += GranularTorqueOffButtonClick;
            // 
            // granularTorqueOnButton
            // 
            granularTorqueOnButton.Location = new Point(139, 23);
            granularTorqueOnButton.Margin = new Padding(4, 3, 4, 3);
            granularTorqueOnButton.Name = "granularTorqueOnButton";
            granularTorqueOnButton.Size = new Size(88, 27);
            granularTorqueOnButton.TabIndex = 13;
            granularTorqueOnButton.Text = "Torque ON";
            granularTorqueOnButton.UseVisualStyleBackColor = true;
            granularTorqueOnButton.Click += GranularTorqueOnButtonClick;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(15, 23);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(90, 15);
            label3.TabIndex = 9;
            label3.Text = "Motor selection";
            // 
            // basicAnimationsGroupBox
            // 
            basicAnimationsGroupBox.Controls.Add(limbComboBox);
            basicAnimationsGroupBox.Controls.Add(positionNowFlashButton);
            basicAnimationsGroupBox.Controls.Add(storePositionButton);
            basicAnimationsGroupBox.Controls.Add(sideComboBox);
            basicAnimationsGroupBox.Controls.Add(engageDictionaryRoutine);
            basicAnimationsGroupBox.Controls.Add(threeStepsBackwardButton);
            basicAnimationsGroupBox.Controls.Add(threeStepsForwardButton);
            basicAnimationsGroupBox.Controls.Add(raiseArmButton);
            basicAnimationsGroupBox.Controls.Add(liftLegButton);
            basicAnimationsGroupBox.Controls.Add(turnHeadButton);
            basicAnimationsGroupBox.Location = new Point(364, 79);
            basicAnimationsGroupBox.Margin = new Padding(4, 3, 4, 3);
            basicAnimationsGroupBox.Name = "basicAnimationsGroupBox";
            basicAnimationsGroupBox.Padding = new Padding(4, 3, 4, 3);
            basicAnimationsGroupBox.Size = new Size(338, 307);
            basicAnimationsGroupBox.TabIndex = 15;
            basicAnimationsGroupBox.TabStop = false;
            basicAnimationsGroupBox.Text = "Basic animations";
            // 
            // limbComboBox
            // 
            limbComboBox.FormattingEnabled = true;
            limbComboBox.Items.AddRange(new object[] { "arm", "leg" });
            limbComboBox.Location = new Point(124, 27);
            limbComboBox.Margin = new Padding(4, 3, 4, 3);
            limbComboBox.Name = "limbComboBox";
            limbComboBox.Size = new Size(60, 23);
            limbComboBox.TabIndex = 15;
            limbComboBox.SelectedIndexChanged += LimbSelectionBoxSelectedIndexChanged;
            // 
            // positionNowFlashButton
            // 
            positionNowFlashButton.FlasherButtonColorOff = SystemColors.Control;
            positionNowFlashButton.FlasherButtonColorOn = Color.LimeGreen;
            positionNowFlashButton.FlashNumber = 0;
            positionNowFlashButton.Location = new Point(15, 60);
            positionNowFlashButton.Name = "positionNowFlashButton";
            positionNowFlashButton.Size = new Size(91, 27);
            positionNowFlashButton.TabIndex = 14;
            positionNowFlashButton.Text = "Position Now";
            positionNowFlashButton.UseVisualStyleBackColor = true;
            positionNowFlashButton.Click += PositionNowFlashButtonClick;
            // 
            // storePositionButton
            // 
            storePositionButton.FlasherButtonColorOff = SystemColors.Control;
            storePositionButton.FlasherButtonColorOn = Color.LimeGreen;
            storePositionButton.FlashNumber = 0;
            storePositionButton.Location = new Point(191, 57);
            storePositionButton.Name = "storePositionButton";
            storePositionButton.Size = new Size(104, 27);
            storePositionButton.TabIndex = 13;
            storePositionButton.Text = "Desired Position";
            storePositionButton.UseVisualStyleBackColor = true;
            storePositionButton.Click += DesiredPositionButtonClick;
            // 
            // sideComboBox
            // 
            sideComboBox.FormattingEnabled = true;
            sideComboBox.Items.AddRange(new object[] { "left", "right" });
            sideComboBox.Location = new Point(124, 60);
            sideComboBox.Margin = new Padding(4, 3, 4, 3);
            sideComboBox.Name = "sideComboBox";
            sideComboBox.Size = new Size(60, 23);
            sideComboBox.TabIndex = 12;
            sideComboBox.SelectedIndexChanged += SideComboBoxSelectedIndexChanged;
            // 
            // engageDictionaryRoutine
            // 
            engageDictionaryRoutine.Location = new Point(168, 267);
            engageDictionaryRoutine.Margin = new Padding(4, 3, 4, 3);
            engageDictionaryRoutine.Name = "engageDictionaryRoutine";
            engageDictionaryRoutine.Size = new Size(167, 27);
            engageDictionaryRoutine.TabIndex = 11;
            engageDictionaryRoutine.Text = "Engage dictionary routine";
            engageDictionaryRoutine.UseVisualStyleBackColor = true;
            engageDictionaryRoutine.Click += EngageDictionaryRoutineClick;
            // 
            // threeStepsBackwardButton
            // 
            threeStepsBackwardButton.Location = new Point(181, 234);
            threeStepsBackwardButton.Margin = new Padding(4, 3, 4, 3);
            threeStepsBackwardButton.Name = "threeStepsBackwardButton";
            threeStepsBackwardButton.Size = new Size(155, 27);
            threeStepsBackwardButton.TabIndex = 10;
            threeStepsBackwardButton.Text = "Walk three steps back";
            threeStepsBackwardButton.UseVisualStyleBackColor = true;
            threeStepsBackwardButton.Click += ThreeStepsBackwardButtonClick;
            // 
            // threeStepsForwardButton
            // 
            threeStepsForwardButton.Location = new Point(180, 201);
            threeStepsForwardButton.Margin = new Padding(4, 3, 4, 3);
            threeStepsForwardButton.Name = "threeStepsForwardButton";
            threeStepsForwardButton.Size = new Size(155, 27);
            threeStepsForwardButton.TabIndex = 9;
            threeStepsForwardButton.Text = "Walk three steps forward";
            threeStepsForwardButton.UseVisualStyleBackColor = true;
            threeStepsForwardButton.Click += ThreeStepsForwardButtonClick;
            // 
            // viewMotorsButton
            // 
            viewMotorsButton.Location = new Point(153, 136);
            viewMotorsButton.Margin = new Padding(4, 3, 4, 3);
            viewMotorsButton.Name = "viewMotorsButton";
            viewMotorsButton.Size = new Size(88, 27);
            viewMotorsButton.TabIndex = 16;
            viewMotorsButton.Text = "View motors";
            viewMotorsButton.UseVisualStyleBackColor = true;
            viewMotorsButton.Click += ViewMotorsButtonClick;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(29, 267);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(123, 15);
            label1.TabIndex = 18;
            label1.Text = "Output from program";
            // 
            // monitorSerialButton
            // 
            monitorSerialButton.Location = new Point(139, 0);
            monitorSerialButton.Margin = new Padding(4, 3, 4, 3);
            monitorSerialButton.Name = "monitorSerialButton";
            monitorSerialButton.Size = new Size(78, 27);
            monitorSerialButton.TabIndex = 20;
            monitorSerialButton.Text = "Monitor";
            monitorSerialButton.UseVisualStyleBackColor = true;
            monitorSerialButton.Click += MonitorSerialButtonClick;
            // 
            // ceaseSerialMonitorButton
            // 
            ceaseSerialMonitorButton.Location = new Point(256, 0);
            ceaseSerialMonitorButton.Margin = new Padding(4, 3, 4, 3);
            ceaseSerialMonitorButton.Name = "ceaseSerialMonitorButton";
            ceaseSerialMonitorButton.Size = new Size(59, 27);
            ceaseSerialMonitorButton.TabIndex = 21;
            ceaseSerialMonitorButton.Text = "Cease";
            ceaseSerialMonitorButton.UseVisualStyleBackColor = true;
            ceaseSerialMonitorButton.Click += CeaseSerialMonitorButtonClick;
            // 
            // xcPositionLabel
            // 
            xcPositionLabel.AutoSize = true;
            xcPositionLabel.Location = new Point(19, 18);
            xcPositionLabel.Margin = new Padding(4, 0, 4, 0);
            xcPositionLabel.Name = "xcPositionLabel";
            xcPositionLabel.Size = new Size(43, 15);
            xcPositionLabel.TabIndex = 22;
            xcPositionLabel.Text = "x - pos";
            // 
            // zcPositionLabel
            // 
            zcPositionLabel.AutoSize = true;
            zcPositionLabel.Location = new Point(19, 74);
            zcPositionLabel.Margin = new Padding(4, 0, 4, 0);
            zcPositionLabel.Name = "zcPositionLabel";
            zcPositionLabel.Size = new Size(42, 15);
            zcPositionLabel.TabIndex = 23;
            zcPositionLabel.Text = "z - pos";
            // 
            // ycPositionLabel
            // 
            ycPositionLabel.AutoSize = true;
            ycPositionLabel.Location = new Point(19, 46);
            ycPositionLabel.Margin = new Padding(4, 0, 4, 0);
            ycPositionLabel.Name = "ycPositionLabel";
            ycPositionLabel.Size = new Size(43, 15);
            ycPositionLabel.TabIndex = 24;
            ycPositionLabel.Text = "y - pos";
            // 
            // clearMonitorButton
            // 
            clearMonitorButton.Location = new Point(364, 549);
            clearMonitorButton.Margin = new Padding(4, 3, 4, 3);
            clearMonitorButton.Name = "clearMonitorButton";
            clearMonitorButton.Size = new Size(59, 27);
            clearMonitorButton.TabIndex = 26;
            clearMonitorButton.Text = "Clear";
            clearMonitorButton.UseVisualStyleBackColor = true;
            clearMonitorButton.Click += ClearMonitorButtonClick;
            // 
            // controlTableGroupBox
            // 
            controlTableGroupBox.Controls.Add(ax18Button);
            controlTableGroupBox.Controls.Add(mxButton);
            controlTableGroupBox.Controls.Add(ax12Button);
            controlTableGroupBox.Location = new Point(33, 180);
            controlTableGroupBox.Margin = new Padding(4, 3, 4, 3);
            controlTableGroupBox.Name = "controlTableGroupBox";
            controlTableGroupBox.Padding = new Padding(4, 3, 4, 3);
            controlTableGroupBox.Size = new Size(301, 60);
            controlTableGroupBox.TabIndex = 27;
            controlTableGroupBox.TabStop = false;
            controlTableGroupBox.Text = "Control tables";
            // 
            // ax18Button
            // 
            ax18Button.Location = new Point(83, 20);
            ax18Button.Margin = new Padding(4, 3, 4, 3);
            ax18Button.Name = "ax18Button";
            ax18Button.Size = new Size(64, 27);
            ax18Button.TabIndex = 2;
            ax18Button.Text = "AX-18A";
            ax18Button.UseVisualStyleBackColor = true;
            ax18Button.Click += Ax18ButtonClick;
            // 
            // mxButton
            // 
            mxButton.Location = new Point(154, 20);
            mxButton.Margin = new Padding(4, 3, 4, 3);
            mxButton.Name = "mxButton";
            mxButton.Size = new Size(115, 27);
            mxButton.TabIndex = 1;
            mxButton.Text = "MX-28 | MX-64";
            mxButton.UseVisualStyleBackColor = true;
            mxButton.Click += MxButtonClick;
            // 
            // ax12Button
            // 
            ax12Button.Location = new Point(12, 20);
            ax12Button.Margin = new Padding(4, 3, 4, 3);
            ax12Button.Name = "ax12Button";
            ax12Button.Size = new Size(64, 27);
            ax12Button.TabIndex = 0;
            ax12Button.Text = "AX-12A";
            ax12Button.UseVisualStyleBackColor = true;
            ax12Button.Click += Ax12ButtonClick;
            // 
            // clearLogButton
            // 
            clearLogButton.Location = new Point(201, 261);
            clearLogButton.Margin = new Padding(4, 3, 4, 3);
            clearLogButton.Name = "clearLogButton";
            clearLogButton.Size = new Size(78, 27);
            clearLogButton.TabIndex = 28;
            clearLogButton.Text = "Clear";
            clearLogButton.UseVisualStyleBackColor = true;
            clearLogButton.Click += ClearLogButtonClick;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(xcPositionLabel);
            groupBox1.Controls.Add(zcPositionLabel);
            groupBox1.Controls.Add(ycPositionLabel);
            groupBox1.Location = new Point(9, 36);
            groupBox1.Margin = new Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 3, 4, 3);
            groupBox1.Size = new Size(89, 97);
            groupBox1.TabIndex = 29;
            groupBox1.TabStop = false;
            groupBox1.Text = "Chest";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(xsPositionLabel);
            groupBox2.Controls.Add(zsPositionLabel);
            groupBox2.Controls.Add(ysPositionLabel);
            groupBox2.Location = new Point(94, 36);
            groupBox2.Margin = new Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 3, 4, 3);
            groupBox2.Size = new Size(89, 97);
            groupBox2.TabIndex = 30;
            groupBox2.TabStop = false;
            groupBox2.Text = "Shoulder";
            // 
            // xsPositionLabel
            // 
            xsPositionLabel.AutoSize = true;
            xsPositionLabel.Location = new Point(19, 18);
            xsPositionLabel.Margin = new Padding(4, 0, 4, 0);
            xsPositionLabel.Name = "xsPositionLabel";
            xsPositionLabel.Size = new Size(43, 15);
            xsPositionLabel.TabIndex = 22;
            xsPositionLabel.Text = "x - pos";
            // 
            // zsPositionLabel
            // 
            zsPositionLabel.AutoSize = true;
            zsPositionLabel.Location = new Point(19, 74);
            zsPositionLabel.Margin = new Padding(4, 0, 4, 0);
            zsPositionLabel.Name = "zsPositionLabel";
            zsPositionLabel.Size = new Size(42, 15);
            zsPositionLabel.TabIndex = 23;
            zsPositionLabel.Text = "z - pos";
            // 
            // ysPositionLabel
            // 
            ysPositionLabel.AutoSize = true;
            ysPositionLabel.Location = new Point(19, 46);
            ysPositionLabel.Margin = new Padding(4, 0, 4, 0);
            ysPositionLabel.Name = "ysPositionLabel";
            ysPositionLabel.Size = new Size(43, 15);
            ysPositionLabel.TabIndex = 24;
            ysPositionLabel.Text = "y - pos";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(xwPositionLabel);
            groupBox3.Controls.Add(zwPositionLabel);
            groupBox3.Controls.Add(ywPositionLabel);
            groupBox3.Location = new Point(184, 36);
            groupBox3.Margin = new Padding(4, 3, 4, 3);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(4, 3, 4, 3);
            groupBox3.Size = new Size(100, 97);
            groupBox3.TabIndex = 31;
            groupBox3.TabStop = false;
            groupBox3.Text = "Waist";
            // 
            // xwPositionLabel
            // 
            xwPositionLabel.AutoSize = true;
            xwPositionLabel.Location = new Point(19, 18);
            xwPositionLabel.Margin = new Padding(4, 0, 4, 0);
            xwPositionLabel.Name = "xwPositionLabel";
            xwPositionLabel.Size = new Size(43, 15);
            xwPositionLabel.TabIndex = 22;
            xwPositionLabel.Text = "x - pos";
            // 
            // zwPositionLabel
            // 
            zwPositionLabel.AutoSize = true;
            zwPositionLabel.Location = new Point(19, 74);
            zwPositionLabel.Margin = new Padding(4, 0, 4, 0);
            zwPositionLabel.Name = "zwPositionLabel";
            zwPositionLabel.Size = new Size(42, 15);
            zwPositionLabel.TabIndex = 23;
            zwPositionLabel.Text = "z - pos";
            // 
            // ywPositionLabel
            // 
            ywPositionLabel.AutoSize = true;
            ywPositionLabel.Location = new Point(19, 46);
            ywPositionLabel.Margin = new Padding(4, 0, 4, 0);
            ywPositionLabel.Name = "ywPositionLabel";
            ywPositionLabel.Size = new Size(43, 15);
            ywPositionLabel.TabIndex = 24;
            ywPositionLabel.Text = "y - pos";
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 591);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 16, 0);
            statusStrip1.Size = new Size(716, 22);
            statusStrip1.TabIndex = 32;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(19, 17);
            toolStripStatusLabel.Text = "....";
            // 
            // torqueStatusIndicator
            // 
            torqueStatusIndicator.FlasherButtonColorOff = SystemColors.Control;
            torqueStatusIndicator.FlasherButtonColorOn = Color.PaleVioletRed;
            torqueStatusIndicator.FlashNumber = 0;
            torqueStatusIndicator.Location = new Point(109, 556);
            torqueStatusIndicator.Name = "torqueStatusIndicator";
            torqueStatusIndicator.Size = new Size(23, 23);
            torqueStatusIndicator.TabIndex = 33;
            torqueStatusIndicator.Text = " ";
            torqueStatusIndicator.UseVisualStyleBackColor = true;
            torqueStatusIndicator.Click += TorqueStatusIndicatorClick;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(26, 560);
            label4.Name = "label4";
            label4.Size = new Size(77, 15);
            label4.TabIndex = 34;
            label4.Text = "Torque status";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(groupBox1);
            groupBox4.Controls.Add(groupBox2);
            groupBox4.Controls.Add(groupBox3);
            groupBox4.Controls.Add(monitorSerialButton);
            groupBox4.Controls.Add(ceaseSerialMonitorButton);
            groupBox4.Location = new Point(364, 392);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(335, 151);
            groupBox4.TabIndex = 35;
            groupBox4.TabStop = false;
            groupBox4.Text = "Serial monitor";
            // 
            // TemplaterForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLight;
            ClientSize = new Size(716, 613);
            Controls.Add(groupBox4);
            Controls.Add(label4);
            Controls.Add(torqueStatusIndicator);
            Controls.Add(statusStrip1);
            Controls.Add(clearLogButton);
            Controls.Add(controlTableGroupBox);
            Controls.Add(clearMonitorButton);
            Controls.Add(label1);
            Controls.Add(viewMotorsButton);
            Controls.Add(basicAnimationsGroupBox);
            Controls.Add(motorControlGroupBox);
            Controls.Add(torqueGroupBox);
            Controls.Add(closeButton);
            Controls.Add(logWindow);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MaximumSize = new Size(732, 652);
            MinimumSize = new Size(732, 652);
            Name = "TemplaterForm";
            Text = "Joi animation templater";
            FormClosing += MainForm_FormClosing;
            torqueGroupBox.ResumeLayout(false);
            motorControlGroupBox.ResumeLayout(false);
            motorControlGroupBox.PerformLayout();
            basicAnimationsGroupBox.ResumeLayout(false);
            controlTableGroupBox.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            groupBox4.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button torqueOnButton;
        private System.Windows.Forms.RichTextBox logWindow;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button torqueOffButton;
        private System.Windows.Forms.Button raiseArmButton;
        private System.Windows.Forms.Button liftLegButton;
        private System.Windows.Forms.Button turnHeadButton;
        private System.Windows.Forms.ComboBox motorSelectionList;
        private System.Windows.Forms.Button getPositionButton;
        private System.Windows.Forms.ComboBox areaMotorSelection;
        private System.Windows.Forms.GroupBox torqueGroupBox;
        private System.Windows.Forms.GroupBox motorControlGroupBox;
        private System.Windows.Forms.Button granularTorqueOffButton;
        private System.Windows.Forms.Button granularTorqueOnButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button viewTableButton;
        private System.Windows.Forms.GroupBox basicAnimationsGroupBox;
        private System.Windows.Forms.Button viewMotorsButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button monitorSerialButton;
        private System.Windows.Forms.Button ceaseSerialMonitorButton;
        private System.Windows.Forms.Label ycPositionLabel;
        private System.Windows.Forms.Label zcPositionLabel;
        private System.Windows.Forms.Label xcPositionLabel;
        private System.Windows.Forms.Button clearMonitorButton;
        private System.Windows.Forms.Button threeStepsBackwardButton;
        private System.Windows.Forms.Button threeStepsForwardButton;
        private System.Windows.Forms.GroupBox controlTableGroupBox;
        private System.Windows.Forms.Button ax18Button;
        private System.Windows.Forms.Button mxButton;
        private System.Windows.Forms.Button ax12Button;
        private System.Windows.Forms.Button clearLogButton;
        public System.Windows.Forms.Label notification;
        private System.Windows.Forms.Button engageDictionaryRoutine;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label xsPositionLabel;
        private System.Windows.Forms.Label zsPositionLabel;
        private System.Windows.Forms.Label ysPositionLabel;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label xwPositionLabel;
        private System.Windows.Forms.Label zwPositionLabel;
        private System.Windows.Forms.Label ywPositionLabel;
        private System.Windows.Forms.ComboBox sideComboBox;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private Controls.FlashButton storePositionButton;
        private Controls.FlashButton positionNowFlashButton;
        private Controls.FlashButton torqueStatusIndicator;
        private Label label4;
        private GroupBox groupBox4;
        private ComboBox limbComboBox;
    }
}

