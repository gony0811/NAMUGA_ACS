using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message
{
    public interface IPacket
    {
        string Data { get; set; }
        string SendId { get; set; }
        string RevId { get; set; }
        string Command { get; set; }
        string Station { get; set; }
        string Number { get; set; }

        string Sub { get; set; }
        string CheckSum { get; set; }

        byte[] RawData { get; set; }

        byte STX { get; set; }

        byte ETX { get; set; }

        DateTime CreateTime { get; set; }
    }
}
