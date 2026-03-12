using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Factory
{
    public class Factory : NamedEntity
    {
        public static string TYPE_FACTORY = "FACTORY";
        public static string FACTORY_NOTDESIGNATED = "FACTORY_ND";
        protected string territory = "F";

        public string Territory
        {
            get { return territory; }
            set { territory = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("factory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(Name);
            sb.Append(", territory=").Append(this.territory);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
