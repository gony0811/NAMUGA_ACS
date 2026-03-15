using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Transfer.Model;
using ACS.Core.Message.Model;
using System.Collections;

namespace ACS.Core.Transfer
{
    public interface ITransferManagerEx
    {
        void CreateTransportCommand(TransportCommandEx paramTransportCommand);

        TransportCommandEx CreateTransportCommand(String paramString1, String paramString2, String paramString3, String paramString4, int paramInt);

        TransportCommandEx CreateTransportCommand(String paramString1, String paramString2, String paramString3, String paramString4, int paramInt, String paramString5, String paramString6, String paramString7, String paramString8, String paramString9, String paramString10, String paramString11, String paramString12);

        TransportCommandEx CreateTransportCommand(TransferMessageEx paramTransferMessage);

        TransportCommandEx CreateRechargeTransportCommand(TransportCommandEx paramTransportCommand);

        TransportCommandEx CreateStockStationTransportCommand(TransportCommandEx paramTransportCommand);

        TransportCommandEx GetTransportCommand(String paramString);

        TransportCommandEx GetTransportCommandByCarrierId(String paramString);

        TransportCommandEx GetTransportCommandByVehicleId(String paramString);

        TransportCommandEx GetTransportCommandByDestPortId(String paramString);

        bool CheckTransportCommandBySourceLocationId(String paramString);

        bool CheckTransportCommandByDestLocationId(String paramString);

        IList GetTransportCommands();

        IList GetQueuedTransportCommands();

        IList GetQueuedUiTransportCommands();
        IList GetQueuedTransportCommandsByBayId(String paramString);

        IList GetTransportCommandsByStateAndBayId(String paramString1, String paramString2);

        int GetTransportCommandCount();

        int GetTransportCommandCountBySourcePortId(String paramString);

        int GetTransportCommandCountByDestPortId(String paramString);

        TransportCommandEx GetTransportCommandByQueueStateFIFO(String paramString);

        void UpdateTransportCommand(TransportCommandEx paramTransportCommand);

        int UpdateTransportCommand(TransportCommandEx paramTransportCommand, Dictionary<string, object> paramMap);

        int UpdateTransportCommandVehicleId(TransportCommandEx paramTransportCommand, String paramString);

        int UpdateTransportCommandPath(TransportCommandEx paramTransportCommand, String paramString);

        int UpdateTransportCommandStateByChangeVehicle(TransportCommandEx paramTransportCommand);

        int DeleteTransportCommand(String paramString);

        int DeleteTransportCommand(TransportCommandEx paramTransportCommand);

        int DeleteTransportCommandsByCarrierId(String paramString);

        int DeleteTransportCommands();

        int DeleteUiTransportById(String paramString);

        bool ExistTransportCommand(String paramString);

        String ConvertPriorityToMES(String paramString);

        void UpdateTransportCommandAdditionalInfo(TransportCommandEx paramTransportCommand);

        String GetAdditionalInfo(TransportCommandEx paramTransportCommand, String paramString);
    }
}
