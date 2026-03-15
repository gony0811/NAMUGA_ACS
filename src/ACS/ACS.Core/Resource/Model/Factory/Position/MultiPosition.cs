using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Factory.Position
{
    public class MultiPosition : NamedEntity 
    {
        protected string carrierName = "";

        public string CarrierName
        {
            get { return carrierName; }
            set { carrierName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("multiPosition{");
            sb.Append("id=").Append(this.Id);
            sb.Append("name=").Append(Name);
            sb.Append("description=").Append(Description);
            sb.Append("createTime=").Append(CreateTime);
            sb.Append("editTime=").Append(EditTime);
            sb.Append("creator=").Append(Creator);
            sb.Append("editor=").Append(Editor);

            sb.Append("carrierName=").Append(this.carrierName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
