using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Emulator
{
    public class EmulSendPacket : SendPacket
    {
        public EmulSendPacket(string RevID, string SendID, string Command, string Data, string emul = "EMULATOR")
           :base(RevID, SendID, Command, Data, emul)
        {

        }
    }
}
