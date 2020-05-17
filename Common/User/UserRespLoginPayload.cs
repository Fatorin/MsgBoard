using System;
using System.Collections.Generic;
using System.Text;

namespace Common.User
{
	public static class UserRespLoginPayload
    {
		public static byte[] CreatePayload(UserAck ackCode)
		{
			byte[] byteArray;
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream);
			binaryWriter.Write(((int)(ackCode)));
			byteArray = memoryStream.ToArray();
			binaryWriter.Close();
			memoryStream.Close();
			return byteArray;
		}

		public static void ParsePayload(this System.Byte[] payload, out UserAck ackCode)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(payload);
			System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);
			ackCode = ((UserAck)(binaryReader.ReadInt32()));
			binaryReader.Close();
			memoryStream.Close();
		}



		
	}
}
