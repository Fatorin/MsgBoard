using Common;
using Common.Setting;
using Server.Redis;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class StartServer
    {
        static void Main(string[] args)
        {
            MainManager.MainManager.Instance.Start();
            
        }
    }
}
