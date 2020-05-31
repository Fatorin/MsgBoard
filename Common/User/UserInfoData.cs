using System;
using System.Collections.Generic;
using System.Text;

namespace Common.User
{
    [Serializable]
    public class UserInfoData
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserPwd { get; set; }
    }
}
