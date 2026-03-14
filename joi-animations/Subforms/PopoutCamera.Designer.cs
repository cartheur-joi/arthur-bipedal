namespace DynamixelWizard.SubForms
{
    partial class PopoutCamera
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
            this.CameraBox = new System.Windows.Forms.PictureBox();
            this.CloseButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.CameraBox)).BeginInit();
            this.SuspendLayout();
            // 
            // CameraBox
            // 
            this.CameraBox.Location = new System.Drawing.Point(13, 13);
            this.CameraBox.Name = "CameraBox";
            this.CameraBox.Size = new System.Drawing.Size(687, 436);
            this.CameraBox.TabIndex = 0;
            this.CameraBox.TabStop = false;
            // 
            // CloseButton
            // 
            this.CloseButton.Location = new System.Drawing.Point(706, 426);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 1;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButtonClick);
            // 
            // PopoutCamera
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.CameraBox);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(800, 500);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "PopoutCamera";
            this.ShowIcon = false;
            this.Text = "Camera Viewer";
            ((System.ComponentModel.ISupportInitialize)(this.CameraBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox CameraBox;
        private System.Windows.Forms.Button CloseButton;
    }
}