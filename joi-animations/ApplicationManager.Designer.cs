namespace Cartheur.Animation.Joi
{
    partial class ApplicationManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApplicationManager));
            groupBox1 = new GroupBox();
            label2 = new Label();
            monitoringFormButton = new Button();
            demoFormButton = new Button();
            NotificationLabel = new Label();
            TemplaterLaunchButton = new Button();
            ControlKeypadLaunchButton = new Button();
            RobotControlLaunchButton = new Button();
            AnimationTrainingLaunchButton = new Button();
            QuitApplicationButton = new Button();
            groupBox2 = new GroupBox();
            CameraBox = new PictureBox();
            StartCameraButton = new Button();
            PopoutCameraButton = new Button();
            label1 = new Label();
            timeDisplayBox = new RichTextBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)CameraBox).BeginInit();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(monitoringFormButton);
            groupBox1.Controls.Add(demoFormButton);
            groupBox1.Controls.Add(NotificationLabel);
            groupBox1.Controls.Add(TemplaterLaunchButton);
            groupBox1.Controls.Add(ControlKeypadLaunchButton);
            groupBox1.Controls.Add(RobotControlLaunchButton);
            groupBox1.Controls.Add(AnimationTrainingLaunchButton);
            groupBox1.Location = new Point(14, 14);
            groupBox1.Margin = new Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 3, 4, 3);
            groupBox1.Size = new Size(233, 455);
            groupBox1.TabIndex = 31;
            groupBox1.TabStop = false;
            groupBox1.Text = "Application manager";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(10, 252);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(51, 15);
            label2.TabIndex = 36;
            label2.Text = "--> here";
            // 
            // monitoringFormButton
            // 
            monitoringFormButton.Location = new Point(68, 368);
            monitoringFormButton.Margin = new Padding(4, 3, 4, 3);
            monitoringFormButton.Name = "monitoringFormButton";
            monitoringFormButton.Size = new Size(88, 47);
            monitoringFormButton.TabIndex = 35;
            monitoringFormButton.Text = "Monitoring";
            monitoringFormButton.UseVisualStyleBackColor = true;
            monitoringFormButton.Click += MonitoringFormButtonClick;
            // 
            // demoFormButton
            // 
            demoFormButton.Location = new Point(68, 302);
            demoFormButton.Margin = new Padding(4, 3, 4, 3);
            demoFormButton.Name = "demoFormButton";
            demoFormButton.Size = new Size(88, 47);
            demoFormButton.TabIndex = 34;
            demoFormButton.Text = "Demos";
            demoFormButton.UseVisualStyleBackColor = true;
            demoFormButton.Click += DemoFormButtonClick;
            // 
            // NotificationLabel
            // 
            NotificationLabel.AutoSize = true;
            NotificationLabel.Location = new Point(7, 425);
            NotificationLabel.Margin = new Padding(4, 0, 4, 0);
            NotificationLabel.Name = "NotificationLabel";
            NotificationLabel.Size = new Size(22, 15);
            NotificationLabel.TabIndex = 33;
            NotificationLabel.Text = "---";
            // 
            // TemplaterLaunchButton
            // 
            TemplaterLaunchButton.Location = new Point(68, 235);
            TemplaterLaunchButton.Margin = new Padding(4, 3, 4, 3);
            TemplaterLaunchButton.Name = "TemplaterLaunchButton";
            TemplaterLaunchButton.Size = new Size(88, 47);
            TemplaterLaunchButton.TabIndex = 31;
            TemplaterLaunchButton.Text = "Templater";
            TemplaterLaunchButton.UseVisualStyleBackColor = true;
            TemplaterLaunchButton.Click += TemplaterLaunchButtonClick;
            // 
            // ControlKeypadLaunchButton
            // 
            ControlKeypadLaunchButton.Location = new Point(68, 168);
            ControlKeypadLaunchButton.Margin = new Padding(4, 3, 4, 3);
            ControlKeypadLaunchButton.Name = "ControlKeypadLaunchButton";
            ControlKeypadLaunchButton.Size = new Size(88, 47);
            ControlKeypadLaunchButton.TabIndex = 30;
            ControlKeypadLaunchButton.Text = "Control keypad";
            ControlKeypadLaunchButton.UseVisualStyleBackColor = true;
            ControlKeypadLaunchButton.Click += ControlKeypadLaunchButtonClick;
            // 
            // RobotControlLaunchButton
            // 
            RobotControlLaunchButton.Location = new Point(68, 36);
            RobotControlLaunchButton.Margin = new Padding(4, 3, 4, 3);
            RobotControlLaunchButton.Name = "RobotControlLaunchButton";
            RobotControlLaunchButton.Size = new Size(88, 47);
            RobotControlLaunchButton.TabIndex = 15;
            RobotControlLaunchButton.Text = "Robot control";
            RobotControlLaunchButton.UseVisualStyleBackColor = true;
            RobotControlLaunchButton.Click += RobotControlFormButtonClick;
            // 
            // AnimationTrainingLaunchButton
            // 
            AnimationTrainingLaunchButton.Location = new Point(68, 102);
            AnimationTrainingLaunchButton.Margin = new Padding(4, 3, 4, 3);
            AnimationTrainingLaunchButton.Name = "AnimationTrainingLaunchButton";
            AnimationTrainingLaunchButton.Size = new Size(88, 47);
            AnimationTrainingLaunchButton.TabIndex = 29;
            AnimationTrainingLaunchButton.Text = "Animation training";
            AnimationTrainingLaunchButton.UseVisualStyleBackColor = true;
            AnimationTrainingLaunchButton.Click += AnimationTrainingButtonClick;
            // 
            // QuitApplicationButton
            // 
            QuitApplicationButton.Location = new Point(386, 421);
            QuitApplicationButton.Margin = new Padding(4, 3, 4, 3);
            QuitApplicationButton.Name = "QuitApplicationButton";
            QuitApplicationButton.Size = new Size(88, 47);
            QuitApplicationButton.TabIndex = 32;
            QuitApplicationButton.Text = "Quit";
            QuitApplicationButton.UseVisualStyleBackColor = true;
            QuitApplicationButton.Click += QuitApplicationButtonClick;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(CameraBox);
            groupBox2.Location = new Point(255, 15);
            groupBox2.Margin = new Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 3, 4, 3);
            groupBox2.Size = new Size(218, 215);
            groupBox2.TabIndex = 33;
            groupBox2.TabStop = false;
            groupBox2.Text = "Camera";
            // 
            // CameraBox
            // 
            CameraBox.Location = new Point(8, 23);
            CameraBox.Margin = new Padding(4, 3, 4, 3);
            CameraBox.Name = "CameraBox";
            CameraBox.Size = new Size(203, 185);
            CameraBox.TabIndex = 0;
            CameraBox.TabStop = false;
            // 
            // StartCameraButton
            // 
            StartCameraButton.Location = new Point(264, 237);
            StartCameraButton.Margin = new Padding(4, 3, 4, 3);
            StartCameraButton.Name = "StartCameraButton";
            StartCameraButton.Size = new Size(88, 27);
            StartCameraButton.TabIndex = 34;
            StartCameraButton.Text = "Start";
            StartCameraButton.UseVisualStyleBackColor = true;
            StartCameraButton.Click += StartCameraButtonClick;
            // 
            // PopoutCameraButton
            // 
            PopoutCameraButton.Location = new Point(379, 237);
            PopoutCameraButton.Margin = new Padding(4, 3, 4, 3);
            PopoutCameraButton.Name = "PopoutCameraButton";
            PopoutCameraButton.Size = new Size(88, 27);
            PopoutCameraButton.TabIndex = 35;
            PopoutCameraButton.Text = "Popout";
            PopoutCameraButton.UseVisualStyleBackColor = true;
            PopoutCameraButton.Click += PopoutCameraButtonClick;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(275, 299);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(70, 15);
            label1.TabIndex = 36;
            label1.Text = "Time active:";
            // 
            // timeDisplayBox
            // 
            timeDisplayBox.Location = new Point(279, 318);
            timeDisplayBox.Margin = new Padding(4, 3, 4, 3);
            timeDisplayBox.Name = "timeDisplayBox";
            timeDisplayBox.Size = new Size(116, 44);
            timeDisplayBox.TabIndex = 37;
            timeDisplayBox.Text = "";
            // 
            // ApplicationManager
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(488, 482);
            Controls.Add(timeDisplayBox);
            Controls.Add(label1);
            Controls.Add(PopoutCameraButton);
            Controls.Add(StartCameraButton);
            Controls.Add(groupBox2);
            Controls.Add(QuitApplicationButton);
            Controls.Add(groupBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MaximumSize = new Size(504, 521);
            MinimizeBox = false;
            MinimumSize = new Size(504, 521);
            Name = "ApplicationManager";
            Text = "Application Manager";
            FormClosing += ApplicationManagerFormClosing;
            KeyDown += ApplicationManagerKeyDown;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)CameraBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button ControlKeypadLaunchButton;
        private System.Windows.Forms.Button RobotControlLaunchButton;
        private System.Windows.Forms.Button AnimationTrainingLaunchButton;
        private System.Windows.Forms.Button QuitApplicationButton;
        private System.Windows.Forms.Button TemplaterLaunchButton;
        private System.Windows.Forms.Label NotificationLabel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.PictureBox CameraBox;
        private System.Windows.Forms.Button StartCameraButton;
        private System.Windows.Forms.Button PopoutCameraButton;
        private System.Windows.Forms.Button monitoringFormButton;
        private System.Windows.Forms.Button demoFormButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox timeDisplayBox;
        private System.Windows.Forms.Label label2;
    }
}