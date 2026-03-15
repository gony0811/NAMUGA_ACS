using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message.Model.Server;
using ACS.Core.Resource.Model.Command;
using ACS.Core.Resource.Model.Factory.Machine;
using ACS.Core.Resource.Model.Factory.Unit;
using ACS.Core.Route.Model.BidirectionalNode;
using ACS.Core.Transfer.Model;

namespace ACS.Core.Route
{
    public interface IBidirectionalManager
    {
        void CreateBidirectionalNode(BidirectionalNode paramBidirectionalNode);

        BidirectionalNode GetBidirectionalNode(String paramString);

        BidirectionalNode GetBidirectionalNode(TransportMachine paramTransportMachine1, Unit paramUnit, TransportMachine paramTransportMachine2);

        BidirectionalNode GetBidirectionalNode(TransportMachine paramTransportMachine, Unit paramUnit, String paramString);

        BidirectionalNode GetBidirectionalNode(String paramString1, String paramString2, String paramString3, String paramString4);

        BidirectionalNode GetBidirectionalNode(String paramString1, String paramString2);

        BidirectionalNode GetBidirectionalNode(TransportMachine paramTransportMachine, Unit paramUnit);

        BidirectionalNode GetBidirectionalNode(String paramString1, String paramString2, String paramString3);

        BidirectionalNode GetBidirectionalNodeWithNoMatterUsed(String paramString1, String paramString2);

        BidirectionalNode GetBidirectionalNodeByUniName(String paramString);

        void UpdateBidirectionalNode(BidirectionalNode paramBidirectionalNode);

        int UpdateBidirectionalNodeDirection(BidirectionalNode paramBidirectionalNode);

        int UpdateBidirectionalNodeDirection(BidirectionalNode paramBidirectionalNode, String paramString, DateTime paramDate);

        int UpdateBidirectionalNodeScheduling(BidirectionalNode paramBidirectionalNode, String paramString);

        int UpdateBidirectionalNodeDirection(BidirectionalNode paramBidirectionalNode, String paramString);

        int UpdateBidirectionalNodeDirectionChangedTime(BidirectionalNode paramBidirectionalNode);

        int UpdateBidirectionalNodeDirectionChangedTime(BidirectionalNode paramBidirectionalNode, String paramString);

        ArrayList GetBidirectionalNodes();

        ArrayList GetBidirectionalNodes(String paramString);

        void DeleteBidirectionalNode(BidirectionalNode paramBidirectionalNode);

        int DeleteBidirectionalNodes();

        bool IsDirectionAcceptable(TransportCommand paramTransportCommand, BidirectionalNode paramBidirectionalNode);

        bool IsDirectionAcceptable(TransportMachine paramTransportMachine, BidirectionalNode paramBidirectionalNode);

        bool IsDirectionAcceptable(String paramString, BidirectionalNode paramBidirectionalNode);

        bool IsScheduling(BidirectionalNode paramBidirectionalNode);



        string ExceedMaxCapacityWithResult(TransportMachine paramTransportMachine, Unit paramUnit, BidirectionalNode paramBidirectionalNode);

        string ExceedMaxCapacityWithResult(String paramString1, String paramString2, BidirectionalNode paramBidirectionalNode);


        string PossibleToChangeDirectionWithResult(BidirectionalNode paramBidirectionalNode);

        string PossibleToChangeDirectionWithResult(BidirectionalNode paramBidirectionalNode, bool paramBoolean);

        string PossibleToChangeDirectionByForceWithResult(BidirectionalNode paramBidirectionalNode);

        bool PossibleToStopScheduling(BidirectionalNode paramBidirectionalNode, DateTime paramCalendar, long paramLong);

        bool ChangeDirectionByForce(BidirectionalNode paramBidirectionalNode);

        bool ChangeDirectionByPortTypeChanged(Port paramPort);

        PortInOutTypeChangeCommand createPortInOutTypeChangeCommand(BidirectionalNode paramBidirectionalNode);

        bool IsBidirectionalModeUsed(String paramString);

        bool IsBidirectionalModeUsed(Port paramPort);

        bool CreateSequentialCommandForChangeDirection(TransferMessage paramTransferMessage, TransportMachine paramTransportMachine, Port paramPort);
    }
}
