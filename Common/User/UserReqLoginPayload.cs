using System;
using System.Collections.Generic;
using System.Text;

namespace Common.User
{
    public static class UserReqLoginPayload
    {
        public static byte[] CreatePayload(UserInfoData infoData)
        {
			byte[] byteArray;
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream);
			if ((infoData != null))
			{
				binaryWriter.Write(true);
				UserReqLoginPayload.BinaryWriter(binaryWriter, infoData);
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

		public static void ParsePayload(this System.Byte[] payload, out UserInfoData infoData)
		{
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(payload);
			System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);
			if ((binaryReader.ReadBoolean() == true))
			{
				UserReqLoginPayload.BinaryReader(binaryReader, out infoData);
			}
			else
			{
				infoData = default(UserInfoData);
			}
			binaryReader.Close();
			memoryStream.Close();
		}

		public static void BinaryWriter(System.IO.BinaryWriter binaryWriter, UserInfoData obj)
		{
			if ((obj != null))
			{
				binaryWriter.Write(true);
				binaryWriter.Write(obj.Id);
				binaryWriter.Write(obj.UserId);
				binaryWriter.Write(obj.UserPwd);
			}
			else
			{
				binaryWriter.Write(false);
			}
		}
		public static void BinaryReader(System.IO.BinaryReader binaryReader, out UserInfoData obj)
		{
			if ((binaryReader.ReadBoolean() == true))
			{
				obj = new UserInfoData();
				obj.Id = binaryReader.ReadInt32();
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
