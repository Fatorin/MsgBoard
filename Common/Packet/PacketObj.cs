using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Common.Packet
{
    public class PacketObj
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 32;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        public int PacketNeedReceiveLen;
        public byte[] infoBytes;
        public bool isCorrectPack = false;
        public int LastReceivedPos;
        public int Command;

        public void ResetData()
        {
            PacketNeedReceiveLen = 0;
            infoBytes = null;
            isCorrectPack = false;
            LastReceivedPos = 0;
            Command = 0;
        }

        public void SetFirstReceive(int dataLen,int command)
        {
            //設定封包驗證通過(如果有要接收第二段就會繼續接收)
            isCorrectPack = true;
            //設定封包的長度(第一次的時候)，要減少前面的crc dataLen command
            PacketNeedReceiveLen = dataLen;
            //設定接收封包大小
            infoBytes = new byte[dataLen];
            //設定封包的指令(第一次的時候)
            Command = command;
        }

        public void SetContiuneReceive(int bytesRead)
        {
            //減去已收到的封包數
            //接收完後更新對應的LastReceivedPos
            PacketNeedReceiveLen -= bytesRead;
            LastReceivedPos += bytesRead;
        }

    }
}
