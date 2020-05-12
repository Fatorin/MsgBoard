using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Setting
{
    public static class GlobalSetting
    {
        public static string LocalIP = "127.0.0.1";
        public static int PortNum = 9987;
        private static string RedisGetConnectStr = $"{LocalIP}:16800,password=jfiredis";

        public static string GetRedisGetConnectStr()
        {
            return RedisGetConnectStr;
        }
    }
}
