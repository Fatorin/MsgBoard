using Common;
using Common.Setting;
using Common.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class StartClient
    {
        static void Main(string[] args)
        {
            CreateClient client = new CreateClient();
            client.Start();
        }
    }
}
