namespace DynamixelWizard.SubForms
{
    partial class AnimationTraining
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnimationTraining));
            closeButton = new Button();
            limbSelectionBox = new ComboBox();
            setPositionCheckBox = new CheckBox();
            releaseCheckBox = new CheckBox();
            notificationLabel = new Label();
            resetPositionsButton = new Button();
            compositeListCheckBox = new CheckBox();
            learnThisCheckBox = new CheckBox();
            groupBox1 = new GroupBox();
            RecordTrainingFlashButton = new DynamixelWizard.Controls.FlashButton();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            StepIntervalBox = new TextBox();
            memoryContainsTextBox = new RichTextBox();
            label4 = new Label();
            showInMemeoryButton = new Button();
            memoryCopyTextBox = new RichTextBox();
            saveCopyButton = new Button();
            label5 = new Label();
            storeInDbButton = new Button();
            playAnimationButton = new Button();
            revertAnimationButton = new Button();
            label6 = new Label();
            label7 = new Label();
            poseHardeningSelectionBox = new ComboBox();
            TimerCountdownBox = new TextBox();
            groupBox2 = new GroupBox();
            ReplayCheckBox = new CheckBox();
            label8 = new Label();
            TimerControlButton = new DynamixelWizard.Controls.FlashButton();
            notUnderstoodFlash = new DynamixelWizard.Controls.FlashButton();
            understoodFlash = new DynamixelWizard.Controls.FlashButton();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // closeButton
            // 
            closeButton.Location = new Point(511, 617);
            closeButton.Margin = new Padding(4, 3, 4, 3);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(88, 27);
            closeButton.TabIndex = 1;
            closeButton.Text = "Close";
            closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += CloseButtonClick;
            // 
            // limbSelectionBox
            // 
            limbSelectionBox.FormattingEnabled = true;
            limbSelectionBox.Items.AddRange(new object[] { "Abdomen", "Bust", "Head", "Left arm", "Right arm", "Left leg", "Right leg" });
            limbSelectionBox.Location = new Point(71, 74);
            limbSelectionBox.Margin = new Padding(4, 3, 4, 3);
            limbSelectionBox.Name = "limbSelectionBox";
            limbSelectionBox.Size = new Size(94, 23);
            limbSelectionBox.TabIndex = 3;
            limbSelectionBox.SelectedIndexChanged += LimbSelectionBoxSelectedIndexChanged;
            // 
            // setPositionCheckBox
            // 
            setPositionCheckBox.AutoSize = true;
            setPositionCheckBox.Location = new Point(202, 63);
            setPositionCheckBox.Margin = new Padding(4, 3, 4, 3);
            setPositionCheckBox.Name = "setPositionCheckBox";
            setPositionCheckBox.Size = new Size(93, 19);
            setPositionCheckBox.TabIndex = 4;
            setPositionCheckBox.Text = "Set position?";
            setPositionCheckBox.UseVisualStyleBackColor = true;
            // 
            // releaseCheckBox
            // 
            releaseCheckBox.AutoSize = true;
            releaseCheckBox.Location = new Point(202, 91);
            releaseCheckBox.Margin = new Padding(4, 3, 4, 3);
            releaseCheckBox.Name = "releaseCheckBox";
            releaseCheckBox.Size = new Size(70, 19);
            releaseCheckBox.TabIndex = 5;
            releaseCheckBox.Text = "Release?";
            releaseCheckBox.UseVisualStyleBackColor = true;
            // 
            // notificationLabel
            // 
            notificationLabel.AutoSize = true;
            notificationLabel.Location = new Point(26, 18);
            notificationLabel.Margin = new Padding(4, 0, 4, 0);
            notificationLabel.Name = "notificationLabel";
            notificationLabel.Size = new Size(17, 15);
            notificationLabel.TabIndex = 6;
            notificationLabel.Text = "--";
            // 
            // resetPositionsButton
            // 
            resetPositionsButton.Location = new Point(202, 147);
            resetPositionsButton.Margin = new Padding(4, 3, 4, 3);
            resetPositionsButton.Name = "resetPositionsButton";
            resetPositionsButton.Size = new Size(88, 44);
            resetPositionsButton.TabIndex = 7;
            resetPositionsButton.Text = "Reset motors";
            resetPositionsButton.UseVisualStyleBackColor = true;
            resetPositionsButton.Click += ResetPositionsButton_Click;
            // 
            // compositeListCheckBox
            // 
            compositeListCheckBox.AutoSize = true;
            compositeListCheckBox.Enabled = false;
            compositeListCheckBox.Location = new Point(202, 118);
            compositeListCheckBox.Margin = new Padding(4, 3, 4, 3);
            compositeListCheckBox.Name = "compositeListCheckBox";
            compositeListCheckBox.Size = new Size(142, 19);
            compositeListCheckBox.TabIndex = 8;
            compositeListCheckBox.Text = "Create composite list?";
            compositeListCheckBox.UseVisualStyleBackColor = true;
            // 
            // learnThisCheckBox
            // 
            learnThisCheckBox.AutoSize = true;
            learnThisCheckBox.Location = new Point(243, 37);
            learnThisCheckBox.Margin = new Padding(4, 3, 4, 3);
            learnThisCheckBox.Name = "learnThisCheckBox";
            learnThisCheckBox.Size = new Size(82, 19);
            learnThisCheckBox.TabIndex = 9;
            learnThisCheckBox.Text = "Learn this?";
            learnThisCheckBox.UseVisualStyleBackColor = true;
            learnThisCheckBox.CheckedChanged += LearnThisCheckBoxCheckedChanged;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(RecordTrainingFlashButton);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(learnThisCheckBox);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(33, 197);
            groupBox1.Margin = new Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 3, 4, 3);
            groupBox1.Size = new Size(548, 115);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            groupBox1.Text = "Learning by showing";
            // 
            // RecordTrainingFlashButton
            // 
            RecordTrainingFlashButton.FlasherButtonColorOff = SystemColors.Control;
            RecordTrainingFlashButton.FlasherButtonColorOn = Color.LightGreen;
            RecordTrainingFlashButton.FlashNumber = 0;
            RecordTrainingFlashButton.Location = new Point(418, 37);
            RecordTrainingFlashButton.Margin = new Padding(4, 3, 4, 3);
            RecordTrainingFlashButton.Name = "RecordTrainingFlashButton";
            RecordTrainingFlashButton.Size = new Size(86, 44);
            RecordTrainingFlashButton.TabIndex = 49;
            RecordTrainingFlashButton.Text = "Record";
            RecordTrainingFlashButton.UseVisualStyleBackColor = true;
            RecordTrainingFlashButton.Click += RecordTrainingFlashButtonClick;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(24, 84);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(79, 15);
            label3.TabIndex = 2;
            label3.Text = "* Tick the box";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(26, 62);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(178, 15);
            label2.TabIndex = 1;
            label2.Text = "* Make the motion to be learned";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(26, 37);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(146, 15);
            label1.TabIndex = 0;
            label1.Text = "* Choose the area to teach";
            // 
            // StepIntervalBox
            // 
            StepIntervalBox.Location = new Point(71, 23);
            StepIntervalBox.Margin = new Padding(4, 3, 4, 3);
            StepIntervalBox.Name = "StepIntervalBox";
            StepIntervalBox.Size = new Size(47, 23);
            StepIntervalBox.TabIndex = 50;
            StepIntervalBox.Text = "30";
            // 
            // memoryContainsTextBox
            // 
            memoryContainsTextBox.Location = new Point(29, 347);
            memoryContainsTextBox.Margin = new Padding(4, 3, 4, 3);
            memoryContainsTextBox.Name = "memoryContainsTextBox";
            memoryContainsTextBox.Size = new Size(224, 246);
            memoryContainsTextBox.TabIndex = 11;
            memoryContainsTextBox.Text = "";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(29, 329);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(147, 15);
            label4.TabIndex = 10;
            label4.Text = "Show what it is in memory";
            // 
            // showInMemeoryButton
            // 
            showInMemeoryButton.Location = new Point(261, 347);
            showInMemeoryButton.Margin = new Padding(4, 3, 4, 3);
            showInMemeoryButton.Name = "showInMemeoryButton";
            showInMemeoryButton.Size = new Size(70, 27);
            showInMemeoryButton.TabIndex = 12;
            showInMemeoryButton.Text = "Show";
            showInMemeoryButton.UseVisualStyleBackColor = true;
            showInMemeoryButton.Click += ShowInMemeoryButtonClick;
            // 
            // memoryCopyTextBox
            // 
            memoryCopyTextBox.Location = new Point(338, 347);
            memoryCopyTextBox.Margin = new Padding(4, 3, 4, 3);
            memoryCopyTextBox.Name = "memoryCopyTextBox";
            memoryCopyTextBox.Size = new Size(242, 246);
            memoryCopyTextBox.TabIndex = 13;
            memoryCopyTextBox.Text = "";
            // 
            // saveCopyButton
            // 
            saveCopyButton.Location = new Point(261, 391);
            saveCopyButton.Margin = new Padding(4, 3, 4, 3);
            saveCopyButton.Name = "saveCopyButton";
            saveCopyButton.Size = new Size(70, 51);
            saveCopyButton.TabIndex = 14;
            saveCopyButton.Text = "Store >>>";
            saveCopyButton.UseVisualStyleBackColor = true;
            saveCopyButton.Click += SaveCopyButtonClick;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(345, 329);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(126, 15);
            label5.TabIndex = 15;
            label5.Text = "Store a set of positions";
            // 
            // storeInDbButton
            // 
            storeInDbButton.Location = new Point(261, 449);
            storeInDbButton.Margin = new Padding(4, 3, 4, 3);
            storeInDbButton.Name = "storeInDbButton";
            storeInDbButton.Size = new Size(70, 51);
            storeInDbButton.TabIndex = 17;
            storeInDbButton.Text = "Store (db)";
            storeInDbButton.UseVisualStyleBackColor = true;
            storeInDbButton.Click += StoreInDbButtonClick;
            // 
            // playAnimationButton
            // 
            playAnimationButton.Location = new Point(359, 120);
            playAnimationButton.Margin = new Padding(4, 3, 4, 3);
            playAnimationButton.Name = "playAnimationButton";
            playAnimationButton.Size = new Size(88, 27);
            playAnimationButton.TabIndex = 18;
            playAnimationButton.Text = "Play!";
            playAnimationButton.UseVisualStyleBackColor = true;
            playAnimationButton.Click += PlayAnimationButtonClick;
            // 
            // revertAnimationButton
            // 
            revertAnimationButton.Location = new Point(359, 153);
            revertAnimationButton.Margin = new Padding(4, 3, 4, 3);
            revertAnimationButton.Name = "revertAnimationButton";
            revertAnimationButton.Size = new Size(88, 27);
            revertAnimationButton.TabIndex = 19;
            revertAnimationButton.Text = "Revert";
            revertAnimationButton.UseVisualStyleBackColor = true;
            revertAnimationButton.Click += RevertAnimationButtonClick;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(64, 51);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(101, 15);
            label6.TabIndex = 20;
            label6.Text = "Training Selection";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(64, 118);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(91, 15);
            label7.TabIndex = 22;
            label7.Text = "Pose Hardening";
            // 
            // poseHardeningSelectionBox
            // 
            poseHardeningSelectionBox.FormattingEnabled = true;
            poseHardeningSelectionBox.Items.AddRange(new object[] { "Abdomen", "Bust", "Head", "Left arm", "Right arm", "Left leg", "Right leg" });
            poseHardeningSelectionBox.Location = new Point(71, 141);
            poseHardeningSelectionBox.Margin = new Padding(4, 3, 4, 3);
            poseHardeningSelectionBox.Name = "poseHardeningSelectionBox";
            poseHardeningSelectionBox.Size = new Size(94, 23);
            poseHardeningSelectionBox.TabIndex = 21;
            poseHardeningSelectionBox.SelectedIndexChanged += PoseHardeningSelectionBoxSelectedIndexChanged;
            // 
            // TimerCountdownBox
            // 
            TimerCountdownBox.Location = new Point(72, 84);
            TimerCountdownBox.Margin = new Padding(4, 3, 4, 3);
            TimerCountdownBox.Name = "TimerCountdownBox";
            TimerCountdownBox.Size = new Size(47, 23);
            TimerCountdownBox.TabIndex = 24;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(ReplayCheckBox);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(TimerCountdownBox);
            groupBox2.Controls.Add(StepIntervalBox);
            groupBox2.Controls.Add(TimerControlButton);
            groupBox2.Location = new Point(469, 23);
            groupBox2.Margin = new Padding(4, 3, 4, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 3, 4, 3);
            groupBox2.Size = new Size(130, 157);
            groupBox2.TabIndex = 52;
            groupBox2.TabStop = false;
            groupBox2.Text = "Training timer";
            // 
            // ReplayCheckBox
            // 
            ReplayCheckBox.AutoSize = true;
            ReplayCheckBox.Location = new Point(16, 120);
            ReplayCheckBox.Margin = new Padding(4, 3, 4, 3);
            ReplayCheckBox.Name = "ReplayCheckBox";
            ReplayCheckBox.Size = new Size(61, 19);
            ReplayCheckBox.TabIndex = 53;
            ReplayCheckBox.Text = "Replay";
            ReplayCheckBox.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(8, 27);
            label8.Margin = new Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new Size(53, 15);
            label8.TabIndex = 52;
            label8.Text = "Duration";
            // 
            // TimerControlButton
            // 
            TimerControlButton.FlasherButtonColorOff = SystemColors.Control;
            TimerControlButton.FlasherButtonColorOn = Color.LightGreen;
            TimerControlButton.FlashNumber = 0;
            TimerControlButton.Location = new Point(83, 57);
            TimerControlButton.Margin = new Padding(4, 3, 4, 3);
            TimerControlButton.Name = "TimerControlButton";
            TimerControlButton.Size = new Size(27, 18);
            TimerControlButton.TabIndex = 51;
            TimerControlButton.UseVisualStyleBackColor = true;
            TimerControlButton.Click += TimerControlButtonClick;
            // 
            // notUnderstoodFlash
            // 
            notUnderstoodFlash.FlasherButtonColorOff = SystemColors.Control;
            notUnderstoodFlash.FlasherButtonColorOn = Color.Red;
            notUnderstoodFlash.FlashNumber = 0;
            notUnderstoodFlash.Location = new Point(350, 69);
            notUnderstoodFlash.Margin = new Padding(4, 3, 4, 3);
            notUnderstoodFlash.Name = "notUnderstoodFlash";
            notUnderstoodFlash.Size = new Size(86, 44);
            notUnderstoodFlash.TabIndex = 2;
            notUnderstoodFlash.Text = "Not understood";
            notUnderstoodFlash.UseVisualStyleBackColor = true;
            // 
            // understoodFlash
            // 
            understoodFlash.FlasherButtonColorOff = SystemColors.Control;
            understoodFlash.FlasherButtonColorOn = Color.LightGreen;
            understoodFlash.FlashNumber = 0;
            understoodFlash.Location = new Point(350, 18);
            understoodFlash.Margin = new Padding(4, 3, 4, 3);
            understoodFlash.Name = "understoodFlash";
            understoodFlash.Size = new Size(86, 44);
            understoodFlash.TabIndex = 0;
            understoodFlash.Text = "Understood";
            understoodFlash.UseVisualStyleBackColor = true;
            // 
            // AnimationTraining
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(612, 655);
            Controls.Add(groupBox2);
            Controls.Add(label7);
            Controls.Add(poseHardeningSelectionBox);
            Controls.Add(label6);
            Controls.Add(revertAnimationButton);
            Controls.Add(playAnimationButton);
            Controls.Add(storeInDbButton);
            Controls.Add(label5);
            Controls.Add(saveCopyButton);
            Controls.Add(memoryCopyTextBox);
            Controls.Add(showInMemeoryButton);
            Controls.Add(label4);
            Controls.Add(memoryContainsTextBox);
            Controls.Add(groupBox1);
            Controls.Add(compositeListCheckBox);
            Controls.Add(resetPositionsButton);
            Controls.Add(notificationLabel);
            Controls.Add(releaseCheckBox);
            Controls.Add(setPositionCheckBox);
            Controls.Add(limbSelectionBox);
            Controls.Add(notUnderstoodFlash);
            Controls.Add(closeButton);
            Controls.Add(understoodFlash);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MaximumSize = new Size(628, 694);
            MinimumSize = new Size(628, 694);
            Name = "AnimationTraining";
            Text = "Animation Training";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private Controls.FlashButton understoodFlash;
        private System.Windows.Forms.Button closeButton;
        private Controls.FlashButton notUnderstoodFlash;
        private System.Windows.Forms.ComboBox limbSelectionBox;
        private System.Windows.Forms.CheckBox setPositionCheckBox;
        private System.Windows.Forms.CheckBox releaseCheckBox;
        private System.Windows.Forms.Label notificationLabel;
        private System.Windows.Forms.Button resetPositionsButton;
        private System.Windows.Forms.CheckBox compositeListCheckBox;
        private System.Windows.Forms.CheckBox learnThisCheckBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox memoryContainsTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button showInMemeoryButton;
        private System.Windows.Forms.RichTextBox memoryCopyTextBox;
        private System.Windows.Forms.Button saveCopyButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button storeInDbButton;
        private System.Windows.Forms.Button playAnimationButton;
        private System.Windows.Forms.Button revertAnimationButton;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox poseHardeningSelectionBox;
        private Controls.FlashButton RecordTrainingFlashButton;
        private System.Windows.Forms.TextBox StepIntervalBox;
        private System.Windows.Forms.TextBox TimerCountdownBox;
        private Controls.FlashButton TimerControlButton;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox ReplayCheckBox;
    }
}