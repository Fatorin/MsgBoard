using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;

namespace Common.Packet
{
    public class Packet
    {
        public static readonly int crcCode = 65517;
        public static readonly int VerificationLen = 12;
        public static byte[] BuildPacket(int command, byte[] dataByte)
        {
            //定義
            //CRC, command , data
            //依序存入crc len cmd data
            byte[] crcByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(crcCode));
            byte[] commandByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(command));
            byte[] dataLen = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(crcByte.Length + commandByte.Length + dataByte.Length));
            byte[] packByte = new byte[crcByte.Length + dataLen.Length + commandByte.Length + dataByte.Length];
            crcByte.CopyTo(packByte, 0);
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packByte.Length)).CopyTo(packByte, 4);
            commandByte.CopyTo(packByte, 8);
            dataByte.CopyTo(packByte, 12);
            return packByte;
        }

        public static void UnPackParam(byte[] dataByte, out int crc, out int dataLen, out int command)
        {
            crc = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(dataByte, 0));
            dataLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(dataByte, 4));
            command = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(dataByte, 8));
        }


        private void testfun()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var pack = Packet.BuildPacket(123, data);
            int len = pack.Length;
            int crc = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(pack, 0));
            int dataLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(pack, 4));
            int command = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(pack, 8));
            byte[] reciveData = new byte[dataLen];
            reciveData = pack.Skip(12).ToArray();
            reciveData.Reverse();
            Console.WriteLine($"packLen={len}");
            Console.WriteLine($"crc={crc}");
            Console.WriteLine($"command={command}");
            Console.WriteLine($"DataLen={reciveData.Length}");

            for (int i = 0; i < reciveData.Length; i++)
            {
                Console.WriteLine($"reciveData{i}={reciveData[i]}");
            }
        }

    }
}
