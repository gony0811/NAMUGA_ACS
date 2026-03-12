using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model;

namespace ACS.Extension.Framework.Path.Comparator
{
    public class NodeCheckTimeComparator : IComparer<VehicleEx>
    {
        public int Compare(VehicleEx vahicle01, VehicleEx vahicle02)
        {
            return vahicle01.NodeCheckTime.CompareTo(vahicle02.NodeCheckTime);
        }
    }
}
