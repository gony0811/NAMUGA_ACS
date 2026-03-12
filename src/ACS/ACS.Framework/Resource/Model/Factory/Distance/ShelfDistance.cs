using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model.Factory.Unit;

namespace ACS.Framework.Resource.Model.Factory.Distance
{
    public class ShelfDistance
    {
        private Shelf shelf;
        private double distance;
        private int absValueX;
        private int absValueY;
        private int absValueZ;

        public int AbsValueX
        {
            get { return absValueX; }
            set { absValueX = value; }
        }

        public int AbsValueY
        {
            get { return absValueY; }
            set { absValueY = value; }
        }
        public int AbsValueZ
        {
            get { return absValueZ; }
            set { absValueZ = value; }
        }

        public Shelf Shelf
        {
            get { return shelf; }
            set { shelf = value; }
        }

        public double Distance
        {
            get { return distance; }
            set { distance = value; }
        }

    }
}
