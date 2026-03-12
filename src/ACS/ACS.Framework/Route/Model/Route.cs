using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Route.Model.Node;

namespace ACS.Framework.Route.Model
{
    public class Route : Entity
    {
        private ArrayList nodeNames = new ArrayList();

        private ArrayList unavailableNodes = new ArrayList();

        private ArrayList bannedNodes = new ArrayList();

        private int cost;
        private int singleNodeLoad;
        private int interNodeLength;
        private int interNodeLoad;
        private int tripleNodeDelay;
        private bool available = true;
        private bool banned;
        private bool fix;
        private string currentTransportMachineName;
        private string destTransportMachineName;
        private bool useInternalRoute = false;
        private ArrayList linkedZoneNodes = new ArrayList();

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

        public ArrayList NodeNames { get { return nodeNames; } set { nodeNames = value; } }
        public ArrayList UnavailableNodes { get { return unavailableNodes; } set { unavailableNodes = value; } }
        public ArrayList BannedNodes { get { return bannedNodes; } set { bannedNodes = value; } }
        public int Cost { get { return cost; } set { cost = value; } }
        public int SingleNodeLoad { get { return singleNodeLoad; } set { singleNodeLoad = value; } }
        public int InterNodeLength { get { return interNodeLength; } set { interNodeLength = value; } }
        public int InterNodeLoad { get { return interNodeLoad; } set { interNodeLoad = value; } }
        public int TripleNodeDelay { get { return tripleNodeDelay; } set { tripleNodeDelay = value; } }
        public string CurrentTransportMachineName { get { return currentTransportMachineName; } set { currentTransportMachineName = value; } }
        public string DestTransportMachineName { get { return destTransportMachineName; } set { destTransportMachineName = value; } }
        public bool IsUseInternalRoute { get { return IsUseInternalRoute; } set { IsUseInternalRoute = value; } }
        public ArrayList LinkedZoneNodes { get { return linkedZoneNodes; } set { linkedZoneNodes = value; } }

        public int AddNodeName(string nodeName)
        {
            return this.nodeNames.Add(nodeName);
        }

        public void AddNodeNameAttheHead(string nodeName)
        {
            this.nodeNames.Insert(0, nodeName);
        }

        public int AddLinkedZoneNonde(string linkedZoneNodeName)
        {
            return this.linkedZoneNodes.Add(linkedZoneNodeName);
        }

        public bool ContainNodeName(string nodeName)
        {
            return this.nodeNames.Contains(nodeName);
        }

        public bool ContainNodeName(string nodeName, ArrayList linkedZoneNodes)
        {
            if (!ContainNodeName(nodeName))
            {
                if (linkedZoneNodes != null)
                {
                    foreach(LinkedZoneNode linkedZoneNode in linkedZoneNodes)
                    {
                        AddLinkedZoneNonde(linkedZoneNode.ZoneName);
                    }
                }
                return false;
            }

            bool result = true;

            foreach(LinkedZoneNode linkedZoneNode in linkedZoneNodes)
            {
                if(ContainsLinkedZoneNode(linkedZoneNode.ZoneName))
                {
                    result = false;
                    AddLinkedZoneNonde(linkedZoneNode.ZoneName);
                }
            }

            return result;
        }

        public bool ContainsLinkedZoneNode(string linkedZoneNodeName)
        {
            return this.linkedZoneNodes.Contains(linkedZoneNodeName);
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

        public int AddUnavailableNode(UnavailableNode unavailableNode)
        {
            return this.unavailableNodes.Add(unavailableNode);
        }

        public int AddUnavailableNode(string unavailableNodeName, InterNode interNode, string reason)
        {
            UnavailableNode unavailableNode = new UnavailableNode();
            unavailableNode.NodeName = unavailableNodeName;
            unavailableNode.InterNode = interNode;
            unavailableNode.Reason = reason;

            return this.unavailableNodes.Add(unavailableNode);
        }

        public string FirstNodeName()
        {
            if (this.nodeNames.Count > 0)
            {
                return (string)this.nodeNames[0];
            }
            return null;
        }

        public string LastNodeName()
        {
            return (string)this.nodeNames[this.nodeNames.Count - 1];
        }
    }
}
