using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Path.Model;

namespace ACS.Extension.Framework.Path.Model
{
    public class PathExs : PathEx, IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            PathEx arg0 = (PathEx)x;
            PathEx arg1 = (PathEx)y;

            if (arg0.Cost > arg1.Cost)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
    }
}