using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Material.Model
{
    public class CarrierEx
    {
        public static String UNKNOWN_TYPE_UNK = "UNK";
        public static String CARRIER_TYPE_TRAY = "TRAY";

        public virtual string Id { get; set; }
        public virtual string Type { get; set; }
        public virtual string CarrierLoc { get; set; }
        public virtual DateTime? CreateTime { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carrier{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", carrierLoc=").Append(this.CarrierLoc);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
