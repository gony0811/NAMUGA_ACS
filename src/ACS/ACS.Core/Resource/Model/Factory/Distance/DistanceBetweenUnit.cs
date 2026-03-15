using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Factory.Distance
{
    public class DistanceBetweenUnit : Entity
    {
        public static int MAXDISTANCE = 10000;
        private string machineName = "";
        private string fromUnitName = "";
        private string toUnitName = "";
        private int distance;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string FromUnitName
        {
            get { return fromUnitName; }
            set { fromUnitName = value; }
        }

        public string ToUnitName
        {
            get { return toUnitName; }
            set { toUnitName = value; }
        }

        public int Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("distanceBetweenUnit{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", fromUnitName=").Append(this.fromUnitName);
            sb.Append(", toUnitName=").Append(this.toUnitName);
            sb.Append(", distance=").Append(this.distance);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
