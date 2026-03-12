using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Application.Model
{
    public class Option : Entity
    {
        public static String OPTION_NAME_USERFMODE = "1001";
        public static String OPTION_NAME_DESC_USERFMODE = "USERFMODE";
        public static String OPTION_VALUE_ACCEPTTRANSPORTREQUEST_TRUE = "01";
        public static String OPTION_VALUE_ACCEPTTRANSPORTREQUEST_FALSE = "02";
        public static String OPTION_VALUE_DESC_USED = "USED";
        public static String OPTION_VALUE_DESC_NOTUSED = "NOTUSED";
        public static String OPTION_VALUE_DESC_TRUE = "TRUE";
        public static String OPTION_VALUE_DESC_FALSE = "FALSE";
        public static String OPTION_VALUE_DESC_INDIVIDUAL = "INDIVIDUAL";
        public static String OPTION_SUBVALUE_DESC_TIME = "TIME";

        private string name;
        private string nameDescription;
        private string value;
        private string valueDescription;
        private string subValue;
        private string subValueDescription;
        private string used = "T";

        public virtual string Name { get { return name; } set { name = value; } }
        public virtual string NameDescription { get { return nameDescription; } set { nameDescription = value; } }
        public virtual string Value { get { return value; } set { this.value = value; } }
        public virtual string ValueDescription { get { return valueDescription; } set { valueDescription = value; } }
        public virtual string SubValue { get { return subValue; } set { subValue = value; } }
        public virtual string SubValueDescription { get { return subValueDescription; } set { subValueDescription = value; } }
        public virtual string Used { get { return used; } set { used = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("option{");
            sb.Append("name=").Append(this.name);
            sb.Append(", nameDescription=").Append(this.nameDescription);
            sb.Append(", value=").Append(this.value);
            sb.Append(", valueDescription=").Append(this.valueDescription);
            sb.Append(", subValue=").Append(this.subValue);
            sb.Append(", subValueDescription=").Append(this.subValueDescription);
            sb.Append(", used=").Append(this.used);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
