using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Route;

namespace ACS.Framework.Route.Model.Node
{
    public class Node
    {
        private string nodeName;
        private InterNode interNode;

        public string NodeName { get { return nodeName; } set { nodeName = value; } }
        public InterNode InterNode { get { return interNode; } set { interNode = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("node{");

            sb.Append("nodeName=").Append(this.nodeName);
            sb.Append(", interNode=").Append(this.interNode);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
