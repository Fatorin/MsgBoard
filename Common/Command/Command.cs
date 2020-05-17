using Common.Command;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Command
{
    
    public enum Command : byte
    {
        LoginAuth,
        GetMsgAll,
        GetMsgOnce,
        LoginKick,
    }
}
