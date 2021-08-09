﻿namespace pkuManager
{
    partial class ImportingWindow
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
            this.descLabel = new System.Windows.Forms.Label();
            this.acceptButton = new System.Windows.Forms.Button();
            this.questionsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.notesPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // descLabel
            // 
            this.descLabel.AutoSize = true;
            this.descLabel.Location = new System.Drawing.Point(32, 20);
            this.descLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.descLabel.MaximumSize = new System.Drawing.Size(408, 0);
            this.descLabel.Name = "descLabel";
            this.descLabel.Size = new System.Drawing.Size(388, 30);
            this.descLabel.TabIndex = 5;
            this.descLabel.Text = "The following questions must be answered before importing this Format (extension)" +
    " file.";
            this.descLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // acceptButton
            // 
            this.acceptButton.Location = new System.Drawing.Point(280, 308);
            this.acceptButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(88, 27);
            this.acceptButton.TabIndex = 6;
            this.acceptButton.Text = "Accept";
            this.acceptButton.UseVisualStyleBackColor = true;
            // 
            // questionsPanel
            // 
            this.questionsPanel.AutoScroll = true;
            this.questionsPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.questionsPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.questionsPanel.Location = new System.Drawing.Point(4, 19);
            this.questionsPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.questionsPanel.Name = "questionsPanel";
            this.questionsPanel.Size = new System.Drawing.Size(195, 247);
            this.questionsPanel.TabIndex = 0;
            this.questionsPanel.WrapContents = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.questionsPanel);
            this.groupBox1.Location = new System.Drawing.Point(39, 69);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(204, 269);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Questions";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.notesPanel);
            this.groupBox3.Location = new System.Drawing.Point(39, 360);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox3.Size = new System.Drawing.Size(333, 134);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Global Flags";
            // 
            // notesPanel
            // 
            this.notesPanel.AutoScroll = true;
            this.notesPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.notesPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.notesPanel.Location = new System.Drawing.Point(4, 19);
            this.notesPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.notesPanel.Name = "notesPanel";
            this.notesPanel.Size = new System.Drawing.Size(325, 112);
            this.notesPanel.TabIndex = 0;
            this.notesPanel.WrapContents = false;
            // 
            // ImportingWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(462, 524);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.acceptButton);
            this.Controls.Add(this.descLabel);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(478, 563);
            this.Name = "ImportingWindow";
            this.Text = "Import Questions (Format)";
            this.groupBox1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label descLabel;
        private System.Windows.Forms.Button acceptButton;
        private System.Windows.Forms.GroupBox groupBox1;
        protected System.Windows.Forms.FlowLayoutPanel questionsPanel;
        private System.Windows.Forms.GroupBox groupBox3;
        protected System.Windows.Forms.FlowLayoutPanel notesPanel;
    }
}