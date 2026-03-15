using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Path.Model
{
    public class StationEx
    {

        public virtual string Id { get; set; }
        public virtual string LinkId { get; set; }
        public virtual string Type { get; set; }
        public virtual int Distance { get; set; }
        public virtual string Direction { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("station{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", linkId=").Append(this.LinkId);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", distance=").Append(this.Distance);
            sb.Append(", direction=").Append(this.Direction);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
