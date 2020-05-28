namespace Client
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

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.lbUid = new System.Windows.Forms.Label();
            this.tbUID = new System.Windows.Forms.TextBox();
            this.lbPW = new System.Windows.Forms.Label();
            this.tbPW = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lbResult = new System.Windows.Forms.Label();
            this.tbResult = new System.Windows.Forms.TextBox();
            this.gbLogin = new System.Windows.Forms.GroupBox();
            this.gbInput = new System.Windows.Forms.GroupBox();
            this.tbInput = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.bgWorkConnect = new System.ComponentModel.BackgroundWorker();
            this.gbLogin.SuspendLayout();
            this.gbInput.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbUid
            // 
            this.lbUid.AutoSize = true;
            this.lbUid.Location = new System.Drawing.Point(6, 9);
            this.lbUid.Name = "lbUid";
            this.lbUid.Size = new System.Drawing.Size(38, 12);
            this.lbUid.TabIndex = 3;
            this.lbUid.Text = "UserID";
            // 
            // tbUID
            // 
            this.tbUID.Location = new System.Drawing.Point(6, 24);
            this.tbUID.Name = "tbUID";
            this.tbUID.Size = new System.Drawing.Size(100, 22);
            this.tbUID.TabIndex = 4;
            // 
            // lbPW
            // 
            this.lbPW.AutoSize = true;
            this.lbPW.Location = new System.Drawing.Point(110, 9);
            this.lbPW.Name = "lbPW";
            this.lbPW.Size = new System.Drawing.Size(70, 12);
            this.lbPW.TabIndex = 5;
            this.lbPW.Text = "UserPaasword";
            // 
            // tbPW
            // 
            this.tbPW.Location = new System.Drawing.Point(112, 24);
            this.tbPW.Name = "tbPW";
            this.tbPW.PasswordChar = '*';
            this.tbPW.Size = new System.Drawing.Size(100, 22);
            this.tbPW.TabIndex = 6;
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(218, 23);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(75, 23);
            this.btnLogin.TabIndex = 7;
            this.btnLogin.Text = "Login";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // lbResult
            // 
            this.lbResult.AutoSize = true;
            this.lbResult.Location = new System.Drawing.Point(10, 65);
            this.lbResult.Name = "lbResult";
            this.lbResult.Size = new System.Drawing.Size(34, 12);
            this.lbResult.TabIndex = 8;
            this.lbResult.Text = "Result";
            // 
            // tbResult
            // 
            this.tbResult.Location = new System.Drawing.Point(12, 80);
            this.tbResult.Multiline = true;
            this.tbResult.Name = "tbResult";
            this.tbResult.ReadOnly = true;
            this.tbResult.Size = new System.Drawing.Size(301, 304);
            this.tbResult.TabIndex = 9;
            // 
            // gbLogin
            // 
            this.gbLogin.Controls.Add(this.tbUID);
            this.gbLogin.Controls.Add(this.lbUid);
            this.gbLogin.Controls.Add(this.lbPW);
            this.gbLogin.Controls.Add(this.btnLogin);
            this.gbLogin.Controls.Add(this.tbPW);
            this.gbLogin.Location = new System.Drawing.Point(12, 8);
            this.gbLogin.Name = "gbLogin";
            this.gbLogin.Size = new System.Drawing.Size(301, 54);
            this.gbLogin.TabIndex = 10;
            this.gbLogin.TabStop = false;
            // 
            // gbInput
            // 
            this.gbInput.Controls.Add(this.tbInput);
            this.gbInput.Controls.Add(this.label1);
            this.gbInput.Controls.Add(this.btnSend);
            this.gbInput.Location = new System.Drawing.Point(12, 8);
            this.gbInput.Name = "gbInput";
            this.gbInput.Size = new System.Drawing.Size(301, 54);
            this.gbInput.TabIndex = 11;
            this.gbInput.TabStop = false;
            this.gbInput.Visible = false;
            // 
            // tbInput
            // 
            this.tbInput.Location = new System.Drawing.Point(6, 24);
            this.tbInput.Name = "tbInput";
            this.tbInput.Size = new System.Drawing.Size(206, 22);
            this.tbInput.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "Input";
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(218, 23);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 7;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            // 
            // bgWorkConnect
            // 
            this.bgWorkConnect.WorkerReportsProgress = true;
            this.bgWorkConnect.WorkerSupportsCancellation = true;
            this.bgWorkConnect.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgWorkerConnect_DoWork);
            // 
            // Client
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(904, 405);
            this.Controls.Add(this.gbLogin);
            this.Controls.Add(this.tbResult);
            this.Controls.Add(this.lbResult);
            this.Controls.Add(this.gbInput);
            this.Name = "Client";
            this.Text = "Client";
            this.gbLogin.ResumeLayout(false);
            this.gbLogin.PerformLayout();
            this.gbInput.ResumeLayout(false);
            this.gbInput.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbUid;
        private System.Windows.Forms.TextBox tbUID;
        private System.Windows.Forms.Label lbPW;
        private System.Windows.Forms.TextBox tbPW;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lbResult;
        private System.Windows.Forms.TextBox tbResult;
        private System.Windows.Forms.GroupBox gbLogin;
        private System.Windows.Forms.GroupBox gbInput;
        private System.Windows.Forms.TextBox tbInput;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSend;
        private System.ComponentModel.BackgroundWorker bgWorkConnect;
    }
}

