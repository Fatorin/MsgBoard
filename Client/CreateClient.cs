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
        static Dictionary<byte, Action<Socket, byte[]>> CommandRespDict = new Dictionary<byte, Action<Socket, byte[]>>();
        static bool isLoginSuccess;
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
            InitCommandMapping();
            //輸入帳密判斷
            CheckAndGenUserInfo(out var userInfoData);
            try
            {
                IPAddress ip = IPAddress.Parse(GlobalSetting.LocalIP);
                IPEndPoint ipe = new IPEndPoint(ip, serverPort);

                SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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

                //進入監聽
                ThreadClient = new Thread(ReceiveCommand);
                ThreadClient.IsBackground = true;
                ThreadClient.Start(SocketClient);

                //送出帳號密碼 要改成用專用送出
                SendCommand(SocketClient, Command.LoginAuth, UserReqLoginPayload.CreatePayload(userInfoData));

                while(isLoginSuccess)
                {
                    //等待驗證中
                }
                Console.WriteLine("Please Type Anything 'Press Enter'：");
                while (true)
                {
                    string sendStr = Console.ReadLine();
                    SendMsgOnce(SocketClient, Command.GetMsgOnce, sendStr);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                    mappingFunc(socket, buffer.Skip(CommandStreamHelper.CommandSize).Take(length).ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"Packet has problem.");
                    socket.Close();
                    break;
                }
            }
        }

        private void ReceivedLogin(Socket socket,byte[] DataArray)
        {
            UserRespLoginPayload.ParsePayload(DataArray, out var ack);
            if (ack != UserAck.Success)
            {
                Console.WriteLine($"{nameof(ReceivedLogin)} Fail, Ack={ack}");
                //重新輸入帳密並登入 暫時不限次數
                Console.WriteLine($"Please check you Id and Password.");
                CheckAndGenUserInfo(out var userInfoData);
                SendCommand(socket, Command.LoginAuth, UserReqLoginPayload.CreatePayload(userInfoData));
                return;
            }
            isLoginSuccess = true;
        }

        private void ReceviveAllMessage(Socket socket, byte[] byteArray)
        {
             MessageStreamHelper.GetStream(byteArray, out var ack, out var infoDatas);

            if (ack != MessageAck.Success)
            {
                Console.WriteLine($"{nameof(ReceviveAllMessage)} Fail, Ack={ack}");
            }
            
            foreach (MessageInfoData infoData in infoDatas)
            {
                Console.WriteLine($"Msg:{infoData.Message}");
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
    }
}
