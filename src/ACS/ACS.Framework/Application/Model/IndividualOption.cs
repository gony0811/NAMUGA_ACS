using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Application.Model
{
    public class IndividualOption : Entity
    {
        private string optionName;
        private string machineName;
        private string unitName;
        private string value;
        private string valueDescription;
        private string subValue;
        private string subValueDescription;

        public string OptionName { get { return optionName; } set { optionName = value; } }
        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string Value { get { return value; } set { this.value = value; } }
        public string ValueDescription { get { return valueDescription; } set { valueDescription = value; } }
        public string SubValue { get { return subValue; } set { subValue = value; } }
        public string SubValueDescription { get { return subValueDescription; } set { subValueDescription = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("individualOption{");
            sb.Append("optionName=").Append(this.optionName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", value=").Append(this.value);
            sb.Append(", valueDescription=").Append(this.valueDescription);
            sb.Append(", subValue=").Append(this.subValue);
            sb.Append(", subValueDescription=").Append(this.subValueDescription);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
