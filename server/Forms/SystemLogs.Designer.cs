namespace server.Forms
{
    partial class SystemLogs
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
            _logBox = new RichTextBox();
            panel1 = new Panel();
            button1 = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // _logBox
            // 
            _logBox.BorderStyle = BorderStyle.None;
            _logBox.Dock = DockStyle.Fill;
            _logBox.Location = new Point(0, 0);
            _logBox.Name = "_logBox";
            _logBox.Size = new Size(1017, 420);
            _logBox.TabIndex = 0;
            _logBox.Text = "";
            // 
            // panel1
            // 
            panel1.BackColor = Color.Black;
            panel1.Controls.Add(button1);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 420);
            panel1.Name = "panel1";
            panel1.Size = new Size(1017, 38);
            panel1.TabIndex = 1;
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(891, 0);
            button1.Name = "button1";
            button1.Size = new Size(123, 38);
            button1.TabIndex = 0;
            button1.Text = "Clear Logs";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // SystemLogs
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1017, 458);
            Controls.Add(_logBox);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SystemLogs";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SystemLogs";
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox _logBox;
        private Panel panel1;
        private Button button1;
    }
}