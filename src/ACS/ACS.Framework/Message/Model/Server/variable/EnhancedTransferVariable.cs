using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server.variable
{
    public class EnhancedTransferVariable
    {
        private string commandState = "NA";
        private string commandId;
        private string priority = "0";
        private string replace;
        private string carrierName;
        private string source;
        private string dest;

        public string CommandState { get { return commandState; } set { commandState = value; } }
        public string CommandId { get { return commandId; } set { commandId = value; } }
        public string Priority { get { return priority; } set { priority = value; } }
        public string Replace { get { return replace; } set { replace = value; } }
        public string CarrierName { get { return carrierName; } set { carrierName = value; } }
        public string Source { get { return source; } set { source = value; } }
        public string Dest { get { return dest; } set { dest = value; } }

        public string changeCommandState(string original)
        {
            string commandState = "NA";
            if (original.Equals("1"))
            {
                commandState = "QUEUED";
            }
            else if (original.Equals("2"))
            {
                commandState = "TRANSFERRING";
            }
            else if (original.Equals("3"))
            {
                commandState = "PAUSED";
            }
            else if (original.Equals("4"))
            {
                commandState = "CANCELING";
            }
            else if (original.Equals("5"))
            {
                commandState = "ABORTING";
            }
            else if (original.Equals("6"))
            {
                commandState = "WAITING";
            }
            return commandState;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedTransferVariable{");
            sb.Append("commandId=").Append(this.commandId);
            sb.Append(", carrierName=").Append(this.carrierName);
            sb.Append(", commandState=").Append(this.commandState);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", source=").Append(this.source);
            sb.Append(", dest=").Append(this.dest);
            sb.Append(", replace=").Append(this.replace);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
