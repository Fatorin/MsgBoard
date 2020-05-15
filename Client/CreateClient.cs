using Common;
using Common.Command;
using Common.Setting;
using Common.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class CreateClient
    {
        static Thread ThreadClient = null;
        static Socket SocketClient = null;
        static Dictionary<byte, Func<Socket, byte[], bool>> CommandRespDict = new Dictionary<byte, Func<Socket, byte[], bool>>();
        public void Start()
        {
            //懶得寫負載平衡 先這樣
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

            //輸入帳密判斷
            CheckAndGenUserInfo(out var userInfoData);
            Console.WriteLine("Logging in");
            try
            {
                IPAddress ip = IPAddress.Parse(GlobalSetting.LocalIP);
                IPEndPoint ipe = new IPEndPoint(ip, serverPort);

                SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ConfigureTcpSocket(SocketClient);

                try
                {
                    SocketClient.Connect(ipe);
                }
                catch (Exception)
                {
                    Console.WriteLine("Connect Fail.");
                    Console.ReadLine();
                    return;
                }

                //送出帳號密碼 要改成用專用送出
                SendCommand(SocketClient,Command.LoginAuth,UserStreamHelper.CreateStream(UserAck.Success, userInfoData));
                //等待接收登入是否成功
                if (!ReceivedLogin())
                {
                    Console.WriteLine("Login Fail, will close connect.");
                    SocketClient.Close();
                }

                //登入成功才會進入監聽結果
                /*ThreadClient = new Thread(ReceiveCommand);
                ThreadClient.IsBackground = true;
                ThreadClient.Start();*/

                Console.WriteLine("Please Type Anything 'Press Enter'：");
                //輸送所有指令給伺服器
                while (true)
                {
                    string sendStr = Console.ReadLine();
                    //ClientSendMsg(sendStr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private void ConfigureTcpSocket(Socket tcpSocket)
        {
            // Don't allow another socket to bind to this port.
            tcpSocket.ExclusiveAddressUse = true;

            // The socket will linger for 10 seconds after
            // Socket.Close is called.
            tcpSocket.LingerState = new LingerOption(true, 10);

            // Disable the Nagle Algorithm for this tcp socket.
            tcpSocket.NoDelay = true;

            // Set the receive buffer size to 8k
            tcpSocket.ReceiveBufferSize = 8192;

            // Set the timeout for synchronous receive methods to
            // 1 second (1000 milliseconds.)
            tcpSocket.ReceiveTimeout = 1000;

            // Set the send buffer size to 8k.
            tcpSocket.SendBufferSize = 8192;

            // Set the timeout for synchronous send methods
            // to 1 second (1000 milliseconds.)
            tcpSocket.SendTimeout = 1000;
        }

        private void InitCommandMapping()
        {
            CommandRespDict = new Dictionary<byte, Func<Socket, byte[] , bool>>()
            {
                { (byte)Command.GetMsgAll, ReceviveAllMessage},
            };
        }
        private void CheckAndGenUserInfo(out UserInfoData infoData)
        {
            Console.WriteLine("Welcome use message board.");
            Console.WriteLine("Please Enter Your ID");
            var userId = Console.ReadLine();
            Console.WriteLine("Please Enter Your Password");
            var userPwd = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    userPwd += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && userPwd.Length > 0)
                    {
                        userPwd = userPwd.Substring(0, (userPwd.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                }
            } while (true);

            infoData = new UserInfoData
            {
                UserId = userId,
                UserPwd = userPwd,
            };
        }
        private void SendCommand(Socket socket, Command command, byte[] dataArray)
        {
            socket.Send(CommandStreamHelper.CreateCommandAndData(command, dataArray));
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
                        Console.WriteLine("Not found mapping function.");
                        //不接受這個封包
                        continue;
                    };
                    //過濾第一個字並拿取封包長度去掉Command的部分
                    mappingFunc(socket, buffer.Skip(CommandStreamHelper.CommandSize).Take(length - CommandStreamHelper.CommandSize).ToArray());
                }
                catch (Exception)
                {
                    Console.WriteLine($"Packet has problem.");
                    socket.Close();
                    break;
                }
            }
        }

        private bool ReceivedLogin()
        {
            bool isLoginSuccess = false;
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[4];

                    SocketClient.Receive(buffer);
                    UserStreamHelper.GetAck(buffer, out var ack);
                    if (ack != UserAck.Success)
                    {
                        Console.WriteLine($"{nameof(ReceivedLogin)} Fail, Ack={ack}");
                        isLoginSuccess = false;
                        break;
                    }

                    isLoginSuccess = true;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server is disconnect！{ex.Message}");
                    break;
                }
            }
            return isLoginSuccess;
        }

        private bool ReceviveAllMessage(Socket socket, byte[] byteArray)
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[512];
                    int length = SocketClient.Receive(buffer);

                    Console.WriteLine($"ReceivedAll Length:{length}");

                    MessageStreamHelper.GetStream(buffer, out var ack, out var infoDatas);

                    if (ack != MessageAck.Success)
                    {
                        Console.WriteLine($"{nameof(ReceviveAllMessage)} Fail, Ack={ack}");
                        break;
                    }
                    else
                    {
                        foreach (MessageInfoData infoData in infoDatas)
                        {
                            Console.WriteLine($"Msg:{infoData.Message}");
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }
            return true;
        }

        private bool ReceivedMsgOnce(Socket socket, byte[] dataArray)
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024];

                    int length = SocketClient.Receive(buffer);

                    string strRevMsg = Encoding.UTF8.GetString(buffer, 0, length);

                    Console.WriteLine($"Server：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} Msg:{strRevMsg}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server is disconnect！{ex.Message}");
                    break;
                }
            }
            return true;
        }

        private void ClientSendMsg(string sendMsg)
        {
            byte[] msgArray = Encoding.UTF8.GetBytes(sendMsg);
            SocketClient.Send(msgArray);
        }
    }
}
