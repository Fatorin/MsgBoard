using Common;
using Common.Command;
using Common.Packet;
using Common.Setting;
using Common.User;
using Newtonsoft.Json;
using Server.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class StartServer
    {
        private static ConcurrentDictionary<string, Socket> ClientConnectDict;
        private static Dictionary<int, Action<Socket, byte[]>> CommandRespDict;
        private static int UsePort;
        private static PGSql.ApplicationContext dbContext;

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

            Console.WriteLine("\nPress ENTER to exit...");
            Console.ReadKey();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            Console.WriteLine("Accept one connect.");
            PacketObj state = new PacketObj();
            ClientConnectDict.TryAdd(handler.RemoteEndPoint.ToString(), handler);
            Console.WriteLine($"Client:{handler.RemoteEndPoint.ToString()} success. AcceptCount:{ClientConnectDict.Count}");
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, PacketObj.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            // Signal the main thread to continue.  
            allDone.Set();
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            PacketObj packetObj = (PacketObj)ar.AsyncState;
            Socket handler = packetObj.workSocket;
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
                            //如果CRC不對就不動作 應該要踢掉 代表這個使用者有問題
                            Console.WriteLine("CRC check fail, make new exception.");
                            throw new Exception();
                        }
                    }

                    //將收到的封包複製到infoBytes，從最後收到的位置     
                    Array.Copy(packetObj.buffer, 0, packetObj.infoBytes, packetObj.LastReceivedPos, bytesRead);
                    packetObj.SetContiuneReceive(bytesRead);

                    //如果封包都收完了 則執行動作
                    if (packetObj.PacketNeedReceiveLen == 0)
                    {
                        //執行對應的FUNC
                        if (CommandRespDict.TryGetValue(packetObj.Command, out var mappingFunc))
                        {
                            //傳送資料給對應的Command，扣掉前面的CRC,DataLen,Command
                            mappingFunc(handler, packetObj.infoBytes.Skip(Packet.VerificationLen).ToArray());
                        }
                        else
                        {
                            //有對應的Function就執行，沒對應的Func就報錯但會繼續執行
                            Console.WriteLine("Not mapping function.");
                        }
                        //清除封包資訊 重設
                        packetObj.ResetData();
                        //重設後再次接收
                        handler.BeginReceive(packetObj.buffer, 0, PacketObj.BufferSize, 0,
                        new AsyncCallback(ReadCallback), packetObj);
                    }
                    else
                    {
                        // Not all data received. Get more.
                        handler.BeginReceive(packetObj.buffer, 0, PacketObj.BufferSize, 0,
                        new AsyncCallback(ReadCallback), packetObj);
                    }
                }
            }
            catch (Exception e)
            {
                //接收時如果發生錯誤則做以下處理
                Console.WriteLine($"Remove {handler.RemoteEndPoint}, AcceptCount:{ClientConnectDict.Count}");
                ClientConnectDict.TryRemove(handler.RemoteEndPoint.ToString(), out var _);
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
            var ackCode = UserAck.Success;
            var user = dbContext.Users.Find(infoData.UserId);
            //dbContext.Users.Single(u => u.UserId == infoData.UserId);
            //如果用戶不存在則自動幫他創帳號
            if (user == null)
            {
                Console.WriteLine("not found equal userid, created new user.");
                user = new UserInfoData
                {
                    UserId = infoData.UserId,
                    UserPwd = infoData.UserPwd
                };
                dbContext.Users.Add(user);
                dbContext.SaveChanges();
            }

            //建立完帳戶、確認用戶帳密是否一致
            if (infoData.UserPwd != user.UserPwd)
            {
                ackCode = UserAck.AuthFail;
                ClientConnectDict.TryRemove(handler.RemoteEndPoint.ToString(), out var _);
                Console.WriteLine($"Remove {handler.RemoteEndPoint}, AcceptCount:{ClientConnectDict.Count}");
            }
            //驗證成功就通知另一個伺服器把人踢了(這邊要用Redis做)
            //要重寫與另一個SERVER溝通的方法
            //回傳成功訊息給對應的人
            Send(handler, Packet.BuildPacket((int)CommandEnum.LoginAuth, UserRespLoginPayload.CreatePayload(ackCode)));

            //回傳留言版資料
            var values = GetRedisDb(RedisHelper.RedisLinkNumber.MsgData).ListRange(GetRedisDataKey(), -100, -1);
            if (ackCode != UserAck.AuthFail && values.Length > 0)
            {
                var MsgInfoList = new List<MessageInfoData>();
                foreach (string value in values)
                {
                    MsgInfoList.Add(new MessageInfoData { Message = value });
                }
                Send(handler, Packet.BuildPacket((int)CommandEnum.MsgAll, MessageRespPayload.CreatePayload(MessageAck.Success, MsgInfoList.ToArray())));
            }
        }

        private static void ReceviveOneMessage(Socket handler, byte[] byteArray)
        {
            MessageReqPayload.ParsePayload(byteArray, out var infoDatas);
            //理論上只有第一筆訊息 懶得分開寫
            //驗證訊息用而已 連這段轉換都不用寫
            Console.WriteLine($"Clinet:{handler.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, Msg:{infoDatas[0].Message}");
            //存入Redis
            SaveOneInfoDataToRedis(GetRedisDb(RedisHelper.RedisLinkNumber.MsgData), infoDatas[0]);
            foreach (var socketTemp in ClientConnectDict)
            {
                //伺服器接收到的資料
                Send(socketTemp.Value, Packet.BuildPacket((int)CommandEnum.MsgOnce, MessageRespPayload.CreatePayload(MessageAck.Success, infoDatas.ToArray())));
            }
        }

        private static void RecevivecKick(Socket handler, byte[] byteArray)
        {
            ClientConnectDict.TryGetValue(Encoding.UTF8.GetString(byteArray), out var socketTemp);
            ClientConnectDict.TryRemove(socketTemp.RemoteEndPoint.ToString(), out var _);
            Console.WriteLine($"Client:{socketTemp.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
            Disconnect(socketTemp);
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

        private static void SaveOneInfoDataToRedis(IDatabase redisDb, MessageInfoData infoData)
        {
            //直接新增到最末端
            redisDb.ListRightPush(GetRedisDataKey(), JsonConvert.SerializeObject(infoData.Message));
        }

        private static void SaveMultiInfoDataToRedis(IDatabase redisDb, string key, List<MessageInfoData> infoDatas)
        {
            //儲存所有的資訊到Redis
            for (int i = 0; i < infoDatas.Count; i++)
            {
                redisDb.ListRightPush(key, infoDatas[i].Message);
            }
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
        }

        private static void InitMapping()
        {
            ClientConnectDict = new ConcurrentDictionary<string, Socket>();
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
                    Message = $"testString{i}"
                });
            }
            SaveMultiInfoDataToRedis(GetRedisDb(RedisHelper.RedisLinkNumber.MsgData), GetRedisDataKey(), dataList);
        }


        public static void Main(String[] args)
        {
            UsePort = GlobalSetting.PortNum1;
            dbContext = new PGSql.ApplicationContext();
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
