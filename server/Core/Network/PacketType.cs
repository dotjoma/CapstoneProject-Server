using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Core.Network
{
    public enum PacketType
    {
        None = 0,
        Register = 1,
        RegisterResponse = 2,
        Login = 3,
        LoginResponse = 4,
        Logout = 5
    }
}
