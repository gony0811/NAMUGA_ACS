using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Transfer.Model;
using System.Collections;

namespace ACS.Framework.Transfer
{
    public interface IRequestManagerEx
    {
        void CreateTransportCommandRequest(TransportCommandRequestEx paramTransportCommandRequest);

        TransportCommandRequestEx CreateTransportCommandRequest(String paramString1, String paramString2, String paramString3, String paramString4);

        TransportCommandRequestEx CreateTransportCommandRequest(String paramString1, String paramString2, String paramString3);

        TransportCommandRequestEx CreateTransportCommandRequest(String paramString1, String paramString2);

        TransportCommandRequestEx GetTransportCommandRequest(String paramString1, String paramString2, String paramString3);

        TransportCommandRequestEx GetTransportCommandRequest(String paramString1, String paramString2);

        TransportCommandRequestEx GetTransportCommandRequest(String paramString);

        IList GetTransportCommandRequests();

        void DeleteTransportCommandRequest(TransportCommandRequestEx paramTransportCommandRequestACS);

        int DeleteTransportCommandRequest(String paramString1, String paramString2, String paramString3);

        int DeleteTransportCommandRequest(String paramString1, String paramString2);

        int DeleteTransportCommandRequest(String paramString);

        int DeleteTransportCommandRequests();
    }
}
