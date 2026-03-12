using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base.Interface;
using ACS.Framework.Resource.Model;

namespace ACS.Framework.Path.Comparator
{
    public class NodeCheckTimeComparator : IComparator
    {
        public int Compare(object object1, object object2)
        {
            VehicleEx vahicle01 = (VehicleEx)object1;
            VehicleEx vahicle02 = (VehicleEx)object2;

            return vahicle01.NodeCheckTime.CompareTo(vahicle02.NodeCheckTime);
        }
    }
}
