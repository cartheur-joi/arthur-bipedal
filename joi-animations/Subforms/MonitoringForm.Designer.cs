namespace DynamixelWizard.SubForms
{
    partial class MonitoringForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitoringForm));
            this.closeButton = new System.Windows.Forms.Button();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.head_y_value = new System.Windows.Forms.Label();
            this.l_shoulder_y_value = new System.Windows.Forms.Label();
            this.l_shoulder_x_value = new System.Windows.Forms.Label();
            this.l_arm_z_value = new System.Windows.Forms.Label();
            this.l_elbow_y_value = new System.Windows.Forms.Label();
            this.bust_x_value = new System.Windows.Forms.Label();
            this.abs_z_value = new System.Windows.Forms.Label();
            this.abs_x_value = new System.Windows.Forms.Label();
            this.l_hip_z_value = new System.Windows.Forms.Label();
            this.l_hip_y_value = new System.Windows.Forms.Label();
            this.l_hip_x_value = new System.Windows.Forms.Label();
            this.l_knee_y_value = new System.Windows.Forms.Label();
            this.l_ankle_y_value = new System.Windows.Forms.Label();
            this.r_ankle_y_value = new System.Windows.Forms.Label();
            this.r_knee_y_value = new System.Windows.Forms.Label();
            this.r_hip_x_value = new System.Windows.Forms.Label();
            this.r_hip_y_value = new System.Windows.Forms.Label();
            this.r_hip_z_value = new System.Windows.Forms.Label();
            this.abs_y_value = new System.Windows.Forms.Label();
            this.bust_y_value = new System.Windows.Forms.Label();
            this.r_elbow_y_value = new System.Windows.Forms.Label();
            this.head_z_value = new System.Windows.Forms.Label();
            this.r_arm_z_value = new System.Windows.Forms.Label();
            this.r_shoulder_x_value = new System.Windows.Forms.Label();
            this.r_shoulder_y_value = new System.Windows.Forms.Label();
            this.StartMonitoringButton = new System.Windows.Forms.Button();
            this.StopMonitoringButton = new System.Windows.Forms.Button();
            this.head_y_overload = new DynamixelWizard.Controls.FlashButton();
            this.l_ankle_y_overload = new DynamixelWizard.Controls.FlashButton();
            this.abs_z_overload = new DynamixelWizard.Controls.FlashButton();
            this.abs_x_overload = new DynamixelWizard.Controls.FlashButton();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(498, 518);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 4;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButtonClick);
            // 
            // pictureBox
            // 
            this.pictureBox.Image = global::DynamixelWizard.Properties.Resources.motors_on_robot;
            this.pictureBox.Location = new System.Drawing.Point(29, 12);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(544, 500);
            this.pictureBox.TabIndex = 5;
            this.pictureBox.TabStop = false;
            // 
            // head_y_value
            // 
            this.head_y_value.AutoSize = true;
            this.head_y_value.Location = new System.Drawing.Point(442, 28);
            this.head_y_value.Name = "head_y_value";
            this.head_y_value.Size = new System.Drawing.Size(13, 13);
            this.head_y_value.TabIndex = 8;
            this.head_y_value.Text = "0";
            // 
            // l_shoulder_y_value
            // 
            this.l_shoulder_y_value.AutoSize = true;
            this.l_shoulder_y_value.Location = new System.Drawing.Point(492, 52);
            this.l_shoulder_y_value.Name = "l_shoulder_y_value";
            this.l_shoulder_y_value.Size = new System.Drawing.Size(13, 13);
            this.l_shoulder_y_value.TabIndex = 9;
            this.l_shoulder_y_value.Text = "0";
            // 
            // l_shoulder_x_value
            // 
            this.l_shoulder_x_value.AutoSize = true;
            this.l_shoulder_x_value.Location = new System.Drawing.Point(523, 75);
            this.l_shoulder_x_value.Name = "l_shoulder_x_value";
            this.l_shoulder_x_value.Size = new System.Drawing.Size(13, 13);
            this.l_shoulder_x_value.TabIndex = 10;
            this.l_shoulder_x_value.Text = "0";
            // 
            // l_arm_z_value
            // 
            this.l_arm_z_value.AutoSize = true;
            this.l_arm_z_value.Location = new System.Drawing.Point(520, 97);
            this.l_arm_z_value.Name = "l_arm_z_value";
            this.l_arm_z_value.Size = new System.Drawing.Size(13, 13);
            this.l_arm_z_value.TabIndex = 11;
            this.l_arm_z_value.Text = "0";
            // 
            // l_elbow_y_value
            // 
            this.l_elbow_y_value.AutoSize = true;
            this.l_elbow_y_value.Location = new System.Drawing.Point(507, 140);
            this.l_elbow_y_value.Name = "l_elbow_y_value";
            this.l_elbow_y_value.Size = new System.Drawing.Size(13, 13);
            this.l_elbow_y_value.TabIndex = 12;
            this.l_elbow_y_value.Text = "0";
            // 
            // bust_x_value
            // 
            this.bust_x_value.AutoSize = true;
            this.bust_x_value.Location = new System.Drawing.Point(450, 178);
            this.bust_x_value.Name = "bust_x_value";
            this.bust_x_value.Size = new System.Drawing.Size(13, 13);
            this.bust_x_value.TabIndex = 13;
            this.bust_x_value.Text = "0";
            // 
            // abs_z_value
            // 
            this.abs_z_value.AutoSize = true;
            this.abs_z_value.Location = new System.Drawing.Point(450, 205);
            this.abs_z_value.Name = "abs_z_value";
            this.abs_z_value.Size = new System.Drawing.Size(13, 13);
            this.abs_z_value.TabIndex = 14;
            this.abs_z_value.Text = "0";
            // 
            // abs_x_value
            // 
            this.abs_x_value.AutoSize = true;
            this.abs_x_value.Location = new System.Drawing.Point(450, 231);
            this.abs_x_value.Name = "abs_x_value";
            this.abs_x_value.Size = new System.Drawing.Size(13, 13);
            this.abs_x_value.TabIndex = 15;
            this.abs_x_value.Text = "0";
            // 
            // l_hip_z_value
            // 
            this.l_hip_z_value.AutoSize = true;
            this.l_hip_z_value.Location = new System.Drawing.Point(470, 257);
            this.l_hip_z_value.Name = "l_hip_z_value";
            this.l_hip_z_value.Size = new System.Drawing.Size(13, 13);
            this.l_hip_z_value.TabIndex = 16;
            this.l_hip_z_value.Text = "0";
            // 
            // l_hip_y_value
            // 
            this.l_hip_y_value.AutoSize = true;
            this.l_hip_y_value.Location = new System.Drawing.Point(470, 293);
            this.l_hip_y_value.Name = "l_hip_y_value";
            this.l_hip_y_value.Size = new System.Drawing.Size(13, 13);
            this.l_hip_y_value.TabIndex = 17;
            this.l_hip_y_value.Text = "0";
            // 
            // l_hip_x_value
            // 
            this.l_hip_x_value.AutoSize = true;
            this.l_hip_x_value.Location = new System.Drawing.Point(470, 328);
            this.l_hip_x_value.Name = "l_hip_x_value";
            this.l_hip_x_value.Size = new System.Drawing.Size(13, 13);
            this.l_hip_x_value.TabIndex = 18;
            this.l_hip_x_value.Text = "0";
            // 
            // l_knee_y_value
            // 
            this.l_knee_y_value.AutoSize = true;
            this.l_knee_y_value.Location = new System.Drawing.Point(486, 399);
            this.l_knee_y_value.Name = "l_knee_y_value";
            this.l_knee_y_value.Size = new System.Drawing.Size(13, 13);
            this.l_knee_y_value.TabIndex = 19;
            this.l_knee_y_value.Text = "0";
            // 
            // l_ankle_y_value
            // 
            this.l_ankle_y_value.AutoSize = true;
            this.l_ankle_y_value.Location = new System.Drawing.Point(486, 469);
            this.l_ankle_y_value.Name = "l_ankle_y_value";
            this.l_ankle_y_value.Size = new System.Drawing.Size(13, 13);
            this.l_ankle_y_value.TabIndex = 20;
            this.l_ankle_y_value.Text = "0";
            // 
            // r_ankle_y_value
            // 
            this.r_ankle_y_value.AutoSize = true;
            this.r_ankle_y_value.Location = new System.Drawing.Point(54, 469);
            this.r_ankle_y_value.Name = "r_ankle_y_value";
            this.r_ankle_y_value.Size = new System.Drawing.Size(13, 13);
            this.r_ankle_y_value.TabIndex = 21;
            this.r_ankle_y_value.Text = "0";
            // 
            // r_knee_y_value
            // 
            this.r_knee_y_value.AutoSize = true;
            this.r_knee_y_value.Location = new System.Drawing.Point(54, 399);
            this.r_knee_y_value.Name = "r_knee_y_value";
            this.r_knee_y_value.Size = new System.Drawing.Size(13, 13);
            this.r_knee_y_value.TabIndex = 22;
            this.r_knee_y_value.Text = "0";
            // 
            // r_hip_x_value
            // 
            this.r_hip_x_value.AutoSize = true;
            this.r_hip_x_value.Location = new System.Drawing.Point(54, 328);
            this.r_hip_x_value.Name = "r_hip_x_value";
            this.r_hip_x_value.Size = new System.Drawing.Size(13, 13);
            this.r_hip_x_value.TabIndex = 23;
            this.r_hip_x_value.Text = "0";
            // 
            // r_hip_y_value
            // 
            this.r_hip_y_value.AutoSize = true;
            this.r_hip_y_value.Location = new System.Drawing.Point(54, 293);
            this.r_hip_y_value.Name = "r_hip_y_value";
            this.r_hip_y_value.Size = new System.Drawing.Size(13, 13);
            this.r_hip_y_value.TabIndex = 24;
            this.r_hip_y_value.Text = "0";
            // 
            // r_hip_z_value
            // 
            this.r_hip_z_value.AutoSize = true;
            this.r_hip_z_value.Location = new System.Drawing.Point(54, 257);
            this.r_hip_z_value.Name = "r_hip_z_value";
            this.r_hip_z_value.Size = new System.Drawing.Size(13, 13);
            this.r_hip_z_value.TabIndex = 25;
            this.r_hip_z_value.Text = "0";
            // 
            // abs_y_value
            // 
            this.abs_y_value.AutoSize = true;
            this.abs_y_value.Location = new System.Drawing.Point(54, 219);
            this.abs_y_value.Name = "abs_y_value";
            this.abs_y_value.Size = new System.Drawing.Size(13, 13);
            this.abs_y_value.TabIndex = 26;
            this.abs_y_value.Text = "0";
            // 
            // bust_y_value
            // 
            this.bust_y_value.AutoSize = true;
            this.bust_y_value.Location = new System.Drawing.Point(54, 193);
            this.bust_y_value.Name = "bust_y_value";
            this.bust_y_value.Size = new System.Drawing.Size(13, 13);
            this.bust_y_value.TabIndex = 27;
            this.bust_y_value.Text = "0";
            // 
            // r_elbow_y_value
            // 
            this.r_elbow_y_value.AutoSize = true;
            this.r_elbow_y_value.Location = new System.Drawing.Point(34, 142);
            this.r_elbow_y_value.Name = "r_elbow_y_value";
            this.r_elbow_y_value.Size = new System.Drawing.Size(13, 13);
            this.r_elbow_y_value.TabIndex = 28;
            this.r_elbow_y_value.Text = "0";
            // 
            // head_z_value
            // 
            this.head_z_value.AutoSize = true;
            this.head_z_value.Location = new System.Drawing.Point(163, 165);
            this.head_z_value.Name = "head_z_value";
            this.head_z_value.Size = new System.Drawing.Size(13, 13);
            this.head_z_value.TabIndex = 29;
            this.head_z_value.Text = "0";
            // 
            // r_arm_z_value
            // 
            this.r_arm_z_value.AutoSize = true;
            this.r_arm_z_value.Location = new System.Drawing.Point(34, 102);
            this.r_arm_z_value.Name = "r_arm_z_value";
            this.r_arm_z_value.Size = new System.Drawing.Size(13, 13);
            this.r_arm_z_value.TabIndex = 30;
            this.r_arm_z_value.Text = "0";
            // 
            // r_shoulder_x_value
            // 
            this.r_shoulder_x_value.AutoSize = true;
            this.r_shoulder_x_value.Location = new System.Drawing.Point(34, 75);
            this.r_shoulder_x_value.Name = "r_shoulder_x_value";
            this.r_shoulder_x_value.Size = new System.Drawing.Size(13, 13);
            this.r_shoulder_x_value.TabIndex = 31;
            this.r_shoulder_x_value.Text = "0";
            // 
            // r_shoulder_y_value
            // 
            this.r_shoulder_y_value.AutoSize = true;
            this.r_shoulder_y_value.Location = new System.Drawing.Point(34, 51);
            this.r_shoulder_y_value.Name = "r_shoulder_y_value";
            this.r_shoulder_y_value.Size = new System.Drawing.Size(13, 13);
            this.r_shoulder_y_value.TabIndex = 32;
            this.r_shoulder_y_value.Text = "0";
            // 
            // StartMonitoringButton
            // 
            this.StartMonitoringButton.Location = new System.Drawing.Point(37, 518);
            this.StartMonitoringButton.Name = "StartMonitoringButton";
            this.StartMonitoringButton.Size = new System.Drawing.Size(104, 23);
            this.StartMonitoringButton.TabIndex = 33;
            this.StartMonitoringButton.Text = "Start monitoring";
            this.StartMonitoringButton.UseVisualStyleBackColor = true;
            this.StartMonitoringButton.Click += new System.EventHandler(this.StartMonitoringButtonClick);
            // 
            // StopMonitoringButton
            // 
            this.StopMonitoringButton.Location = new System.Drawing.Point(147, 518);
            this.StopMonitoringButton.Name = "StopMonitoringButton";
            this.StopMonitoringButton.Size = new System.Drawing.Size(104, 23);
            this.StopMonitoringButton.TabIndex = 34;
            this.StopMonitoringButton.Text = "Stop monitoring";
            this.StopMonitoringButton.UseVisualStyleBackColor = true;
            this.StopMonitoringButton.Click += new System.EventHandler(this.StopMonitoringButtonClick);
            // 
            // head_y_overload
            // 
            this.head_y_overload.FlasherButtonColorOff = System.Drawing.SystemColors.Control;
            this.head_y_overload.FlasherButtonColorOn = System.Drawing.Color.LightGreen;
            this.head_y_overload.FlashNumber = 0;
            this.head_y_overload.Location = new System.Drawing.Point(407, 27);
            this.head_y_overload.Name = "head_y_overload";
            this.head_y_overload.Size = new System.Drawing.Size(23, 16);
            this.head_y_overload.TabIndex = 52;
            this.head_y_overload.UseVisualStyleBackColor = true;
            // 
            // l_ankle_y_overload
            // 
            this.l_ankle_y_overload.FlasherButtonColorOff = System.Drawing.SystemColors.Control;
            this.l_ankle_y_overload.FlasherButtonColorOn = System.Drawing.Color.LightGreen;
            this.l_ankle_y_overload.FlashNumber = 0;
            this.l_ankle_y_overload.Location = new System.Drawing.Point(453, 467);
            this.l_ankle_y_overload.Name = "l_ankle_y_overload";
            this.l_ankle_y_overload.Size = new System.Drawing.Size(23, 16);
            this.l_ankle_y_overload.TabIndex = 53;
            this.l_ankle_y_overload.UseVisualStyleBackColor = true;
            // 
            // abs_z_overload
            // 
            this.abs_z_overload.FlasherButtonColorOff = System.Drawing.SystemColors.Control;
            this.abs_z_overload.FlasherButtonColorOn = System.Drawing.Color.LightGreen;
            this.abs_z_overload.FlashNumber = 0;
            this.abs_z_overload.Location = new System.Drawing.Point(411, 205);
            this.abs_z_overload.Name = "abs_z_overload";
            this.abs_z_overload.Size = new System.Drawing.Size(23, 16);
            this.abs_z_overload.TabIndex = 54;
            this.abs_z_overload.UseVisualStyleBackColor = true;
            // 
            // abs_x_overload
            // 
            this.abs_x_overload.FlasherButtonColorOff = System.Drawing.SystemColors.Control;
            this.abs_x_overload.FlasherButtonColorOn = System.Drawing.Color.LightGreen;
            this.abs_x_overload.FlashNumber = 0;
            this.abs_x_overload.Location = new System.Drawing.Point(410, 232);
            this.abs_x_overload.Name = "abs_x_overload";
            this.abs_x_overload.Size = new System.Drawing.Size(23, 16);
            this.abs_x_overload.TabIndex = 55;
            this.abs_x_overload.UseVisualStyleBackColor = true;
            // 
            // MonitoringForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(608, 558);
            this.Controls.Add(this.abs_x_overload);
            this.Controls.Add(this.abs_z_overload);
            this.Controls.Add(this.l_ankle_y_overload);
            this.Controls.Add(this.head_y_overload);
            this.Controls.Add(this.StopMonitoringButton);
            this.Controls.Add(this.StartMonitoringButton);
            this.Controls.Add(this.r_shoulder_y_value);
            this.Controls.Add(this.r_shoulder_x_value);
            this.Controls.Add(this.r_arm_z_value);
            this.Controls.Add(this.head_z_value);
            this.Controls.Add(this.r_elbow_y_value);
            this.Controls.Add(this.bust_y_value);
            this.Controls.Add(this.abs_y_value);
            this.Controls.Add(this.r_hip_z_value);
            this.Controls.Add(this.r_hip_y_value);
            this.Controls.Add(this.r_hip_x_value);
            this.Controls.Add(this.r_knee_y_value);
            this.Controls.Add(this.r_ankle_y_value);
            this.Controls.Add(this.l_ankle_y_value);
            this.Controls.Add(this.l_knee_y_value);
            this.Controls.Add(this.l_hip_x_value);
            this.Controls.Add(this.l_hip_y_value);
            this.Controls.Add(this.l_hip_z_value);
            this.Controls.Add(this.abs_x_value);
            this.Controls.Add(this.abs_z_value);
            this.Controls.Add(this.bust_x_value);
            this.Controls.Add(this.l_elbow_y_value);
            this.Controls.Add(this.l_arm_z_value);
            this.Controls.Add(this.l_shoulder_x_value);
            this.Controls.Add(this.l_shoulder_y_value);
            this.Controls.Add(this.head_y_value);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.closeButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(624, 597);
            this.MinimumSize = new System.Drawing.Size(624, 597);
            this.Name = "MonitoringForm";
            this.Text = "Monitor david\'s motor performance";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label head_y_value;
        private System.Windows.Forms.Label l_shoulder_y_value;
        private System.Windows.Forms.Label l_shoulder_x_value;
        private System.Windows.Forms.Label l_arm_z_value;
        private System.Windows.Forms.Label l_elbow_y_value;
        private System.Windows.Forms.Label bust_x_value;
        private System.Windows.Forms.Label abs_z_value;
        private System.Windows.Forms.Label abs_x_value;
        private System.Windows.Forms.Label l_hip_z_value;
        private System.Windows.Forms.Label l_hip_y_value;
        private System.Windows.Forms.Label l_hip_x_value;
        private System.Windows.Forms.Label l_knee_y_value;
        private System.Windows.Forms.Label l_ankle_y_value;
        private System.Windows.Forms.Label r_ankle_y_value;
        private System.Windows.Forms.Label r_knee_y_value;
        private System.Windows.Forms.Label r_hip_x_value;
        private System.Windows.Forms.Label r_hip_y_value;
        private System.Windows.Forms.Label r_hip_z_value;
        private System.Windows.Forms.Label abs_y_value;
        private System.Windows.Forms.Label bust_y_value;
        private System.Windows.Forms.Label r_elbow_y_value;
        private System.Windows.Forms.Label head_z_value;
        private System.Windows.Forms.Label r_arm_z_value;
        private System.Windows.Forms.Label r_shoulder_x_value;
        private System.Windows.Forms.Label r_shoulder_y_value;
        private System.Windows.Forms.Button StartMonitoringButton;
        private System.Windows.Forms.Button StopMonitoringButton;
        private Controls.FlashButton head_y_overload;
        private Controls.FlashButton l_ankle_y_overload;
        private Controls.FlashButton abs_z_overload;
        private Controls.FlashButton abs_x_overload;
    }
}