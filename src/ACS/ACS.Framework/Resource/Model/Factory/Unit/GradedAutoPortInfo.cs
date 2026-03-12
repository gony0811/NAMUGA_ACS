using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model.Factory.Distance;

namespace ACS.Framework.Resource.Model.Factory.Unit
{
    public class GradedAutoPortInfo
    {
        private bool useBidirectionalNode;
        private bool usePortModeChangeCommand;
        private List<GradedPort> suitableGradedAutoPorts = new List<GradedPort>();
        private List<GradedPort> suitableGradedAutoPortsOnBidirectionalNode = new List<GradedPort>();
        private List<GradedPort> unsuitableGradedAutoPortsAsState = new List<GradedPort>();
        private List<GradedPort> unsuitableGradedAutoPortsAsCommandPoolSize = new List<GradedPort>();

        public bool IsUseBidirectionalNode
        {
            get { return useBidirectionalNode; }
            set { useBidirectionalNode = value; }
        }

        public bool IsUsePortModeChangeCommand
        {
            get { return usePortModeChangeCommand; }
            set { usePortModeChangeCommand = value; }
        }

        public List<GradedPort> SuitableGradedAutoPorts
        {
            get { return suitableGradedAutoPorts; }
            set { suitableGradedAutoPorts = value; }
        }
           
        public List<GradedPort> SuitableGradedAutoPortsOnBidirectionalNode
        {
            get { return suitableGradedAutoPortsOnBidirectionalNode; }
            set { suitableGradedAutoPortsOnBidirectionalNode = value; }
        }

        public List<GradedPort> UnsuitableGradedAutoPortsAsState
        {
            get { return unsuitableGradedAutoPortsAsState; }
            set { unsuitableGradedAutoPortsAsState = value; }
        }

        public List<GradedPort> UnsuitableGradedAutoPortsAsCommandPoolSize
        {
            get { return unsuitableGradedAutoPortsAsCommandPoolSize; }
            set { unsuitableGradedAutoPortsAsCommandPoolSize = value; }
        }

        public void AddSuitableGradedAutoPort(GradedPort gradedPort)
        {
            this.suitableGradedAutoPorts.Add(gradedPort);
        }

        public void AddSuitableGradedAutoPortOnBidirectionalNode(GradedPort gradedPort)
        {
            this.suitableGradedAutoPortsOnBidirectionalNode.Add(gradedPort);
        }

        public void AddUnsuitableGradedAutoPortsAsState(GradedPort gradedPort)
        {
            this.unsuitableGradedAutoPortsAsState.Add(gradedPort);
        }

        public void AddUnsuitableGradedAutoPortsAsCommandPoolSize(GradedPort gradedPort)
        {
            this.unsuitableGradedAutoPortsAsCommandPoolSize.Add(gradedPort);
        }
    }
}
