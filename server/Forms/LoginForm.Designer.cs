namespace server.Forms
{
    partial class LoginForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            label4 = new Label();
            label5 = new Label();
            cbStartServerOnLogin = new CheckBox();
            btnLogin = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.None;
            label1.AutoSize = true;
            label1.Font = new Font("Arial", 22.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(125, 31);
            label1.Name = "label1";
            label1.Size = new Size(181, 35);
            label1.TabIndex = 0;
            label1.Text = "POS Server";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.None;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(139, 61);
            label2.Name = "label2";
            label2.Size = new Size(156, 21);
            label2.TabIndex = 1;
            label2.Text = "Administrator Login";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.None;
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold);
            label3.Location = new Point(29, 105);
            label3.Name = "label3";
            label3.Size = new Size(101, 25);
            label3.TabIndex = 2;
            label3.Text = "Username";
            // 
            // txtUsername
            // 
            txtUsername.Anchor = AnchorStyles.None;
            txtUsername.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtUsername.Location = new Point(33, 130);
            txtUsername.Margin = new Padding(3, 2, 3, 2);
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "Enter server admin username";
            txtUsername.Size = new Size(385, 29);
            txtUsername.TabIndex = 3;
            // 
            // txtPassword
            // 
            txtPassword.Anchor = AnchorStyles.None;
            txtPassword.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtPassword.Location = new Point(33, 200);
            txtPassword.Margin = new Padding(3, 2, 3, 2);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.PlaceholderText = "Enter password";
            txtPassword.Size = new Size(385, 29);
            txtPassword.TabIndex = 5;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.None;
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold);
            label4.Location = new Point(29, 174);
            label4.Name = "label4";
            label4.Size = new Size(97, 25);
            label4.TabIndex = 4;
            label4.Text = "Password";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.None;
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.Location = new Point(29, 245);
            label5.Name = "label5";
            label5.Size = new Size(145, 25);
            label5.TabIndex = 6;
            label5.Text = "Server Options";
            // 
            // cbStartServerOnLogin
            // 
            cbStartServerOnLogin.Anchor = AnchorStyles.None;
            cbStartServerOnLogin.AutoSize = true;
            cbStartServerOnLogin.Checked = true;
            cbStartServerOnLogin.CheckState = CheckState.Checked;
            cbStartServerOnLogin.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            cbStartServerOnLogin.ForeColor = Color.Black;
            cbStartServerOnLogin.Location = new Point(33, 272);
            cbStartServerOnLogin.Margin = new Padding(3, 2, 3, 2);
            cbStartServerOnLogin.Name = "cbStartServerOnLogin";
            cbStartServerOnLogin.Size = new Size(169, 23);
            cbStartServerOnLogin.TabIndex = 7;
            cbStartServerOnLogin.Text = "Start server after login";
            cbStartServerOnLogin.UseVisualStyleBackColor = true;
            // 
            // btnLogin
            // 
            btnLogin.Anchor = AnchorStyles.None;
            btnLogin.BackColor = Color.FromArgb(76, 175, 80);
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(33, 311);
            btnLogin.Margin = new Padding(3, 2, 3, 2);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(384, 38);
            btnLogin.TabIndex = 8;
            btnLogin.Text = "LOGIN";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(447, 380);
            Controls.Add(btnLogin);
            Controls.Add(cbStartServerOnLogin);
            Controls.Add(label5);
            Controls.Add(txtPassword);
            Controls.Add(label4);
            Controls.Add(txtUsername);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Login";
            Load += LoginForm_Load;
            KeyDown += LoginForm_KeyDown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Label label4;
        private Label label5;
        private CheckBox cbStartServerOnLogin;
        private Button btnLogin;
    }
}