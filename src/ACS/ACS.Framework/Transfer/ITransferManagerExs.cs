using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Transfer;
using ACS.Framework.Transfer.Model;


namespace ACS.Framework.Transfer
{
    public interface ITransferManagerExs : ITransferManagerEx 
    {
        TransportCommandEx GetLastTransportCMDbyDest(String portID);

        //200622 Change NIO Logic About ES.exe does not restart
        IList GetEventUiCommand();
        //

        //200622 Change NIO Logic About ES.exe does not restart
        int DeleteUiCommandById(string Id, string messageName, string applicationName);
        //
    }
}
