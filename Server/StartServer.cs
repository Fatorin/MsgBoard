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
        private static Dictionary<int, Action<Socket, byte[]>> CommandRespDict;
        private static List<MessageInfoData> tempMsg = new List<MessageInfoData>();
        private static int UsePort;

        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public StartServer()
        {
        }

        #region AsynchronousSocketListener

        public static void StartListening()
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, UsePort);

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
            Console.ReadKey();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            Console.WriteLine("Accept one connect.");
            StateObject state = new StateObject();
            ClientConnectDict.Add(handler.RemoteEndPoint.ToString(), handler);
            Console.WriteLine($"Client:{handler.RemoteEndPoint.ToString()} success. AcceptCount:{ClientConnectDict.Count}");
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            try
            {

                //從socket接收資料
                int bytesRead = handler.EndReceive(ar);
                //如果有收到的封包才會做事情
                if (bytesRead > 0)
                {
                    //如果收到結束符號才會停止
                    //檢查有沒有CRC 有的話再嘗試接收封包 不然就拒絕
                    //檢查封包長度 傳值給PacketLength
                    //定義PacketLength
                    //如果資料少於PacketLength，會持續接收，並減少前面接收的長度
                    //如果資料都收完了，那麼會結束判斷，並做後續的動作
                    //DO SOMETHING FOR PACK LEN

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
                            Console.WriteLine("CRC check fail");
                            return;
                        }
                    }
                    //減去已收到的封包數
                    //將收到的封包複製到infoBytes，從最後收到的位置
                    state.PacketNeedReceiveLen -= bytesRead;
                    Array.Copy(state.buffer, 0, state.infoBytes, state.LastReceivedPos, bytesRead);
                    //接收完後更新對應的LastReceivedPos
                    state.LastReceivedPos += bytesRead;

                    //如果封包都收完了 則執行動作
                    if (state.PacketNeedReceiveLen == 0)
                    {
                        // All the data has been read from the
                        // client. Display it on the console.  
                        Console.WriteLine($"Read {state.infoBytes.Length} bytes from socket.");

                        //執行對應的FUNC
                        if (!CommandRespDict.TryGetValue(state.Command, out var mappingFunc))
                        {
                            Console.WriteLine("Not found mapping command function.");

                        };
                        //傳送資料給對應的Command，扣掉前面的CRC,DataLen,Command
                        mappingFunc(handler, state.infoBytes.Skip(Packet.VerificationLen).ToArray());
                        //清除封包資訊 重設
                        state.LastReceivedPos = 0;
                        state.PacketNeedReceiveLen = 0;
                        state.isCorrectPack = false;
                        state.infoBytes = null;
                        state.Command = 0;
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                    else
                    {
                        // Not all data received. Get more.
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch(Exception e)
            {
                //接收時如果對方斷線則做以下處理
                Console.WriteLine(e.Message);
                ClientConnectDict.Remove(handler.RemoteEndPoint.ToString());
                handler.Close();
            }
        }

        private static void Send(Socket handler, byte[] byteData)
        {
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        #endregion

        private static void ReceviveLoginAuthData(Socket handler, byte[] byteArray)
        {
            UserReqLoginPayload.ParsePayload(byteArray, out var infoData);
            Console.WriteLine($"Clinet:{handler.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, UserId:{infoData.UserId}");
            //這邊是帳密驗證的部分 邏輯還沒寫//db還沒撈
            //而且要驗證這帳號有沒有在裡面 有的話就踢掉另一邊的連線
            var id = infoData.Id;
            var userid = infoData.UserId;
            var pw = infoData.UserPwd;
            var ackCode = UserAck.Success;
            if (infoData.UserId != "999")
            {
                ackCode = UserAck.AuthFail;
            }
            //驗證成功就通知另一個伺服器把人踢了
            //要重寫與另一個SERVER溝通的方法
            //回傳成功訊息給對應的人
            Send(handler, Packet.BuildPacket((int)CommandEnum.LoginAuth, UserRespLoginPayload.CreatePayload(ackCode)));

            if (ackCode == UserAck.AuthFail)
            {
                ClientConnectDict.Remove(handler.RemoteEndPoint.ToString());
                Console.WriteLine($"Remove {handler.RemoteEndPoint.ToString()}, AcceptCount:{ClientConnectDict.Count}");
                return;
            }
            //回傳留言版資料
            if (tempMsg.Count != 0 && ackCode != UserAck.AuthFail)
            {
                Send(handler, Packet.BuildPacket((int)CommandEnum.MsgAll, MessageRespPayload.CreatePayload(MessageAck.Success, tempMsg.ToArray())));
            }
        }

        private static void ReceviveOneMessage(Socket handler, byte[] byteArray)
        {
            MessageReqPayload.ParsePayload(byteArray, out var infoDatas);
            //理論上只有第一筆訊息 懶得分開寫
            //驗證訊息用而已 連這段轉換都不用寫
            string receviedStr = infoDatas[0].Message;
            Console.WriteLine($"Clinet:{handler.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, Msg:{receviedStr}");
            if (ClientConnectDict.Count > 0)
            {
                foreach (var socketTemp in ClientConnectDict)
                {
                    //不傳送給發話人
                    if (socketTemp.Key == handler.RemoteEndPoint.ToString()) continue;
                    //伺服器接收到的資料
                    Send(socketTemp.Value, Packet.BuildPacket((int)CommandEnum.MsgOnce, byteArray));
                }
            }
        }

        private static void RecevivecKick(Socket handler, byte[] byteArray)
        {
            ClientConnectDict.TryGetValue(Encoding.UTF8.GetString(byteArray), out var socketTemp);
            ClientConnectDict.Remove(socketTemp.RemoteEndPoint.ToString());
            Console.WriteLine($"Client:{socketTemp.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
            socketTemp.Close();
        }

        private static void Disconnect(Socket handler)
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
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
            ClientConnectDict = new Dictionary<string, Socket>();
            CommandRespDict = new Dictionary<int, Action<Socket, byte[]>>()
                {
                    { (int)CommandEnum.LoginAuth, ReceviveLoginAuthData},
                    { (int)CommandEnum.MsgOnce, ReceviveOneMessage},
                    { (int)CommandEnum.LoginKick, RecevivecKick},
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

        public static void Main(String[] args)
        {
            UsePort = GlobalSetting.PortNum1;
            Init();
            /*if (PortInUse(GlobalSetting.PortNum1))
            {
                UsePort = GlobalSetting.PortNum2;
            }

            if (PortInUse(GlobalSetting.PortNum1) && PortInUse(GlobalSetting.PortNum2))
            {
                Console.WriteLine("Port are used.");
                Console.ReadKey();
                return;
            }*/

            Console.WriteLine($"Port use {UsePort}");

            StartListening();
            return;
        }
    }
}
