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
        //監聽用的PORT
        private const int portNum = 9987;
        private const string hostIP = "127.0.0.1";

        static void Main(string[] args)
        {
            try
            { 
                IPAddress ip = IPAddress.Parse(hostIP);
                IPEndPoint ipe = new IPEndPoint(ip, portNum);

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

        public static void Received()
        {
            bool isMsg = false;
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024];

                    int length = SocketClient.Receive(buffer);
 
                    string strRevMsg = Encoding.UTF8.GetString(buffer, 0, length);
                    if (isMsg)
                    {
                        Console.WriteLine($"Server：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff} Msg:{strRevMsg}");

                    }
                    else
                    {
                        Console.WriteLine(strRevMsg + "\r\n");
                        isMsg = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server is disconnect！{ex.Message}");
                    break;
                }
            }
        }

        public static void ClientSendMsg(string sendMsg)
        {     
            byte[] msgArray = Encoding.UTF8.GetBytes(sendMsg); 
            SocketClient.Send(msgArray);
        }
    }
}
