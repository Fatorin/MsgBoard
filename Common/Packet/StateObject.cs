using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Common.Packet
{
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        public int PacketNeedReceiveLen;
        public byte[] infoBytes;
        public bool isCorrectPack = false;
        public int LastReceivedPos;
    }
}
