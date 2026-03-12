using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Factory.Unit
{
    public class Shuttle : Unit
    {
        private string location;

        public string Location
        {
            get { return location; }
            set { location = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("shuttle{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(base.Name);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", subState=").Append(this.subState);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", lastTransferTime=").Append(this.lastTransferTime);

            sb.Append(", location=").Append(this.location);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
