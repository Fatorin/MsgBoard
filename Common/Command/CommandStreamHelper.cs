using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Command
{
    public static class CommandStreamHelper
    {
        public static readonly int CommandSize = 1;
        public static void GetCommand(this System.Byte[] payload, out Command command)
        {
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(payload);
            System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);
            command = ((Command)(binaryReader.ReadByte()));
            binaryReader.Close();
            memoryStream.Close();
        }

        public static byte[] CreateCommandAndData(Command command, byte[] DataArray)
        {
            byte[] byteArray;
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream);
            binaryWriter.Write((byte)command);
            binaryWriter.Write(DataArray);
            byteArray = memoryStream.ToArray();
            binaryWriter.Close();
            memoryStream.Close();
            return byteArray;
        }
    }
}
