using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model
{
    public class ZoneEx
    {

        public virtual string Id { get; set; }
        public virtual string BayId { get; set; }
        public virtual string Description { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("zone{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", description=").Append(this.Description);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
