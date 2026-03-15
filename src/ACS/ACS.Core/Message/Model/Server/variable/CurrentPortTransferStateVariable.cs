using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class CurrentPortTransferStateVariable
    {
        private string name;
        private string portTransferState;

        protected internal string Name { get { return name; } set { name = value; } }
        protected internal string PortTransferState { get { return portTransferState; } set { portTransferState = value; } }  

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("currentPortTransferStateVariable{");
            sb.Append("name=").Append(this.name);
            sb.Append(", portTransferState=").Append(this.portTransferState);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
