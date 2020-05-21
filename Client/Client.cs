using Common;
using Common.Command;
using Common.Setting;
using Common.User;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Client : Form
    {
        static Dictionary<byte, Action<Socket, byte[]>> CommandRespDict = new Dictionary<byte, Action<Socket, byte[]>>();


        private void btnLogin_Click(object sender, EventArgs e)
        {
            Login();
        }


        public Client()
        {
            InitializeComponent();
            InitCommandMapping();
        }

        public void Login()
        {
            //懶得寫負載平衡 先這樣
            btnLogin.Enabled = false;
            var rand = new Random().Next(1, 3);
            int serverPort;
            if (rand == 1)
            {
                serverPort = GlobalSetting.PortNum1;
            }
            else
            {
                serverPort = GlobalSetting.PortNum2;
            }

            try
            {
                
                //送出帳號密碼 要改成用專用送出
                SendCommand(, Command.LoginAuth, UserReqLoginPayload.CreatePayload(GenUserInfo()));

            }
            catch (Exception ex)
            {
                ShowLogOnResult(ex.Message);
                Console.ReadLine();
            }
        }
        private void InitCommandMapping()
        {
            CommandRespDict = new Dictionary<byte, Action<Socket, byte[]>>()
            {
                { (byte)Command.LoginAuth, ReceivedLogin},
                { (byte)Command.GetMsgAll, ReceviveAllMessage},
                { (byte)Command.GetMsgOnce, ReceviveAllMessage},
            };
        }

        private void ReceiveCommand(object socketClient)
        {
            Socket socket = (Socket)socketClient;
            while (true)
            {
                try
                {
                    //先接收指令類別，然後將對應的資料傳給對應的Func
                    byte[] buffer = new byte[8192];
                    int length = socket.Receive(buffer);
                    buffer.GetCommand(out var command);
                    if (!CommandRespDict.TryGetValue((byte)command, out var mappingFunc))
                    {
                        ShowLogOnResult("Not found mapping function.");
                        //不接受這個封包
                        continue;
                    };
                    //過濾第一個字並拿取封包長度去掉Command的部分
                    mappingFunc(socket, buffer.Skip(CommandStreamHelper.CommandSize).Take(length).ToArray());
                }
                catch (Exception ex)
                {
                    ShowLogOnResult(ex.Message);
                    ShowLogOnResult($"Packet has problem.");
                    socket.Close();
                    break;
                }
            }
        }

        private void ReceivedLogin(Socket socket, byte[] DataArray)
        {
            UserRespLoginPayload.ParsePayload(DataArray, out var ack);
            if (ack != UserAck.Success)
            {
                ShowLogOnResult($"{nameof(ReceivedLogin)} Fail, Ack={ack}");
                //重新輸入帳密並登入 暫時不限次數
                ShowLogOnResult($"Please check you Id and Password.");
                return;
            }

            ShowLogOnResult("Please Type Anything 'Press Enter'：");
        }

        private void ReceviveAllMessage(Socket socket, byte[] byteArray)
        {
            MessageStreamHelper.GetStream(byteArray, out var ack, out var infoDatas);

            if (ack != MessageAck.Success)
            {
                ShowLogOnResult($"{nameof(ReceviveAllMessage)} Fail, Ack={ack}");
            }

            foreach (MessageInfoData infoData in infoDatas)
            {
                ShowLogOnResult($"Msg:{infoData.Message}");
            }
        }

        private void SendMsgOnce(Socket socket, Command command, string sendMsg)
        {
            //轉換成物件再送出
            var infoDatas = new MessageInfoData[]
            {
                new MessageInfoData
                {
                    Message = sendMsg,
                }
            };

            SendCommand(SocketClient, command, MessageStreamHelper.CreateStream(MessageAck.Success, infoDatas));
        }

        private void ShowLogOnResult(string str)
        {
            tbResult.Text = tbResult.Text + str + Environment.NewLine;
        }

        private UserInfoData GenUserInfo()
        {
            ShowLogOnResult($"id={tbUID.Text},pw={tbPW.Text}");
            var infoData = new UserInfoData
            {
                UserId = tbUID.Text,
                UserPwd = tbPW.Text,
            };
            return infoData;
        }

        private void SetLoginAndSendUI(bool showLogin, bool showSend)
        {
            gbLogin.Visible = showLogin;
            gbInput.Visible = showSend;
        }

    }
}
