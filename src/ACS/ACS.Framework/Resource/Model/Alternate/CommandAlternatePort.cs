using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Alternate
{
    public class CommandAlternatePort : TimedEntity
    {
        private string machineName = "";
        private string portName = "";
        private string alternateMachineName = "";
        private string alternatePortName = "";
        protected int alternatePriority;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }

        public string AlternateMachineName
        {
            get { return alternateMachineName; }
            set { alternateMachineName = value; }
        }

        public string AlternatePortName
        {
            get { return alternatePortName; }
            set { alternatePortName = value; }
        }

        public int AlternatePriority
        {
            get { return alternatePriority; }
            set { alternatePriority = value; }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("commandAlternateStorage{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", alternateMachineName=").Append(this.alternateMachineName);
            sb.Append(", alternatePortName=").Append(this.alternatePortName);
            sb.Append(", alternatePriority=").Append(this.alternatePriority);
            sb.Append("}");
            return sb.ToString();
        }
    }


}
