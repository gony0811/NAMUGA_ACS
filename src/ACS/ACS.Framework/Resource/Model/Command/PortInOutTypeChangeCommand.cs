using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Command
{
    public class PortInOutTypeChangeCommand : ResourceCommand
    {
        private string commandMachineName;
        private string portName;
        private string inOutType;

        public string CommandMachineName
        {
            get { return commandMachineName; }
            set { commandMachineName = value; }
        }

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }

        public string InOutType
        {
            get { return inOutType; }
            set { inOutType = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("portInOutTypeChangeCommand{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", commandMachineName=").Append(this.commandMachineName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", inOutType=").Append(this.inOutType);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
