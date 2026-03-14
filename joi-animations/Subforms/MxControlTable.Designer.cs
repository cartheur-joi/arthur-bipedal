namespace DynamixelWizard.SubForms
{
    partial class MxControlTable
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
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.closeWindowButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Image = global::DynamixelWizard.Properties.Resources.MX_control_table;
            this.pictureBox.Location = new System.Drawing.Point(12, 12);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(648, 728);
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // closeWindowButton
            // 
            this.closeWindowButton.Location = new System.Drawing.Point(585, 746);
            this.closeWindowButton.Name = "closeWindowButton";
            this.closeWindowButton.Size = new System.Drawing.Size(75, 23);
            this.closeWindowButton.TabIndex = 1;
            this.closeWindowButton.Text = "Close";
            this.closeWindowButton.UseVisualStyleBackColor = true;
            this.closeWindowButton.Click += new System.EventHandler(this.closeWindowButton_Click);
            // 
            // MxControlTable
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 776);
            this.Controls.Add(this.closeWindowButton);
            this.Controls.Add(this.pictureBox);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(700, 815);
            this.MinimumSize = new System.Drawing.Size(700, 815);
            this.Name = "MxControlTable";
            this.Text = "The control table of the RAM area for the MX-28 and MX-64 motors";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Button closeWindowButton;
    }
}