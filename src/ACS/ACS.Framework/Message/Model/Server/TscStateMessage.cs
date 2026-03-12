using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server
{
    public class TscStateMessage: BaseMessage
    {
        private string tscState;

        protected internal string TscState { get { return tscState; } set { tscState = value; } }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("tscStateMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", tscState=").Append(this.tscState);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
