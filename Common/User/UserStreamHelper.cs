using System;
using System.Collections.Generic;
using System.Text;

namespace Common.User
{
	public static class UserStreamHelper
    {
		public static byte[] CreateAck(UserAck ackCode)
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

		public static void GetAck(this System.Byte[] payload, out UserAck ackCode)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(payload);
			System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);
			ackCode = ((UserAck)(binaryReader.ReadInt32()));
			binaryReader.Close();
			memoryStream.Close();
		}

		public static byte[] CreateStream(UserAck ackCode, UserInfoData infoData)
		{
			byte[] byteArray;
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream);
			binaryWriter.Write(((int)(ackCode)));
			if ((infoData != null))
			{
				binaryWriter.Write(true);
				UserStreamHelper.BinaryWriter(binaryWriter, infoData);
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

		public static void GetStream(this System.Byte[] payload, out UserAck ackCode, out UserInfoData infoData)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(payload);
			System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);
			ackCode = ((UserAck)(binaryReader.ReadInt32()));
			if ((binaryReader.ReadBoolean() == true))
			{
				UserStreamHelper.BinaryReader(binaryReader, out infoData);
			}
			else
			{
				infoData = default(UserInfoData);
			}
			binaryReader.Close();
			memoryStream.Close();
		}

		private static void BinaryWriter(System.IO.BinaryWriter binaryWriter, UserInfoData obj)
		{
			if ((obj != null))
			{
				binaryWriter.Write(true);
				binaryWriter.Write(obj.UserId);
				binaryWriter.Write(obj.UserPwd);
			}
			else
			{
				binaryWriter.Write(false);
			}
		}
		private static void BinaryReader(System.IO.BinaryReader binaryReader, out UserInfoData obj)
		{
			if ((binaryReader.ReadBoolean() == true))
			{
				obj = new UserInfoData();
				obj.UserId = binaryReader.ReadString();
				obj.UserPwd = binaryReader.ReadString();
			}
			else
			{
				obj = default(UserInfoData);
			}
		}
	}
}
