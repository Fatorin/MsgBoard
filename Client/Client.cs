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
            Login();
        }

        public void Login()
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

            StartClient();
            
            // Send test data to the remote device.  
            Send(socketClient, Packet.BuildPacket((int)CommandEnum.LoginAuth, UserReqLoginPayload.CreatePayload(GenUserInfo())));
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(socketClient);
            receiveDone.WaitOne();
        }
        private void InitCommandMapping()
        {
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

        private static void StartClient()
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
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
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
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
                    Console.WriteLine($"Get {bytesRead} bytes from socket.");

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    Console.WriteLine($"Read {state.infoBytes.Length} bytes from socket.");
                    //收封包
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, byte[] byteData)
        {
            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void SocketShutDown(Socket socket)
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
