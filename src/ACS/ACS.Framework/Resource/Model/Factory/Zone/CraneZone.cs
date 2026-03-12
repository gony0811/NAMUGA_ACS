using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Factory.Zone
{
    public class CraneZone : TimedEntity
    {
        public string craneName = "";
        public string zoneName = "";
        public string craneAvailable = "T";

        public string CraneAvailable
        {
            get { return craneAvailable; }
            set { craneAvailable = value; }
        }

        public string CraneName
        {
            get { return craneName; }
            set { craneName = value; }
        }

        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("craneZone{");
            sb.Append("craneName=").Append(this.craneName);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append(", craneAvailable=").Append(this.craneAvailable);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
