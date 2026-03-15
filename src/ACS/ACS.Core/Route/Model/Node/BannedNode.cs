using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Route.Model;

namespace ACS.Core.Route.Model.Node
{
    public class BannedNode : Node
    {
        private string reason;
        public string Reason { get { return reason; } set { reason = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("bannedNode{");

            sb.Append("nodeName=").Append(this.NodeName);
            sb.Append(", ").Append(this.InterNode.ToCompactString());
            sb.Append(", reason=").Append(this.reason);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
