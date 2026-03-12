using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Factory.Alias
{
    public class UnitAlias : Entity
    {
        private string machineName;
        private string unitName;
        private string aliasName;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string UnitName
        {
            get { return unitName; }
            set { unitName = value; }
        }

        public string AliasName
        {
            get { return aliasName; }
            set { aliasName = value; }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unitAlias{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", aliasName=").Append(this.aliasName);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
