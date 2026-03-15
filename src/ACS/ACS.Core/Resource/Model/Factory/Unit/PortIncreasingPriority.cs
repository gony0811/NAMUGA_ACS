using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Factory.Unit
{
    public class PortIncreasingPriority : Entity
    {

        private string machineName;
        private string portName;
        private string fromIncreasingPriority = "50";
        private string toIncreasingPriority = "50";
        private string used = "T";

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

        public string FromIncreasingPriority
        {
            get { return fromIncreasingPriority; }
            set { fromIncreasingPriority = value; }
        }

        public string ToIncreasingPriority
        {
            get { return toIncreasingPriority; }
            set { toIncreasingPriority = value; }
        }

        public string Used
        {
            get { return used; }
            set { used = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("portIncreasingPriority{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", fromIncreasingPriority=").Append(this.fromIncreasingPriority);
            sb.Append(", toIncreasingPriority=").Append(this.toIncreasingPriority);
            sb.Append(", used=").Append(this.used);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
