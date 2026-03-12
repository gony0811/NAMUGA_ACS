using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Route.Model;

namespace ACS.Framework.Route.Model.Node
{
    public class UnavailableNode : Node
    {
        private string reason;

        public string Reason { get { return reason; } set { reason = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unavailableNode{");

            sb.Append("nodeName=").Append(this.NodeName);
            if (this.InterNode != null)
            {
                sb.Append(", ").Append(this.InterNode.ToCompactString());
            }
            sb.Append(", reason=").Append(this.reason);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
