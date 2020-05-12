using Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class StartServer
    {
        //監聽用的PORT
        private const int portNum = 9987;
        private const string hostIP = "127.0.0.1";
        static Socket Sockets = null;
        static Dictionary<string, Socket> ClientConnectDict = new Dictionary<string, Socket>();
        static List<MessageInfoData> tempMsg = new List<MessageInfoData>();
        static void Main(string[] args)
        {
            //IPAddress ip = IPAddress.Parse(hostIP);
            IPAddress ip = IPAddress.Any;
            //綁定到IPEndPoint上
            IPEndPoint ipe = new IPEndPoint(ip, portNum);

            Sockets = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Sockets.Bind(ipe);
            //監聽數量
            Sockets.Listen(20);

            //抓資料
            GetTempData();

            Thread t1 = new Thread(Connecting);
            t1.IsBackground = true;
            t1.Start();

            Console.WriteLine("Listing Now... Press any key close server.");
            Console.ReadKey();
            Sockets.Close();
        }

        static private bool GetTempData()
        {
            //從DB或REDIS撈資料
            //先從Redis Api 尋找資料 有的話就回傳整串 沒有就從DB撈

            return false;
        }

        static void Connecting()
        {
            Socket connection = null;

            while (true)
            {
                try
                {
                    connection = Sockets.Accept();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }

                ClientConnectDict.Add(connection.RemoteEndPoint.ToString(), connection);
                Console.WriteLine($"Clinet {connection.RemoteEndPoint} connect success. Connect count:{ClientConnectDict.Count}");

                IPAddress clientIP = ((IPEndPoint)connection.RemoteEndPoint).Address;
                int clientPort = ((IPEndPoint)connection.RemoteEndPoint).Port;

                //通知連線完成
                string sendMsg = $" ClientIP:{clientIP} , Port:{clientPort} connect success.";
                connection.Send(Encoding.UTF8.GetBytes(sendMsg));

                //使用Server平常用的方式Payload做串流資料傳送過去
                connection.Send(MessageStreamHelper.CreateStream(MessageAck.Success, tempMsg.ToArray()));

                Thread t2 = new Thread(Received);
                t2.IsBackground = true;
                t2.Start(connection);
            }
        }

        static void Received(object socketclientpara)
        {
            Socket socket = (Socket)socketclientpara;

            while (true)
            {
                byte[] buffer = new byte[1024 * 1024];

                try
                {
                    int length = socket.Receive(buffer);

                    string receviedStr = Encoding.UTF8.GetString(buffer, 0, length);

                    Console.WriteLine($"Clinet:{socket.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, Msg:{receviedStr}");

                    if (ClientConnectDict.Count > 0)
                    {
                        foreach (var socketTemp in ClientConnectDict)
                        {
                            if (socketTemp.Key == socket.RemoteEndPoint.ToString()) continue;
                            socketTemp.Value.Send(Encoding.UTF8.GetBytes($"[{socket.RemoteEndPoint}]:{receviedStr}"));
                        }
                    }
                }
                catch (Exception)
                {
                    ClientConnectDict.Remove(socket.RemoteEndPoint.ToString());
                    Console.WriteLine($"Client:{socket.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
                    socket.Close();
                    break;
                }
            }
        }

    }
}
