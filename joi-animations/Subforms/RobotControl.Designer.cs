namespace DynamixelWizard.SubForms
{
    partial class RobotControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RobotControl));
            this.engageButton = new System.Windows.Forms.Button();
            this.motorEngagemenetList = new System.Windows.Forms.RichTextBox();
            this.motorSelectionList = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.goalPosition = new System.Windows.Forms.TextBox();
            this.addToListButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.saveListButton = new System.Windows.Forms.Button();
            this.notificationLabel = new System.Windows.Forms.Label();
            this.loadDictionaryButton = new System.Windows.Forms.Button();
            this.clearBoxButton = new System.Windows.Forms.Button();
            this.fileNameTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // engageButton
            // 
            this.engageButton.Location = new System.Drawing.Point(101, 170);
            this.engageButton.Name = "engageButton";
            this.engageButton.Size = new System.Drawing.Size(110, 23);
            this.engageButton.TabIndex = 0;
            this.engageButton.Text = "Create dictionary";
            this.engageButton.UseVisualStyleBackColor = true;
            this.engageButton.Click += new System.EventHandler(this.EngageButtonClick);
            // 
            // motorEngagemenetList
            // 
            this.motorEngagemenetList.Location = new System.Drawing.Point(235, 32);
            this.motorEngagemenetList.Name = "motorEngagemenetList";
            this.motorEngagemenetList.Size = new System.Drawing.Size(169, 339);
            this.motorEngagemenetList.TabIndex = 1;
            this.motorEngagemenetList.Text = "";
            // 
            // motorSelectionList
            // 
            this.motorSelectionList.FormattingEnabled = true;
            this.motorSelectionList.Items.AddRange(new object[] {
            "l_hip_x",
            "l_hip_z",
            "l_hip_y",
            "l_knee_y",
            "l_ankle_y",
            "r_hip_x",
            "r_hip_z",
            "r_hip_y",
            "r_knee_y",
            "r_ankle_y",
            "abs_y",
            "abs_x",
            "abs_z",
            "bust_y",
            "bust_x",
            "l_shoulder_y",
            "l_shoulder_x",
            "l_arm_z",
            "l_elbow_y",
            "r_shoulder_y",
            "r_shoulder_x",
            "r_arm_z",
            "r_elbow_y"});
            this.motorSelectionList.Location = new System.Drawing.Point(12, 53);
            this.motorSelectionList.Name = "motorSelectionList";
            this.motorSelectionList.Size = new System.Drawing.Size(74, 21);
            this.motorSelectionList.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Body designation";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(110, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Position (goal)";
            // 
            // goalPosition
            // 
            this.goalPosition.Location = new System.Drawing.Point(115, 54);
            this.goalPosition.Name = "goalPosition";
            this.goalPosition.Size = new System.Drawing.Size(64, 20);
            this.goalPosition.TabIndex = 14;
            // 
            // addToListButton
            // 
            this.addToListButton.Location = new System.Drawing.Point(147, 80);
            this.addToListButton.Name = "addToListButton";
            this.addToListButton.Size = new System.Drawing.Size(82, 23);
            this.addToListButton.TabIndex = 15;
            this.addToListButton.Text = "Add ->>";
            this.addToListButton.UseVisualStyleBackColor = true;
            this.addToListButton.Click += new System.EventHandler(this.AddToListButtonClick);
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(340, 390);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(64, 23);
            this.closeButton.TabIndex = 16;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButtonClick);
            // 
            // saveListButton
            // 
            this.saveListButton.Location = new System.Drawing.Point(113, 199);
            this.saveListButton.Name = "saveListButton";
            this.saveListButton.Size = new System.Drawing.Size(98, 23);
            this.saveListButton.TabIndex = 17;
            this.saveListButton.Text = "Save dictionary";
            this.saveListButton.UseVisualStyleBackColor = true;
            this.saveListButton.Click += new System.EventHandler(this.SaveListButtonClick);
            // 
            // notificationLabel
            // 
            this.notificationLabel.AutoSize = true;
            this.notificationLabel.Location = new System.Drawing.Point(9, 400);
            this.notificationLabel.Name = "notificationLabel";
            this.notificationLabel.Size = new System.Drawing.Size(13, 13);
            this.notificationLabel.TabIndex = 18;
            this.notificationLabel.Text = "--";
            // 
            // loadDictionaryButton
            // 
            this.loadDictionaryButton.Location = new System.Drawing.Point(113, 228);
            this.loadDictionaryButton.Name = "loadDictionaryButton";
            this.loadDictionaryButton.Size = new System.Drawing.Size(98, 23);
            this.loadDictionaryButton.TabIndex = 19;
            this.loadDictionaryButton.Text = "Load dictionary";
            this.loadDictionaryButton.UseVisualStyleBackColor = true;
            this.loadDictionaryButton.Click += new System.EventHandler(this.LoadDictionaryButtonClick);
            // 
            // clearBoxButton
            // 
            this.clearBoxButton.Location = new System.Drawing.Point(235, 390);
            this.clearBoxButton.Name = "clearBoxButton";
            this.clearBoxButton.Size = new System.Drawing.Size(64, 23);
            this.clearBoxButton.TabIndex = 20;
            this.clearBoxButton.Text = "Clear";
            this.clearBoxButton.UseVisualStyleBackColor = true;
            this.clearBoxButton.Click += new System.EventHandler(this.ClearBoxButtonClick);
            // 
            // fileNameTextBox
            // 
            this.fileNameTextBox.Location = new System.Drawing.Point(22, 263);
            this.fileNameTextBox.Name = "fileNameTextBox";
            this.fileNameTextBox.Size = new System.Drawing.Size(157, 20);
            this.fileNameTextBox.TabIndex = 21;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 238);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "File name";
            // 
            // RobotControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(439, 438);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.fileNameTextBox);
            this.Controls.Add(this.clearBoxButton);
            this.Controls.Add(this.loadDictionaryButton);
            this.Controls.Add(this.notificationLabel);
            this.Controls.Add(this.saveListButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.addToListButton);
            this.Controls.Add(this.goalPosition);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.motorSelectionList);
            this.Controls.Add(this.motorEngagemenetList);
            this.Controls.Add(this.engageButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(455, 477);
            this.MinimumSize = new System.Drawing.Size(455, 477);
            this.Name = "RobotControl";
            this.Text = "Robot Control";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button engageButton;
        private System.Windows.Forms.RichTextBox motorEngagemenetList;
        private System.Windows.Forms.ComboBox motorSelectionList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox goalPosition;
        private System.Windows.Forms.Button addToListButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button saveListButton;
        private System.Windows.Forms.Label notificationLabel;
        private System.Windows.Forms.Button loadDictionaryButton;
        private System.Windows.Forms.Button clearBoxButton;
        private System.Windows.Forms.TextBox fileNameTextBox;
        private System.Windows.Forms.Label label3;
    }
}