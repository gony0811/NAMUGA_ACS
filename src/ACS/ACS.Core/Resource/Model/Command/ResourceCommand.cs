using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Command
{
    public class ResourceCommand : TimedEntity
    {
        protected string machineName;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("resourceCommand{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
