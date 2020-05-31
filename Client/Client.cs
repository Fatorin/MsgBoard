using Common;
using Common.Command;
using Common.Packet;
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
        static Dictionary<int, Action> CommandReqDict = new Dictionary<int, Action>();
        static Dictionary<int, Action<byte[]>> CommandRespDict = new Dictionary<int, Action<byte[]>>();
        static int serverPort;
        static Socket socketClient;

        public Client()
        {
            InitializeComponent();
            InitCommandMapping();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            //懶得寫負載平衡 先這樣
            btnLogin.Enabled = false;
            var rand = new Random().Next(1, 3);
            serverPort = GlobalSetting.PortNum1;
            /*if (rand == 1)
            {
                serverPort = GlobalSetting.PortNum1;
            }
            else
            {
                serverPort = GlobalSetting.PortNum2;
            }*/
            bgWorkerGoFunc((int)CommandEnum.LoginAuth);
        }

        private void bgWorkerGoFunc(int command)
        {
            if (bgWorkConnect.IsBusy != true)
            {
                bgWorkConnect.RunWorkerAsync(command);
            }
        }

        private void bgWorkerConnect_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!CommandReqDict.TryGetValue((int)e.Argument, out var function))
            {
                ShowLogOnResult("Not found mapping req function.");
                return;
            }
            function();
        }
        private void bgWorkerConnect_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ShowLogOnResult("Func Finish.");
        }

        private void InitCommandMapping()
        {
            CommandReqDict = new Dictionary<int, Action>()
            {
                {(int)CommandEnum.LoginAuth, StartClientAndLogin}
            };

            CommandRespDict = new Dictionary<int, Action<byte[]>>()
            {
                { (int)CommandEnum.LoginAuth, ReceivedLogin},
                { (int)CommandEnum.GetMsgAll, ReceviveAllMessage},
                { (int)CommandEnum.GetMsgOnce, ReceviveAllMessage},
            };
        }

        private void ReceivedLogin(byte[] DataArray)
        {
            UserRespLoginPayload.ParsePayload(DataArray, out var ack);
            if (ack != UserAck.Success)
            {
                ShowLogOnResult($"{nameof(ReceivedLogin)} Fail, Ack={ack}");
                //重新輸入帳密並登入 暫時不限次數
                ShowLogOnResult($"Please check you Id and Password.");
                SocketShutDown(socketClient);
                btnLogin.InvokeIfRequired(() =>
                {
                    btnLogin.Enabled = true;
                });
                return;
            }

            ShowLogOnResult("Please Type Anything 'Press Enter'：");
        }

        private void ReceviveAllMessage(byte[] byteArray)
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

        private void SendMsgOnce(Socket socket, CommandEnum command, string sendMsg)
        {
            //轉換成物件再送出
            var infoDatas = new MessageInfoData[]
            {
                new MessageInfoData
                {
                    Message = sendMsg,
                }
            };

            Send(socketClient, Packet.BuildPacket((int)command, MessageStreamHelper.CreateStream(MessageAck.Success, infoDatas)));
            sendDone.WaitOne();
        }

        private void ShowLogOnResult(string str)
        {
            //不同執行序的寫法
            tbResult.InvokeIfRequired(() =>
            {
                tbResult.AppendText(str + Environment.NewLine);
            });
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

        #region AsynchronousClient

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        private void StartClientAndLogin()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the
                // remote device is "host.contoso.com".
                IPHostEntry ipHostInfo = Dns.GetHostEntry("127.0.0.1");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

                // Create a TCP/IP socket.  
                socketClient = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                socketClient.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), socketClient);
                connectDone.WaitOne();

                // Send test data to the remote device.  
                Send(socketClient, Packet.BuildPacket((int)CommandEnum.LoginAuth, UserReqLoginPayload.CreatePayload(GenUserInfo())));
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(socketClient);
                receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                ShowLogOnResult(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                ShowLogOnResult($"Socket connected to {client.RemoteEndPoint.ToString()}");

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                ShowLogOnResult(e.ToString());
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                ShowLogOnResult(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //持續收到封包直到結束
                    ShowLogOnResult($"Get {bytesRead} bytes from socket.");

                    if (!state.isCorrectPack)
                    {
                        //檢查是不是正常封包 第一會檢查CRC 有的話就改成TRUE
                        //如果沒有CRC那就直接拒絕接收
                        Packet.UnPackParam(state.buffer, out var crc, out var dataLen, out var command);
                        if (crc == Packet.crcCode)
                        {
                            //設定封包驗證通過(如果有要接收第二段就會繼續接收)
                            state.isCorrectPack = true;
                            //設定封包的長度(第一次的時候)，要減少前面的crc dataLen command
                            state.PacketNeedReceiveLen = dataLen;
                            //設定接收封包大小
                            state.infoBytes = new byte[dataLen];
                            //設定封包的指令(第一次的時候)
                            state.Command = command;
                        }
                        else
                        {
                            //如果CRC不對就不動作(先不關閉)
                            ShowLogOnResult("CRC check fail");
                            return;
                        }
                    }
                    //減去已收到的封包數
                    //將收到的封包複製到infoBytes，從最後收到的位置
                    state.PacketNeedReceiveLen -= bytesRead;
                    Array.Copy(state.buffer, 0, state.infoBytes, state.LastReceivedPos, bytesRead);
                    //接收完後更新對應的LastReceivedPos
                    state.LastReceivedPos += bytesRead;

                    if (state.PacketNeedReceiveLen == 0)
                    {
                        //執行對應的FUNC
                        if (!CommandRespDict.TryGetValue(state.Command, out var mappingFunc))
                        {
                            ShowLogOnResult("Not found mapping command function.");
                        };
                        ShowLogOnResult($"Get all bytes {state.infoBytes.Length} from socket.");
                        //接收完成
                        receiveDone.Set();
                        //傳送資料給對應的Command，扣掉前面的CRC,DataLen,Command
                        mappingFunc(state.infoBytes.Skip(Packet.VerificationLen).ToArray());
                        //清除封包資訊 重設
                        /*state.LastReceivedPos = 0;
                        state.PacketNeedReceiveLen = 0;
                        state.isCorrectPack = false;
                        state.infoBytes = null;
                        state.Command = 0;
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);*/
                        Receive(socketClient);
                        receiveDone.WaitOne();
                    }
                    else
                    {
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (Exception e)
            {
                ShowLogOnResult(e.ToString());
            }
        }

        private void Send(Socket client, byte[] byteData)
        {
            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                ShowLogOnResult($"Sent {bytesSent} bytes to server.");

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                ShowLogOnResult(e.ToString());
            }
        }

        private void SocketShutDown(Socket socket)
        {
            // Release the socket.  
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        #endregion
    }

    public static class Extension
    {
        //非同步委派更新UI
        public static void InvokeIfRequired(this Control control, MethodInvoker action)
        {
            if (control.InvokeRequired)//在非當前執行緒內 使用委派
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
