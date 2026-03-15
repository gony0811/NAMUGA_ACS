using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Command
{
    public class IdReadCommand : ResourceCommand
    {
        private string unitName;
        private string carrierName;

        public string UnitName
        {
            get { return unitName; }
            set { unitName = value; }
        }

        public string CarrierName
        {
            get { return carrierName; }
            set { carrierName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("idReadCommand{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", carrierName=").Append(this.carrierName);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
