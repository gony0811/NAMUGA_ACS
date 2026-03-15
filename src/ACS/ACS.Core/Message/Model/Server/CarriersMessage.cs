using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Material.Model;

namespace ACS.Core.Message.Model.Server
{
    public class CarriersMessage : StatusVariableMessage
    {

        private System.Collections.IList carriers = new ArrayList();
        private string currentZoneName = "";

        protected internal IList Carriers { get { return carriers; } set { carriers = value; } }
        protected internal string CurrentZoneName { get { return currentZoneName; } set { currentZoneName = value; } }

        public int Add(Carrier carrier)
        {
            return this.Carriers.Add(carrier);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carriersMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentZoneName=").Append(this.currentZoneName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", carriers=").Append(this.carriers);
            sb.Append("}");
            return sb.ToString();
        }


    }
}
