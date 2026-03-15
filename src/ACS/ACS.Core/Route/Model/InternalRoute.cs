using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ACS.Core.Route.Model
{
    public class InternalRoute : Route, ICloneable
    {
        private IList originalNodeNames = new ArrayList();
        private int originalCost = 0;

        public IList OriginalNodeNames { get { return originalNodeNames; } set { originalNodeNames = value; } }
        public int OriginalCost { get { return originalCost; } set { originalCost = value; } }

        object ICloneable.Clone()
        {
            InternalRoute clone = new InternalRoute();

            foreach(string nodeName in this.NodeNames)
            {
                clone.NodeNames.Add(nodeName);
            }

            clone.IsAvailable = this.IsAvailable;
            clone.Cost = this.Cost;
            clone.OriginalCost = this.OriginalCost;
            clone.CurrentTransportMachineName = this.CurrentTransportMachineName;
            clone.DestTransportMachineName = this.DestTransportMachineName;

            return clone;
        }
    }
}
