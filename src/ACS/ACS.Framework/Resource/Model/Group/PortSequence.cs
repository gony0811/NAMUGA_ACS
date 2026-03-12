using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Group
{
    public class PortSequence : NamedEntity
    {
        private string machineName = "";
        private string portName = "";
        private int sequence = 1;

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string PortName { get { return portName; } set { portName = value; } }
        public int Sequence { get { return sequence; } set { sequence = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("portSequence{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", sequence=").Append(this.sequence);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
