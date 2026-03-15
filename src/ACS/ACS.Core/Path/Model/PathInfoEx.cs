using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Route.Model;

namespace ACS.Core.Path.Model
{
    public class PathInfoEx : Entity
    {
        private NodeEx currentNode;
        private StationEx currentStation;
        private List<PathEx> pathsAvailable = new List<PathEx>();
        private List<PathEx> pathsUnavailable = new List<PathEx>();
        private bool existPath = true;
        private int maxHoppingCount;
        private int maxTotalCost;
        private bool useDynamicLoad;
        private Dictionary<string, SingleNode> singleNodes;
        private bool useHeuristicDelay;
        private Dictionary<string, TripleNode> tripleNodes;
        private int toleranceHoppingPercent = 0;
        private int toleranceCostPercent = 0;

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

        public List<PathEx> PathsAvailable
        {
            get { return pathsAvailable; }
            set { pathsAvailable = value; }
        }

        public List<PathEx> PathsUnavailable
        {
            get { return pathsUnavailable; }
            set { pathsUnavailable = value; }
        }

        public bool IsExistPath
        {
            get { return existPath; }
            set { existPath = value; }
        }

        public int MaxHoppingCount
        {
            get { return maxHoppingCount; }
            set { maxHoppingCount = value; }
        }

        public int MaxTotalCost
        {
            get { return maxTotalCost; }
            set { maxTotalCost = value; }
        }

        public bool IsUseDynamicLoad
        {
            get { return useDynamicLoad; }
            set { useDynamicLoad = value; }
        }

        public Dictionary<string, SingleNode> SingleNodes
        {
            get { return singleNodes; }
            set { singleNodes = value; }
        }

        public bool IsUseHeuristicDelay
        {
            get { return useHeuristicDelay; }
            set { useHeuristicDelay = value; }
        }

        public Dictionary<string, TripleNode> TripleNodes
        {
            get { return tripleNodes; }
            set { tripleNodes = value; }
        }

        public int ToleranceHoppingPercent
        {
            get { return toleranceHoppingPercent; }
            set { toleranceHoppingPercent = value; }
        }

        public int TolerenceCostPercent
        {
            get { return toleranceCostPercent; }
            set { toleranceCostPercent = value; }
        }

        public int AddPathAvailable(PathEx path)
        {
            this.pathsAvailable.Add(path);
            return pathsAvailable.Count;
        }

        public int AddPathUnavailable(PathEx path)
        {
            this.pathsUnavailable.Add(path);
            return pathsUnavailable.Count;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("pathInfo{");
            sb.Append("pathsAvailable{" + this.pathsAvailable.Count+ "}=").Append(this.pathsAvailable);
            sb.Append(", pathsUnavailable{" + this.pathsUnavailable.Count + "}=").Append(this.pathsUnavailable);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
