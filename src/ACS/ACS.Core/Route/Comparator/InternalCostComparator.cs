using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Route.Model;
using ACS.Core.Base.Interface;


namespace ACS.Core.Route.Comparator
{
    public class InternalCostComparator : IComparator
    {
        public int Compare(object object1, object object2)
        {
            InternalRoute route01 = (InternalRoute)object1;
            InternalRoute route02 = (InternalRoute)object2;

            int result = route01.Cost - route02.Cost;
            if (result == 0)
            {
                result = route01.NodeNames.Count - route02.NodeNames.Count;
            }
            return result;
        }
    }
}
