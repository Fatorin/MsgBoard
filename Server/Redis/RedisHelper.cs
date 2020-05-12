using Common.Setting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Redis
{
    class RedisHelper
    {
        static RedisHelper()
        {
            var RedisGetConnectStr = GlobalSetting.GetRedisGetConnectStr();
            RedisHelper._connection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect(RedisGetConnectStr);
            });
        }

        private static Lazy<ConnectionMultiplexer> _connection;

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return _connection.Value;
            }
        }

        public enum RedisLinkNumber
        {
            Connect,
            MsgData,
        }
    }

    
}
