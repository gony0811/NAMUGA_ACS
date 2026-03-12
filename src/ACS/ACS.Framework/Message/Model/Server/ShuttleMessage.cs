using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server
{
    public class ShuttleMessage : BaseMessage
    {
        private string state;
        private string location;

        public string State { get { return state; } set { state = value; } }

        public string Location { get { return location; } set { location = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("shuttleMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", state=").Append(this.state);
            sb.Append(", location=").Append(this.location);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
