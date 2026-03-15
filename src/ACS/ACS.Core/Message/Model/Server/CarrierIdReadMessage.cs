using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class CarrierIdReadMessage: BaseMessage
    {
        private string idReadState;
        private string carrierType;

        public string IdReadState { get { return idReadState; } set { idReadState = value; } }
        public string CarrierType { get { return carrierType; } set { carrierType = value; } }  

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carrierIdReadMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", idReadState=").Append(this.IdReadState);
            sb.Append(", carrierType=").Append(this.CarrierType);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
