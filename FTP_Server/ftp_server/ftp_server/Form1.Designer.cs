namespace ftp_server
{
    partial class Form1
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
            this.button_AddUser = new System.Windows.Forms.Button();
            this.button_DeleteUser = new System.Windows.Forms.Button();
            this.textBox_AddUserID = new System.Windows.Forms.TextBox();
            this.textBox_AddUserPassword = new System.Windows.Forms.TextBox();
            this.textBox_AddUserConfirm = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_DeleteUserID = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button_StartServer = new System.Windows.Forms.Button();
            this.button_StopServer = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Port = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button_AddUser
            // 
            this.button_AddUser.Location = new System.Drawing.Point(149, 179);
            this.button_AddUser.Margin = new System.Windows.Forms.Padding(4);
            this.button_AddUser.Name = "button_AddUser";
            this.button_AddUser.Size = new System.Drawing.Size(100, 28);
            this.button_AddUser.TabIndex = 3;
            this.button_AddUser.Text = "Add User";
            this.button_AddUser.UseVisualStyleBackColor = true;
            this.button_AddUser.Click += new System.EventHandler(this.button1_Click_AddUser);
            // 
            // button_DeleteUser
            // 
            this.button_DeleteUser.Location = new System.Drawing.Point(424, 85);
            this.button_DeleteUser.Margin = new System.Windows.Forms.Padding(4);
            this.button_DeleteUser.Name = "button_DeleteUser";
            this.button_DeleteUser.Size = new System.Drawing.Size(100, 28);
            this.button_DeleteUser.TabIndex = 5;
            this.button_DeleteUser.Text = "Delete User";
            this.button_DeleteUser.UseVisualStyleBackColor = true;
            this.button_DeleteUser.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox_AddUserID
            // 
            this.textBox_AddUserID.Location = new System.Drawing.Point(39, 55);
            this.textBox_AddUserID.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_AddUserID.Name = "textBox_AddUserID";
            this.textBox_AddUserID.Size = new System.Drawing.Size(210, 22);
            this.textBox_AddUserID.TabIndex = 0;
            // 
            // textBox_AddUserPassword
            // 
            this.textBox_AddUserPassword.Location = new System.Drawing.Point(38, 102);
            this.textBox_AddUserPassword.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_AddUserPassword.Name = "textBox_AddUserPassword";
            this.textBox_AddUserPassword.Size = new System.Drawing.Size(211, 22);
            this.textBox_AddUserPassword.TabIndex = 1;
            // 
            // textBox_AddUserConfirm
            // 
            this.textBox_AddUserConfirm.Location = new System.Drawing.Point(38, 149);
            this.textBox_AddUserConfirm.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_AddUserConfirm.Name = "textBox_AddUserConfirm";
            this.textBox_AddUserConfirm.Size = new System.Drawing.Size(211, 22);
            this.textBox_AddUserConfirm.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 36);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "User ID";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(35, 81);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(35, 128);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(121, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Confirm Password";
            // 
            // textBox_DeleteUserID
            // 
            this.textBox_DeleteUserID.Location = new System.Drawing.Point(314, 55);
            this.textBox_DeleteUserID.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_DeleteUserID.Name = "textBox_DeleteUserID";
            this.textBox_DeleteUserID.Size = new System.Drawing.Size(210, 22);
            this.textBox_DeleteUserID.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(311, 36);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 17);
            this.label4.TabIndex = 2;
            this.label4.Text = "User ID to Delete";
            // 
            // button_StartServer
            // 
            this.button_StartServer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_StartServer.Location = new System.Drawing.Point(72, 347);
            this.button_StartServer.Margin = new System.Windows.Forms.Padding(4);
            this.button_StartServer.Name = "button_StartServer";
            this.button_StartServer.Size = new System.Drawing.Size(177, 54);
            this.button_StartServer.TabIndex = 6;
            this.button_StartServer.Text = "Start the Server";
            this.button_StartServer.UseVisualStyleBackColor = true;
            this.button_StartServer.Click += new System.EventHandler(this.button3_Click_1);
            // 
            // button_StopServer
            // 
            this.button_StopServer.Enabled = false;
            this.button_StopServer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_StopServer.Location = new System.Drawing.Point(314, 347);
            this.button_StopServer.Margin = new System.Windows.Forms.Padding(4);
            this.button_StopServer.Name = "button_StopServer";
            this.button_StopServer.Size = new System.Drawing.Size(177, 53);
            this.button_StopServer.TabIndex = 7;
            this.button_StopServer.Text = "Stop the Server";
            this.button_StopServer.UseVisualStyleBackColor = true;
            this.button_StopServer.Click += new System.EventHandler(this.button4_Click_1);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(39, 277);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(101, 17);
            this.label5.TabIndex = 8;
            this.label5.Text = "Port (Optional)";
            // 
            // textBox_Port
            // 
            this.textBox_Port.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Port.Location = new System.Drawing.Point(39, 298);
            this.textBox_Port.MaxLength = 5;
            this.textBox_Port.Name = "textBox_Port";
            this.textBox_Port.Size = new System.Drawing.Size(210, 22);
            this.textBox_Port.TabIndex = 9;
            this.textBox_Port.Text = "6000";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(557, 414);
            this.Controls.Add(this.textBox_Port);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button_StopServer);
            this.Controls.Add(this.button_StartServer);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_DeleteUserID);
            this.Controls.Add(this.textBox_AddUserConfirm);
            this.Controls.Add(this.textBox_AddUserPassword);
            this.Controls.Add(this.textBox_AddUserID);
            this.Controls.Add(this.button_DeleteUser);
            this.Controls.Add(this.button_AddUser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "FTP Server";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_AddUser;
        private System.Windows.Forms.Button button_DeleteUser;
        private System.Windows.Forms.TextBox textBox_AddUserID;
        private System.Windows.Forms.TextBox textBox_AddUserPassword;
        private System.Windows.Forms.TextBox textBox_AddUserConfirm;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_DeleteUserID;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button_StartServer;
        private System.Windows.Forms.Button button_StopServer;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Port;
    }
}

