using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Common
{
    public static class MessageRespPayload
    {
        public static byte[] CreatePayload(MessageAck ackCode, MessageInfoData[] infoDataArray)
        {
            byte[] byteArray;
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream);
			binaryWriter.Write(((int)(ackCode)));
			if ((infoDataArray != null))
			{
				binaryWriter.Write(true);
				binaryWriter.Write(infoDataArray.Length);
				for (int i0 = 0; (i0 < infoDataArray.Length); i0 = (i0 + 1))
				{
					if ((infoDataArray[i0] != null))
					{
						binaryWriter.Write(true);
						MessageRespPayload.BinaryWriter(binaryWriter, infoDataArray[i0]);
					}
					else
					{
						binaryWriter.Write(false);
					}
				}
			}
			else
			{
				binaryWriter.Write(false);
			}
			byteArray = memoryStream.ToArray();
			binaryWriter.Close();
			memoryStream.Close();
			return byteArray;
		}

		public static void ParsePayload(this System.Byte[] payload, out MessageAck ackCode, out MessageInfoData[] infoDataArray)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(payload);
			System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);
			ackCode = ((MessageAck)(binaryReader.ReadInt32()));
			if (binaryReader.ReadBoolean() == true)
			{
				infoDataArray = new MessageInfoData[binaryReader.ReadInt32()];
				for (int i0 = 0; (i0 < infoDataArray.Length); i0 = (i0 + 1))
				{
					if ((binaryReader.ReadBoolean() == true))
					{
						MessageRespPayload.BinaryReader(binaryReader, out infoDataArray[i0]);
					}
					else
					{
						infoDataArray[i0] = default(MessageInfoData);
					}
				}
			}
			else
			{
				infoDataArray = default(MessageInfoData[]);
			}
			binaryReader.Close();
			memoryStream.Close();
		}

		private static void BinaryWriter(System.IO.BinaryWriter binaryWriter, MessageInfoData obj)
		{
			if ((obj != null))
			{
				binaryWriter.Write(true);
				binaryWriter.Write(obj.Message);
			}
			else
			{
				binaryWriter.Write(false);
			}
		}
		private static void BinaryReader(System.IO.BinaryReader binaryReader, out MessageInfoData obj)
		{
			if ((binaryReader.ReadBoolean() == true))
			{
				obj = new MessageInfoData();
				obj.Message = binaryReader.ReadString();
			}
			else
			{
				obj = default(MessageInfoData);
			}
		}
	}
}
