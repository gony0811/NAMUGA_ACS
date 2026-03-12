using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ACS.Framework.Base;

namespace ACS.Extension.Framework.Resource.Model
{
    //NA_R_BAY_GROUP_CHARGE
    public class BayGroupCharegeEx : Entity
    {
        public virtual string Id { get; set; }
        //202008 SJP NA_R_BAY_GROUP_CHARGE table 수정
        public virtual string Bays { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("BayGroupCharegeEx [id=");
            builder.Append(Id);
            builder.Append(", bays=");
            builder.Append(Bays);
            builder.Append("]");
            return builder.ToString();
        }
    }
}
