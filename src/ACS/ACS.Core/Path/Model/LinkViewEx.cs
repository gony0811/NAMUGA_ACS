using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Path.Model
{
    public class LinkViewEx
    {      
        public virtual string Id { get; set; }
        public virtual string ZoneId { get; set; }
        public virtual string BayId { get; set; }
        public virtual string FromNodeId { get; set; }
        public virtual int From_xpos { get; set; }
        public virtual int From_ypos { get; set; }
        public virtual string ToNodeId { get; set; }
        public virtual int To_xpos { get; set; }
        public virtual int To_ypos { get; set; }
        public virtual int Length { get; set; }
        public virtual int LeftBranch { get; set; }
        public virtual string Availability { get; set; }
        public virtual int Load { get; set; }
        public virtual int Loading { get; set; }
        public virtual int Speed { get; set; }
        public virtual string TransferFlag { get; set; }
       
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("linkView{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", transferFlag=").Append(this.TransferFlag);
            sb.Append(", zoneId=").Append(this.ZoneId);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", fromNodeId=").Append(this.FromNodeId);
            sb.Append(", from_xpos=").Append(this.From_xpos);
            sb.Append(", from_ypos=").Append(this.From_ypos);
            sb.Append(", toNodeId=").Append(this.ToNodeId);
            sb.Append(", to_xpos=").Append(this.To_xpos);
            sb.Append(", to_ypos=").Append(this.To_ypos);
            sb.Append(", length=").Append(this.Length);
            sb.Append(", speed=").Append(this.Speed);
            sb.Append(", leftBranch=").Append(this.LeftBranch);
            sb.Append(", availability=").Append(this.Availability);
            sb.Append(", load=").Append(this.Load);
            sb.Append(", loading=").Append(this.Loading);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
