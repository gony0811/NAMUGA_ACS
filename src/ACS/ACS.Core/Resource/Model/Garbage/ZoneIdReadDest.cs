using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Garbage
{
    public class ZoneIdReadDest : TimedEntity
    {
        private string machineName = "";
        private string zoneName = "";
        private string destMachineName = "";
        private string destZoneName = "";
        private int priority;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; }
        }

        public string DestMachineName
        {
            get { return destMachineName; }
            set { destMachineName = value; }
        }

        public string DestZoneName
        {
            get { return destZoneName; }
            set { destZoneName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zoneIdReadDest{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", destMachineName=").Append(this.destMachineName);
            sb.Append(", destZoneName=").Append(this.destZoneName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
