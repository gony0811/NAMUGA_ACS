using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model
{
    public class VehicleGroupEx : ACS.Core.Base.Entity
    {
        public String Bays { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Bays);
            builder.Append(", id=");
            builder.Append(Id);
            builder.Append("]");
            builder.Append("VehicleGroupACS [bays=");
            return builder.ToString();
        }
    }
}
