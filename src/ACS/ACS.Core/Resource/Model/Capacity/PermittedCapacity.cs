using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Capacity
{
    public class PermittedCapacity : TimedEntity
    {
        public static string CRITERIA_RESULTCODE = "RESULTCODE";
        public static string CRITERIA_CARRIERLOC = "CARRIERLOC";

        private string samplingTime = "";
        private string machineType = "";
        private string machineName = "";
        private string portName = "";
        private string transportMachineName = "";
        private string used = "T";
        private int threshold;
        private int decrease;
        private int increase;
        private int min;
        private int max;
        private int original;
        private DateTime checkTime = new DateTime();
        private string criteria = "CARRIERLOC";

        public PermittedCapacity()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public PermittedCapacity(string machineName, string portName)
        {
            this.machineName = machineName;
            this.portName = portName;

            this.Id = this.machineName + "-" + this.portName;
        }

        public string SamplingTime
        {
            get { return samplingTime; }
            set { samplingTime = value; }
        }

        public string MachineType
        {
            get { return machineType; }
            set { machineType = value; }
        }

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }

        public string TransportMachineName
        {
            get { return transportMachineName; }
            set { transportMachineName = value; }
        }

        public string Used
        {
            get { return used; }
            set { used = value; }
        }

        public int Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }

        public int Decrease
        {
            get { return decrease; }
            set { decrease = value; }
        }

        public int Increase
        {
            get { return increase; }
            set { increase = value; }
        }

        public int Min
        {
            get { return min; }
            set { min = value; }
        }

        public int Max
        {
            get { return max; }
            set { max = value; }
        }

        public int Original
        {
            get { return original; }
            set { original = value; }
        }
        public string Criteria
        {
            get { return criteria; }
            set { criteria = value; }
        }

        public DateTime CheckTime
        {
            get { return checkTime; }
            set { checkTime = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("permittedCapacity{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", machineType=").Append(this.machineType);
            sb.Append(", transportMachineName=").Append(this.transportMachineName);
            sb.Append(", criteria=").Append(this.criteria);
            sb.Append(", samplingTime=").Append(this.samplingTime);
            sb.Append(", used=").Append(this.used);
            sb.Append(", threshold=").Append(this.threshold);
            sb.Append(", decrease=").Append(this.decrease);
            sb.Append(", increase=").Append(this.increase);
            sb.Append(", min=").Append(this.min);
            sb.Append(", max=").Append(this.max);
            sb.Append(", checkTime=").Append(this.checkTime);
            sb.Append(", origianl=").Append(this.original);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
