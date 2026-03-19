using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model
{
    public class LocationEx
    {
        public static string TYPE_CHARGE = "CHARGE";
        public static string TYPE_STOCK = "STOCK";

        public virtual long Id { get; set; }
        public virtual string LocationId { get; set; }
        public virtual string StationId { get; set; }
        public virtual string Type { get; set; }
        public virtual string CarrierType { get; set; }
        public virtual string State { get; set; }
        public virtual string Direction { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("location{");
            sb.Append("locationId=").Append(this.LocationId);
            sb.Append(", stationId=").Append(this.StationId);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", carrierType=").Append(this.CarrierType);
            sb.Append(", state=").Append(this.State);
            sb.Append(", direction=").Append(this.Direction);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
