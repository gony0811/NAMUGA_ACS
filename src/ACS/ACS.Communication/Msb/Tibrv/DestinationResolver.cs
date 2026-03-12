using ACS.Communication.Msb.Highway101;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb
{
    public interface DestinationResolver
    {
        //IDestination getDestination(string paramString);
        ChannelDestination getDestination(string paramString);
    }
}
