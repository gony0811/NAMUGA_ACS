using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model.ProcessType
{
    public class ProcessType : Entity
    {
        public static string PROCESSTYPE = "PROCESSTYPE";
        public static string PROCESSTYPE_ALL = "ALL";
        private string name;
        private string carrierKind = "CARRIER";

        public ProcessType()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string Name { get { return name; } set { name = value; } }
        public string CarrierKind { get { return carrierKind; } set { carrierKind = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("processType{");
            sb.Append("name=").Append(this.name);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
