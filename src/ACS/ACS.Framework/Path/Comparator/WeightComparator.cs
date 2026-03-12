using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base.Interface;
using ACS.Framework.Path.Model;

namespace ACS.Framework.Path.Comparator
{
    public class WeightComparator : IComparator
    {
        public int Compare(object object1, object object2)
        {
            PathEx path01 = (PathEx)object1;
            PathEx path02 = (PathEx)object2;

            int result = path01.Cost - path02.Cost;
            if (result == 0)
            {
                result = path01.NodeIds.Count - path02.NodeIds.Count;
            }
            return result;
        }
    }
}
