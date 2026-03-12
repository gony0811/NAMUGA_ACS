using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Path.Model
{
    public class LocationViewEx
    {
        public virtual string PortId {get;set;}

        public virtual string StationId { get; set; }

        public virtual string BayId { get; set; }

        public virtual string Location_Type { get; set; }

        public virtual string CarrierType { get; set; }

        public virtual string Direction { get; set; }

        public virtual string State { get; set; }

        public virtual string LinkId { get; set; }

        public virtual string ParentNode { get; set; }

        public virtual string NextNode { get; set; }

        public virtual string Station_type { get; set; }

        public virtual int Distance { get; set; }

        public virtual int From_xpos { get; set; }

        public virtual int From_ypos { get; set; }

        public virtual int To_xpos { get; set; }

        public virtual int To_ypos { get; set; }

        public virtual int Length { get; set; }

        //public virtual int Speed { get; set; }

        public virtual int LeftBranch { get; set; }

        public virtual string Availability { get; set; }

        public virtual int Load { get; set; }

        public virtual string TransferFlag { get; set; }
       
        public override string ToString() 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("location_view{");
            sb.Append("id=").Append(this.PortId);
            sb.Append(", stationid=").Append(this.StationId);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", location_type=").Append(this.Location_Type);
            sb.Append(", carriertype=").Append(this.CarrierType);
            sb.Append(", direction=").Append(this.Direction);
            sb.Append(", state=").Append(this.State);
            sb.Append(", linkid=").Append(this.LinkId);
            sb.Append(", parentnode=").Append(this.ParentNode);
            sb.Append(", station_type=").Append(this.Station_type);
            sb.Append(", distance=").Append(this.Distance);
            sb.Append(", from_xpos=").Append(this.From_xpos);
            sb.Append(", from_ypos=").Append(this.From_ypos);
            sb.Append(", to_xpos=").Append(this.To_xpos);
            sb.Append(", to_ypos=").Append(this.To_ypos);
            sb.Append(", length=").Append(this.Length);
            sb.Append(", leftbranch=").Append(this.LeftBranch);
            sb.Append(", availability=").Append(this.Availability);
            sb.Append(", load=").Append(this.Load);
            sb.Append(", transferFlag=").Append(this.TransferFlag);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
