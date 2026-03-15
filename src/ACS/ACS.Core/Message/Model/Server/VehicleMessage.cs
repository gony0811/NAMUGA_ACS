using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource.Model.Factory.Unit;
using ACS.Core.Resource.Model.Factory.Position;

namespace ACS.Core.Message.Model.Server
{
    public class VehicleMessage: BaseMessage
    {
        private string transferPortName;
        private Unit transferPort;
        private string processingState;

        private string vehicleStageName;

        private VehicleStage vehicleStage;

        public string TransferPortName { get { return transferPortName; } set { transferPortName = value; } }
        public Unit TransferPort { get { return transferPort; } set { transferPort = value; } }
        public string ProcessingState { get { return processingState; } set { processingState = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicleMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", transferPortName=").Append(this.transferPortName);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
