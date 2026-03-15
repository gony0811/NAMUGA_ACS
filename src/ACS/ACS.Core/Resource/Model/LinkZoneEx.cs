using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model
{
    public class LinkZoneEx : Entity
    {

        public virtual string LinkId { get; set; }
        public virtual string ZoneId { get; set; }
        public virtual string TransferFlag { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("link_zone{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", linkId=").Append(this.LinkId);
            sb.Append(", zoneId=").Append(this.ZoneId);
            sb.Append(", transferFlag=").Append(this.TransferFlag);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
