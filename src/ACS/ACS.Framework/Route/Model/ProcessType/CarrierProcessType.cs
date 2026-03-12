using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model.ProcessType
{
    public class CarrierProcessType : Entity
    {
        private string carrierName = "";
        private string processTypeName = "";

        public string CarrierName { get { return carrierName; } set { carrierName = value; } }
        public string ProcessTypeName { get { return processTypeName; } set { processTypeName = value; } }

        public CarrierProcessType()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carrierProcessType{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", carrierName=").Append(this.carrierName);
            sb.Append(", processTypeName=").Append(this.processTypeName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
