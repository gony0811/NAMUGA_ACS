using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Transfer.Model
{
    public class PriorityRange : Entity
    {
        public static String VALUE_ASCENDING = "ASCENDING";
        public static String VALUE_DESCENDING = "DESCENDING";
        private String systemName;
        private int min;
        private int max;

        public String getSystemName()
        {
            return this.systemName;
        }

        public void setSystemName(String systemName)
        {
            this.systemName = systemName;
        }

        public int getMin()
        {
            return this.min;
        }

        public void setMin(int min)
        {
            this.min = min;
        }

        public int getMax()
        {
            return this.max;
        }

        public void setMax(int max)
        {
            this.max = max;
        }

        public String getDirection()
        {
            if (this.max < this.min)
            {
                return "DESCENDING";
            }
            return "ASCENDING";
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("priority{");
            sb.Append("systemName=").Append(this.systemName);
            sb.Append(", min=").Append(this.min);
            sb.Append(", max=").Append(this.max);
            sb.Append(", direction=").Append(getDirection());
            sb.Append("}");
            return sb.ToString();
        }
    }
}
