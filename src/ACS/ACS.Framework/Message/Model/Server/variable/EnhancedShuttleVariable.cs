using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server.variable
{
    public class EnhancedShuttleVariable
    {
        private string lifterName;
        private string shuttleName;
        private string location;
        private string state;

        public string LifterName { get { return lifterName; } set { lifterName = value; } }
        public string ShuttleName { get { return shuttleName; } set { shuttleName = value; } }
        public string Location { get { return location; } set { location = value; } }
        public string State { get { return state; } set { state = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedShuttleVariable{");
            sb.Append("shuttleName=").Append(this.shuttleName);
            sb.Append(", location=").Append(this.location);
            sb.Append(", state=").Append(this.state);
            sb.Append(", lifterName=").Append(this.lifterName);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
