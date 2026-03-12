using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model
{
    public class OrderPairNodeEx
    {
        public virtual string Id { get; set; }
        public virtual string OrderGroup { get; set; }
        public virtual string Status { get; set; }


        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("OrderPairNodeACS [id=");
            builder.Append(Id);
            builder.Append(", orderGroup=");
            builder.Append(OrderGroup);
            builder.Append(", status=");
            builder.Append(Status);
            builder.Append("]");
            return builder.ToString();
        }
    }
}
