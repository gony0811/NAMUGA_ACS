using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Path.Model
{
    public class PathEx : Entity, ICloneable
    {
        protected List<string> nodeIds = new List<string>();
        protected List<LinkEx> unavailableLinks = new List<LinkEx>();
        protected int cost;
        protected int singleNodeLoad;
        protected int interNodeLength;
        protected int interNodeLoad;
        protected int tripleNodeDelay;
        protected bool available = true;
        protected bool banned;
        protected bool fix;
        protected string currentNodeId;
        protected string destNodeId;
        protected NodeEx currentNode;
        protected StationEx currentStation;

        public List<string> NodeIds
        {
            get { return nodeIds; }
            set { nodeIds = value; }
        }

        public List<LinkEx> UnavailableLinks
        {
            get { return unavailableLinks; }
            set { unavailableLinks = value; }
        }

        public int Cost
        {
            get { return cost; }
            set { cost = value; }
        }

        public int SingleNodeLoad
        {
            get { return singleNodeLoad; }
            set { singleNodeLoad = value; }
        }

        public int InterNodeLength
        {
            get { return interNodeLength; }
            set { interNodeLength = value; }
        }

        public int InterNodeLoad
        {
            get { return interNodeLoad; }
            set { interNodeLoad = value; }
        }

        public int TripleNodeDelay
        {
            get { return tripleNodeDelay; }
            set { tripleNodeDelay = value; }
        }

        public bool IsAvailable
        {
            get { return available; }
            set { available = value; }
        }

        public bool IsBanned
        {
            get { return banned; }
            set { banned = value; }
        }

        public bool IsFixed
        {
            get { return fix; }
            set { fix = value; }
        }

        public string CurrentNodeId
        {
            get { return currentNodeId; }
            set { currentNodeId = value; }
        }

        public string DestNodeId
        {
            get { return destNodeId; }
            set { destNodeId = value; }
        }

        public NodeEx CurrentNode
        {
            get { return currentNode; }
            set { currentNode = value; }
        }

        public StationEx CurrentStation
        {
            get { return currentStation; }
            set { currentStation = value; }
        }

        public void AddNodeId(string nodeId)
        {
            this.nodeIds.Add(nodeId);
        }

        public void AddNodeIdAttachHead(string nodeId)
        {
            this.nodeIds.Insert(0, nodeId);
        }

        public bool ContainNodeId(string nodeId)
        {
            return this.nodeIds.Contains(nodeId);
        }

        public int AddCost(int singleNodeLoad, int interNodeLength, int interNodeLoad, int tripleNodeDelay)
        {
            this.singleNodeLoad += singleNodeLoad;
            this.interNodeLength += interNodeLength;
            this.interNodeLoad += interNodeLoad;
            this.tripleNodeDelay += tripleNodeDelay;

            this.cost = (this.singleNodeLoad + this.interNodeLength + this.interNodeLoad + this.tripleNodeDelay);
            return this.cost;
        }

        public string FirstNodeId()
        {
            if (this.nodeIds.Count > 0)
            {
                return (string)this.nodeIds[0];
            }

            return "";
        }

        public string LastNodeId()
        {
            return (string)this.nodeIds[this.nodeIds.Count - 1];
        }

        public object Clone()
        {
            PathEx clone = new PathEx();
            if (this.nodeIds.Count > 0)
            {
                clone.nodeIds.AddRange(this.nodeIds);
            }
            clone.IsAvailable = this.available;
            clone.IsBanned = this.banned;
            clone.IsFixed = this.fix;

            clone.Cost = this.cost;
            clone.SingleNodeLoad = this.singleNodeLoad;
            clone.InterNodeLength = this.interNodeLength;
            clone.InterNodeLoad = this.interNodeLoad;
            clone.TripleNodeDelay = this.tripleNodeDelay;

            clone.CurrentNodeId = this.currentNodeId;
            clone.DestNodeId = this.destNodeId;

            clone.CurrentNode = this.currentNode;
            clone.CurrentStation = this.currentStation;

            return clone;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("route{");
            sb.Append("hopping=").Append(this.nodeIds.Count);
            sb.Append(", cost=").Append(this.cost);
            sb.Append("{");
            sb.Append("singleNodeLoad(").Append(this.singleNodeLoad).Append("),");
            sb.Append("interNodeLength(").Append(this.interNodeLength).Append("),");
            sb.Append("interNodeLoad(").Append(this.interNodeLoad).Append("),");
            sb.Append("tripleNodeDelay(").Append(this.tripleNodeDelay).Append(")}");
            sb.Append(", nodeNames=").Append(this.nodeIds);
            sb.Append(", available=").Append(this.available);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", fixed=").Append(this.fix) ;

            sb.Append("}");
            return sb.ToString();
        }
    }
}
