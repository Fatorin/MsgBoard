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

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbInput.Text))
            {
                ShowLogOnResult($"Enter something, do not enter spaces or blanks.");
                return;
            }

            bgWorkerGoFunc((int)CommandEnum.MsgOnce);
        }

        private void SendMsgOnce()
        {
            var msgInfo = new MessageInfoData[]
            {
                new MessageInfoData{ Message=tbInput.Text },
            };

            tbInput.InvokeIfRequired(() =>
            {
                tbInput.Text = "";
            });
            Send(socketClient, Packet.BuildPacket((int)CommandEnum.MsgOnce, MessageReqPayload.CreatePayload(msgInfo)));
            sendDone.WaitOne();
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
                {(int)CommandEnum.LoginAuth, StartClientAndLogin},
                {(int)CommandEnum.MsgOnce, SendMsgOnce}
            };

            CommandRespDict = new Dictionary<int, Action<byte[]>>()
            {
                { (int)CommandEnum.LoginAuth, ReceivedLogin},
                { (int)CommandEnum.MsgAll, ReceviveAllMessage},
                { (int)CommandEnum.MsgOnce, ReceviveAllMessage},
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
            SetLoginAndSendUI(false, true);
            ShowLogOnResult("Please Type Anything 'Press Enter'：");
        }

        private void ReceviveAllMessage(byte[] byteArray)
        {
            MessageRespPayload.ParsePayload(byteArray, out var ack, out var infoDatas);

            if (ack != MessageAck.Success)
            {
                ShowLogOnResult($"{nameof(ReceviveAllMessage)} Fail, Ack={ack}");
                return;
            }

            foreach (MessageInfoData infoData in infoDatas)
            {
                ShowLogOnResult($"Msg:{infoData.Message}");
            }
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
            gbLogin.InvokeIfRequired(() =>
            {
                gbLogin.Visible = showLogin;
            });

            gbInput.InvokeIfRequired(() =>
            {
                gbInput.Visible = showSend;
            });
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
                PacketObj packetObj = new PacketObj();
                packetObj.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(packetObj.buffer, 0, PacketObj.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), packetObj);
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
                PacketObj packetObj = (PacketObj)ar.AsyncState;
                Socket client = packetObj.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    if (!packetObj.isCorrectPack)
                    {
                        //檢查是不是正常封包 第一會檢查CRC 有的話就改成TRUE
                        //如果沒有CRC那就直接拒絕接收
                        Packet.UnPackParam(packetObj.buffer, out var crc, out var dataLen, out var command);
                        if (crc == Packet.crcCode)
                        {
                            packetObj.SetFirstReceive(dataLen, command);
                        }
                        else
                        {
                            //如果CRC不對就不動作(先不關閉)
                            ShowLogOnResult("CRC check fail");
                            throw new Exception();
                        }
                    }
                    //將收到的封包複製到infoBytes，從最後收到的位置
                    Array.Copy(packetObj.buffer, 0, packetObj.infoBytes, packetObj.LastReceivedPos, bytesRead);
                    //減去已收到的封包數
                    //接收完後更新對應的LastReceivedPos
                    packetObj.SetContiuneReceive(bytesRead);

                    if (packetObj.PacketNeedReceiveLen == 0)
                    {
                        //執行對應的FUNC
                        if (CommandRespDict.TryGetValue(packetObj.Command, out var mappingFunc))
                        {
                            mappingFunc(packetObj.infoBytes.Skip(Packet.VerificationLen).ToArray());
                        }
                        else
                        {
                            ShowLogOnResult("Not mapping function.");
                        }
                        //接收完成
                        receiveDone.Set();                        
                        //開始接收新的封包
                        Receive(socketClient);
                        receiveDone.WaitOne();
                    }
                    else
                    {
                        client.BeginReceive(packetObj.buffer, 0, PacketObj.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), packetObj);
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

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                ShowLogOnResult("發送失敗");
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
