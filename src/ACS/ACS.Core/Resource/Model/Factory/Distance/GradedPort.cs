using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource.Model.Factory.Unit;

namespace ACS.Core.Resource.Model.Factory.Distance
{
    public class GradedPort
    {
        public static int GRADE_HIGHEST = 7;
        public static int GRADE_HIGHER = 6;
        public static int GRADE_HIGH = 5;
        public static int GRADE_MIDDLE = 4;
        public static int GRADE_LOW = 3;
        public static int GRADE_LOWER = 2;
        public static int GRADE_LOWEST = 1;
        public static int GRADE_WORST = 0;

        private Port port;
        private int priority = 7;
        private int maxCapacity;
        private int currentCapacity;
        private int reservedCommandCount;
        private int distance;
        private string exceedCapacity = "F";
        private string biDirectional = "F";
        private string description = "";

        public Port Port
        {
            get { return port; }
            set { port = value; }
        }

        public int Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        public int MaxCapacity
        {
            get { return maxCapacity; }
            set { maxCapacity = value; }
        }

        public int CurrentCapacity
        {
            get { return currentCapacity; }
            set { currentCapacity = value; }
        }

        public int ReservedCommandCount
        {
            get { return reservedCommandCount; }
            set { reservedCommandCount = value; }
        }

        public int Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        public string BiDirectional
        {
            get { return biDirectional; }
            set { biDirectional = value; }
        }

        public string ExceedCapacity
        {
            get { return exceedCapacity; }
            set { exceedCapacity = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("portWithDistance{");
            sb.Append("port=").Append(this.port);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", reservedCommandCount=").Append(this.reservedCommandCount);
            sb.Append(", biDirectional=").Append(this.biDirectional);
            sb.Append(", exceedCapacity=").Append(this.exceedCapacity);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
