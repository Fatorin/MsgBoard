using Common;
using Common.Setting;
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
    class StartClient
    {
        static Thread ThreadClient = null;
        static Socket SocketClient = null;
        static void Main(string[] args)
        {
            try
            { 
                IPAddress ip = IPAddress.Parse(GlobalSetting.LocalIP);
                IPEndPoint ipe = new IPEndPoint(ip, GlobalSetting.PortNum);

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

                ThreadClient = new Thread(Received);
                ThreadClient.IsBackground = true;
                ThreadClient.Start();

                Console.WriteLine("Please Type Anything 'Press Enter'：");
                while (true)
                {
                    string sendStr = Console.ReadLine();
                    ClientSendMsg(sendStr);
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static void ReceivedAll()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024];

                    int length = SocketClient.Receive(buffer);

                    MessageStreamHelper.GetStream(buffer, out var ack, out var infoDatas);

                    if (ack != MessageAck.Success)
                    {
                        Console.WriteLine($"{nameof(ReceivedAll)} Fail, Ack={ack}");
                        break;
                    }
                    
                    foreach(MessageInfoData infoData in infoDatas)
                    {
                        Console.WriteLine($"Msg:{infoData.Message}");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server is disconnect！{ex.Message}");
                    break;
                }
            }
        }
        private static void Received()
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
        }

        private static void ClientSendMsg(string sendMsg)
        {     
            byte[] msgArray = Encoding.UTF8.GetBytes(sendMsg); 
            SocketClient.Send(msgArray);
        }
    }
}
