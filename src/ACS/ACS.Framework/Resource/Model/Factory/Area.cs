using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Factory
{
    public class Area : Module
    {
        public static string TYPE_SHOP = "SHOP";
        public static string TYPE_AREA = "AREA";
        public static string TYPE_BAY = "BAY";

        public Area()
        {
            this.type = "AREA";
        }

        private string factoryName = "";
        private string parentName = "";
        

        public string FactoryName
        {
            get { return factoryName; }
            set { factoryName = value; }
        }

        public string ParentName
        {
            get { return parentName; }
            set { parentName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("area{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(Name);
            sb.Append(", type=").Append(this.type);
            sb.Append(", factoryName=").Append(this.factoryName);
            sb.Append(", parentName=").Append(this.parentName);
            sb.Append(", state=").Append(this.state);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
