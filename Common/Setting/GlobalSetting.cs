using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Setting
{
    public static class GlobalSetting
    {
        public static string LocalIP = "127.0.0.1";
        public static int PortNum1 = 9987;
        public static int PortNum2 = 9987;
        private static readonly string RedisGetConnectStr = $"{LocalIP}:16800,password=jfiredis";
        
        public static string GetRedisGetConnectStr()
        {
            return RedisGetConnectStr;
        }
    }
}
