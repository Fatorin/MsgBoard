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
using Common.Command;
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
        static Dictionary<byte, Action<Socket, byte[]>> CommandRespDict = new Dictionary<byte, Action<Socket,byte[]>>();
        static List<MessageInfoData> tempMsg = new List<MessageInfoData>();

        public void Start()
        {
            //初始化對應的Command
            InitCommandMapping();
            //IPAddress ip = IPAddress.Parse(hostIP);
            IPAddress ip = IPAddress.Any;
            int port = GlobalSetting.PortNum1;
            if (PortInUse(GlobalSetting.PortNum1)){
                port = GlobalSetting.PortNum2;
            }

            if (PortInUse(GlobalSetting.PortNum1) && PortInUse(GlobalSetting.PortNum2))
            {
                Console.WriteLine("Port are used.");
                Console.ReadKey();
                return;
            }
            Console.WriteLine($"Port use {port}");
            //綁定到IPEndPoint上
            IPEndPoint ipe = new IPEndPoint(ip, port);

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

        private void InitCommandMapping()
        {
            CommandRespDict = new Dictionary<byte, Action<Socket, byte[]>>()
            {
                { (byte)Command.LoginAuth, ReceviveLoginAuthData},
                { (byte)Command.GetMsgOnce, ReceviveOneMessage},
            };
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
                    ClientConnectDict.Remove(socket.RemoteEndPoint.ToString());
                    Console.WriteLine($"Client:{socket.RemoteEndPoint} disconnect. Client online count:{ClientConnectDict.Count}");
                    socket.Close();
                    break;
                }
            }
        }

        private void ReceviveLoginAuthData(Socket socket, byte[] byteArray)
        {
            UserStreamHelper.GetStream(byteArray, out var ackCode, out var infoData);
            Console.WriteLine($"Clinet:{socket.RemoteEndPoint} Time：{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}, UserId:{infoData.UserId}");
            //這邊是帳密驗證的部分 邏輯還沒寫//db還沒撈
            ackCode = UserAck.Success;
            if (infoData.UserId != "999")
            {
                ackCode = UserAck.AuthFail;
            }
            //回傳成功訊息給對應的人
            SendCommand(socket, Command.LoginAuth, UserStreamHelper.CreateAck(ackCode));
            //回傳留言版資料
            if (tempMsg.Count != 0)
            {
                SendCommand(socket, Command.GetMsgAll, MessageStreamHelper.CreateStream(MessageAck.Success, tempMsg.ToArray()));
            }
        }

        private void ReceviveOneMessage(Socket socket, byte[] byteArray)
        {
            while (true)
            {
                try
                {
                    //這邊還沒改 也要弄成對應的command/func
                    //這邊有包含玩家資料 所以要小心
                    string receviedStr = Encoding.UTF8.GetString(byteArray);

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

        private void SendCommand(Socket socket, Command command, byte[] dataArray)
        {
            socket.Send(CommandStreamHelper.CreateCommandAndData(command, dataArray));
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

                Thread thread = new Thread(ReceiveCommand);
                thread.IsBackground = true;
                thread.Start(connection);
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
