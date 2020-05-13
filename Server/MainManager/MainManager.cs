using Common;
using Common.Setting;
using Server.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using IDatabase = StackExchange.Redis.IDatabase;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using Common.User;
using System.Linq;

namespace Server.MainManager
{
    public class MainManager
    {
        private static MainManager instace = null;

        public static MainManager Instance
        {
            get
            {
                return instace ?? new MainManager();
            }
        }
        private MainManager()
        {
            instace = this;
        }

        static Socket Sockets = null;
        static Dictionary<string, Socket> ClientConnectDict = new Dictionary<string, Socket>();
        static List<MessageInfoData> tempMsg = new List<MessageInfoData>();
        public void Start()
        {
            //IPAddress ip = IPAddress.Parse(hostIP);
            IPAddress ip = IPAddress.Any;
            //綁定到IPEndPoint上
            IPEndPoint ipe = new IPEndPoint(ip, GlobalSetting.PortNum1);

            Sockets = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Sockets.Bind(ipe);
            //最多連入數量
            Sockets.Listen(50);

            //去Redis//DB產生假的資料
            InitFakeData();
            GetTempData();
            
            Thread t1 = new Thread(Connecting);
            t1.IsBackground = true;
            t1.Start();

            Console.WriteLine("Listing Now...");

            while (true)
            {
                var cmd = Console.ReadLine();
                if (cmd.ToLower() == "exit")
                {
                    Sockets.Close();
                    break;
                }
                else
                {
                    Console.WriteLine("Not support command.");
                }
            }
        }

        private void Connecting()
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

                //比對帳密
                byte[] buffer = new byte[1024 * 1024];
                try
                {
                    connection.Receive(buffer);
                    UserStreamHelper.GetStream(buffer, out var ack, out var infoData);
                    Console.WriteLine($"Clinet:{connection.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, UserId:{infoData.UserId}");

                    var ackCode = UserAck.Success;
                    if (infoData.UserId != "999")
                    {
                        ackCode = UserAck.AuthFail;
                    }
                    connection.Send(UserStreamHelper.CreateAck(ackCode));
                    //成功了請傳送ACK 不然會CLINET會卡住
                }
                catch (Exception)
                {
                    ClientConnectDict.Remove(connection.RemoteEndPoint.ToString());
                    Console.WriteLine($"Client:{connection.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
                    connection.Close();
                    break;
                }

                //送出連線成功訊息與前一百則留言
                string sendMsg = $" ClientIP:{clientIP} , Port:{clientPort} connect success.";
                connection.Send(Encoding.UTF8.GetBytes(sendMsg));

                if (tempMsg.Count != 0)
                {
                    connection.Send(MessageStreamHelper.CreateStream(MessageAck.Success, tempMsg.ToArray()));
                }

                Thread t2 = new Thread(Received);
                t2.IsBackground = true;
                t2.Start(connection);
            }
        }

        private void Received(object socketclientpara)
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

        private void InitFakeData()
        {
            List<MessageInfoData> dataList = new List<MessageInfoData>();
            for(int i = 0; i < 10; i++)
            {
                dataList.Add(new MessageInfoData
                {
                    Message = $"testString:{i}"
                });
            }
            SaveMultiInfoDataToRedis(GetRedisDb(), GetRedisDataKey(), dataList);
        }

        #region DB and Redis
        private bool GetTempData()
        {
            //從DB或REDIS撈資料
            //先從Redis Api 尋找資料 有的話就回傳整串 沒有就從DB撈
            //0表示連線  1表示資料存放處
            var entries = GetRedisDb().HashGetAll(GetRedisDataKey());
            foreach(HashEntry entry in entries)
            {
                GetOneInfoDataFromRedis(entry);
            }
            return false;
        }
        private IDatabase GetRedisDb()
        {
            return RedisHelper.Connection.GetDatabase((int)Redis.RedisHelper.RedisLinkNumber.MsgData);
        }
        private string GetRedisDataKey()
        {
            return "MessageList";
        }
        private void GetOneInfoDataFromRedis(HashEntry entry)
        {
            var infoData = JsonConvert.DeserializeObject<MessageInfoData>(entry.Value);

            tempMsg.Add(infoData);
        }

        private void SaveOneInfoDataToRedis(MessageInfoData infoData)
        {
            //找到當前行數 並新增字串
            GetRedisDb().HashSet(GetRedisDataKey(), tempMsg.Count-1, JsonConvert.SerializeObject(infoData));
        }

        private void SaveMultiInfoDataToRedis(IDatabase redisDb, string key, List<MessageInfoData> infoDatas)
        {
            //儲存所有的資訊到Redis
            var hashes = new List<HashEntry>();

            for(int i=0; i<infoDatas.Count; i++)
            {
                hashes.Add(new HashEntry(i, JsonConvert.SerializeObject(infoDatas[i])));
            }

            redisDb.HashSet(key, hashes.ToArray());
        }
        #endregion

        /// <summary>
        /// 檢查Port用
        /// </summary>
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

    }


}
