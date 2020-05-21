using Common;
using Common.Command;
using Common.Packet;
using Common.Setting;
using Common.User;
using Newtonsoft.Json;
using Server.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class StartServer
    {
        private static Dictionary<string, Socket> ClientConnectDict;
        private static Dictionary<byte, Action<Socket, byte[]>> CommandRespDict;
        private static List<MessageInfoData> tempMsg = new List<MessageInfoData>();

        public class StateObject
        {
            // Client  socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 1024;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];

            public int PacketNeedReceiveLen;
        }

        public class AsynchronousSocketListener
        {
            // Thread signal.  
            public static ManualResetEvent allDone = new ManualResetEvent(false);

            public AsynchronousSocketListener()
            {
            }

            public static void StartListening()
            {
                // Establish the local endpoint for the socket.  
                // The DNS name of the computer  
                // running the listener is "host.contoso.com".  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP socket.  
                Socket listener = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Bind the socket to the local endpoint and listen for incoming connections.  
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (true)
                    {
                        // Set the event to nonsignaled state.  
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.  
                        Console.WriteLine("Waiting for a connection...");
                        listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            listener);

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("\nPress ENTER to continue...");
                Console.Read();

            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                // Signal the main thread to continue.  
                allDone.Set();

                // Get the socket that handles the client request.  
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }

            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;
                
                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                //從socket接收資料
                int bytesRead = handler.EndReceive(ar);
                //如果有收到的封包才會做事情
                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.

                    // Check for end-of-file tag. If it is not there, read
                    // more data.  
                    //如果收到結束符號才會停止
                    //檢查有沒有CRC 有的話再嘗試接收封包 不然就拒絕
                    //檢查封包長度 傳值給PacketLength
                    //定義PacketLength
                    //如果資料少於PacketLength，會持續接收，並減少前面接收的長度
                    //如果資料都收完了，那麼會結束判斷，並做後續的動作
                    if (state.PacketNeedReceiveLen == 0)
                    {
                        //如果封包長度是0，那要接收
                    }
                    //改成收到結束的長度才會結束
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        // All the data has been read from the
                        // client. Display it on the console.  
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            content.Length, content);
                        // Echo the data back to the client.  
                        Send(handler, content);
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }
            }

            private static void Send(Socket handler, String data)
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.  
                    Socket handler = (Socket)ar.AsyncState;

                    // Complete sending the data to the remote device.  
                    int bytesSent = handler.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
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
                        //如果出錯就移除該用戶在連線表的資訊並移除該用戶的連線
                        ClientConnectDict.Remove(socket.RemoteEndPoint.ToString());
                        Console.WriteLine($"Client:{socket.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
                        socket.Close();
                        break;
                    }
                }
            }

            private static void ReceviveLoginAuthData(Socket socket, byte[] byteArray)
            {
                UserReqLoginPayload.ParsePayload(byteArray, out var infoData);
                Console.WriteLine($"Clinet:{socket.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, UserId:{infoData.UserId}");
                //這邊是帳密驗證的部分 邏輯還沒寫//db還沒撈
                //而且要驗證這帳號有沒有在裡面 有的話就踢掉另一邊的連線
                var id = infoData.UserId;
                var pw = infoData.UserPwd;
                var ackCode = UserAck.Success;
                if (infoData.UserId != "999")
                {
                    ackCode = UserAck.AuthFail;
                }
                //驗證成功就通知另一個伺服器把人踢了
                //要重寫與另一個SERVER溝通的方法
                //回傳成功訊息給對應的人
                SendCommand(socket, Command.LoginAuth, UserRespLoginPayload.CreatePayload(ackCode));
                //回傳留言版資料
                if (tempMsg.Count != 0)
                {
                    //SendCommand(socket, Command.GetMsgAll, MessageStreamHelper.CreateStream(MessageAck.Success, tempMsg.ToArray()));
                }
            }

            private static void ReceviveOneMessage(Socket socket, byte[] byteArray)
            {
                MessageStreamHelper.GetStream(byteArray, out var ackCode, out var infoDatas);
                //理論上只有第一筆訊息 懶得分開寫
                //驗證訊息用而已 連這段轉換都不用寫
                string receviedStr = infoDatas[0].Message;
                Console.WriteLine($"Clinet:{socket.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, Msg:{receviedStr}");
                if (ClientConnectDict.Count > 0)
                {
                    foreach (var socketTemp in ClientConnectDict)
                    {
                        //不傳送給發話人
                        if (socketTemp.Key == socket.RemoteEndPoint.ToString()) continue;
                        //伺服器接收到的資料
                        SendCommand(socketTemp.Value, Command.GetMsgOnce, byteArray);
                    }
                }
            }

            private static void RecevivecKick(Socket socket, byte[] byteArray)
            {
                ClientConnectDict.TryGetValue(Encoding.UTF8.GetString(byteArray), out var socketTemp);
                ClientConnectDict.Remove(socketTemp.RemoteEndPoint.ToString());
                Console.WriteLine($"Client:{socketTemp.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
                socketTemp.Close();
            }

            private static void SendCommand(Socket socket, Command command, byte[] dataArray)
            {
                socket.Send(CommandStreamHelper.CreateCommandAndData(command, dataArray));
            }

            #region Redis//DB

            private static IDatabase GetRedisDb(RedisHelper.RedisLinkNumber redisLinkNumber)
            {
                return RedisHelper.Connection.GetDatabase((int)redisLinkNumber);
            }
            private static string GetRedisDataKey()
            {
                return "MessageList";
            }
            private static void GetOneInfoDataFromRedis(HashEntry entry)
            {
                var infoData = JsonConvert.DeserializeObject<MessageInfoData>(entry.Value);

                tempMsg.Add(infoData);
            }

            private static void SaveOneInfoDataToRedis(IDatabase redisDb, MessageInfoData infoData)
            {
                //找到當前行數 並新增字串
                redisDb.HashSet(GetRedisDataKey(), tempMsg.Count - 1, JsonConvert.SerializeObject(infoData));
            }

            private static void SaveMultiInfoDataToRedis(IDatabase redisDb, string key, List<MessageInfoData> infoDatas)
            {
                //儲存所有的資訊到Redis
                var hashes = new List<HashEntry>();

                for (int i = 0; i < infoDatas.Count; i++)
                {
                    hashes.Add(new HashEntry(i, JsonConvert.SerializeObject(infoDatas[i])));
                }

                redisDb.HashSet(key, hashes.ToArray());
            }
            #endregion

            public static bool PortInUse(int port)
            {
                bool inUse = false;

                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

                foreach (IPEndPoint endPoint in ipEndPoints)
                {
                    if (endPoint.Port == port)
                    {
                        inUse = true;
                        break;
                    }
                }
                return inUse;
            }
            private static void Init()
            {
                InitMapping();
                InitFakeData();
                GetTempData();
            }

            private static void InitMapping()
            {
                CommandRespDict = new Dictionary<byte, Action<Socket, byte[]>>()
                {
                    { (byte)Command.LoginAuth, ReceviveLoginAuthData},
                    { (byte)Command.GetMsgOnce, ReceviveOneMessage},
                    { (byte)Command.LoginKick, RecevivecKick},
                };
            }

            private static void InitFakeData()
            {
                List<MessageInfoData> dataList = new List<MessageInfoData>();
                for (int i = 0; i < 10; i++)
                {
                    dataList.Add(new MessageInfoData
                    {
                        Message = $"testString:{i}"
                    });
                }
                SaveMultiInfoDataToRedis(GetRedisDb(RedisHelper.RedisLinkNumber.MsgData), GetRedisDataKey(), dataList);
            }

            private static bool GetTempData()
            {
                //從DB或REDIS撈資料
                //先從Redis Api 尋找資料 有的話就回傳整串 沒有就從DB撈
                //0表示連線  1表示資料存放處
                var entries = GetRedisDb(RedisHelper.RedisLinkNumber.MsgData).HashGetAll(GetRedisDataKey());
                foreach (HashEntry entry in entries)
                {
                    GetOneInfoDataFromRedis(entry);
                }
                return false;
            }

            private void testfun()
            {
                byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                var pack = Packet.BuildPacket(123, data);
                int len = pack.Length;
                int crc = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(pack, 0));
                int dataLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(pack, 4));
                int command = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(pack, 8));
                byte[] reciveData = new byte[dataLen];
                reciveData = pack.Skip(12).ToArray();
                reciveData.Reverse();
                Console.WriteLine($"packLen={len}");
                Console.WriteLine($"crc={crc}");
                Console.WriteLine($"command={command}");
                Console.WriteLine($"DataLen={reciveData.Length}");

                for (int i = 0; i < reciveData.Length; i++)
                {
                    Console.WriteLine($"reciveData{i}={reciveData[i]}");
                }
            }
            public static void Main(String[] args)
            {
                int UsePort = GlobalSetting.PortNum1;
                Init();
                if (PortInUse(GlobalSetting.PortNum1))
                {
                    UsePort = GlobalSetting.PortNum2;
                }

                if (PortInUse(GlobalSetting.PortNum1) && PortInUse(GlobalSetting.PortNum2))
                {
                    Console.WriteLine("Port are used.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"Port use {UsePort}");
                StartListening();
                return;
            }
        }
    }
}
