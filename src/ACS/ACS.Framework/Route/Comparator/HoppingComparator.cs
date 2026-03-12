using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Route;
using ACS.Framework.Base.Interface;

namespace ACS.Framework.Route.Comparator
{
    public class HoppingComparator : IComparator
    {
        public int Compare(object object1, object object2)
        {
            Model.Route route1 = object1 as Model.Route;
            Model.Route route2 = object2 as Model.Route;

            int result = route1.NodeNames.Count - route2.NodeNames.Count;

            if(result == 0)
            {
                result = route1.Cost - route2.Cost;
            }

            return result;
        }
    }
}
