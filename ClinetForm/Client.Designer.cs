namespace ClinetForm
{
    partial class Client
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnLogin = new System.Windows.Forms.Button();
            this.tbUID = new System.Windows.Forms.TextBox();
            this.lbUid = new System.Windows.Forms.Label();
            this.lbPW = new System.Windows.Forms.Label();
            this.tbPW = new System.Windows.Forms.TextBox();
            this.lbResult = new System.Windows.Forms.Label();
            this.tbResult = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(215, 22);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(75, 23);
            this.btnLogin.TabIndex = 0;
            this.btnLogin.Text = "Login";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // tbUID
            // 
            this.tbUID.Location = new System.Drawing.Point(3, 22);
            this.tbUID.Name = "tbUID";
            this.tbUID.Size = new System.Drawing.Size(100, 22);
            this.tbUID.TabIndex = 1;
            // 
            // lbUid
            // 
            this.lbUid.AutoSize = true;
            this.lbUid.Location = new System.Drawing.Point(4, 4);
            this.lbUid.Name = "lbUid";
            this.lbUid.Size = new System.Drawing.Size(38, 12);
            this.lbUid.TabIndex = 2;
            this.lbUid.Text = "UserID";
            // 
            // lbPW
            // 
            this.lbPW.AutoSize = true;
            this.lbPW.Location = new System.Drawing.Point(110, 4);
            this.lbPW.Name = "lbPW";
            this.lbPW.Size = new System.Drawing.Size(70, 12);
            this.lbPW.TabIndex = 3;
            this.lbPW.Text = "UserPaasword";
            // 
            // tbPW
            // 
            this.tbPW.Location = new System.Drawing.Point(109, 22);
            this.tbPW.Name = "tbPW";
            this.tbPW.PasswordChar = '*';
            this.tbPW.Size = new System.Drawing.Size(100, 22);
            this.tbPW.TabIndex = 4;
            // 
            // lbResult
            // 
            this.lbResult.AutoSize = true;
            this.lbResult.Location = new System.Drawing.Point(4, 56);
            this.lbResult.Name = "lbResult";
            this.lbResult.Size = new System.Drawing.Size(34, 12);
            this.lbResult.TabIndex = 5;
            this.lbResult.Text = "Result";
            // 
            // tbResult
            // 
            this.tbResult.Location = new System.Drawing.Point(3, 71);
            this.tbResult.Multiline = true;
            this.tbResult.Name = "tbResult";
            this.tbResult.ReadOnly = true;
            this.tbResult.Size = new System.Drawing.Size(287, 304);
            this.tbResult.TabIndex = 6;
            // 
            // Client
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tbResult);
            this.Controls.Add(this.lbResult);
            this.Controls.Add(this.tbPW);
            this.Controls.Add(this.lbPW);
            this.Controls.Add(this.lbUid);
            this.Controls.Add(this.tbUID);
            this.Controls.Add(this.btnLogin);
            this.Name = "Client";
            this.Size = new System.Drawing.Size(645, 378);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.TextBox tbUID;
        private System.Windows.Forms.Label lbUid;
        private System.Windows.Forms.Label lbPW;
        private System.Windows.Forms.TextBox tbPW;
        private System.Windows.Forms.Label lbResult;
        private System.Windows.Forms.TextBox tbResult;
    }
}
