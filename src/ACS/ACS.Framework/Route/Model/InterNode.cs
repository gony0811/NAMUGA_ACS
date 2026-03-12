using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model
{
    public class InterNode : Entity
    {
        public static string AVAILABILITY_AVAILABLE = "0";
        public static string AVAILABILITY_UNAVAILABLE = "1";
        public static string AVAILABILITY_BANNED = "2";
        public static string AVAILABLE = "AVAILABLE";
        public static string UNAVAILABLE = "UNAVAILABLE";
        public static string BANNED = "BANNED";
        private string fromMachineName = "";
        private string toMachineName = "";
        private int length;
        private int load;
        private string fromAvailability = "0";
        private string toAvailability = "0";

        public string FromMachineName { get { return fromMachineName; } set { fromMachineName = value; } }
        public string ToMachineName { get { return toMachineName; } set { toMachineName = value; } }
        public int Length { get { return length; } set { length = value; } }
        public int Load { get { return load; } set { load = value; } }
        public string FromAvailability { get { return fromAvailability; } set { fromAvailability = value; } }
        public string ToAvailability { get { return toAvailability; } set { toAvailability = value; } }

        public int CalculateTotalCost()
        {
            return this.length + this.load;
        }

        public string ToCompactString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("interNode{");
            sb.Append(this.fromMachineName);
            sb.Append(" -> ").Append(this.toMachineName);
            sb.Append(", (from:to) ").Append(this.fromAvailability);
            sb.Append(":").Append(this.toAvailability);

            sb.Append("}");
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("interNode{");
            sb.Append("fromMachineName=").Append(this.fromMachineName);
            sb.Append(", toMachineName=").Append(this.toMachineName);
            sb.Append(", fromAvailability=").Append(this.fromAvailability);
            sb.Append(", toAvailability=").Append(this.toAvailability);
            sb.Append(", length=").Append(this.length);
            sb.Append(", load=").Append(this.load);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
