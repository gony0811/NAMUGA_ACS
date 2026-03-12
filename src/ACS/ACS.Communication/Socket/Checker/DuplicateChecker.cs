using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Checker
{
    public interface DuplicateChecker
    {
        bool Duplicate(ReceivePacket paramReceivePacket, string paramString);

        //200423 Ignore garbage Value in ES, Before Biz
        bool Validate(ReceivePacket rcvPacket);
        //
    }

}
