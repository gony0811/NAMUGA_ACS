using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.History.Model
{
    public class ApplicationHistoryEx : AbstractHistoryEx
    {
        private String applicationName = "";
        private String creator = "";
        private String editor = "";
        private String type = "";
        private String state = "";
        private DateTime startTime;
        private DateTime editTime;
        private DateTime checkTime;
        private String initialHardware = "";
        private String runningHardware = "";
        private String runningHardwareAddress;
        private String msb;
        private String memory;
        private String jmx;
        private String destinationName;

        public string ApplicationName { get { return applicationName; } set { applicationName = value; } }
        public string Creator { get { return creator; } set { creator = value; } }
        public string Editor { get { return editor; } set { editor = value; } }
        public string Type { get { return type; } set { type = value; } }
        public string State { get { return state; } set { state = value; } }
        public DateTime StartTime { get { return startTime; } set { startTime = value; } }
        public DateTime EditTime { get { return editTime; } set { editTime = value; } }
        public DateTime CheckTime { get { return checkTime; } set { checkTime = value; } }
        public string InitialHardware { get { return initialHardware; } set { initialHardware = value; } }
        public string RunningHardware { get { return runningHardware; } set { runningHardware = value; } }
        public string RunningHardwareAddress { get { return runningHardwareAddress; } set { runningHardwareAddress = value; } }
        public string Msb { get { return msb; } set { msb = value; } }
        public string Memory { get { return memory; } set { memory = value; } }
        public string Jmx { get { return jmx; } set { jmx = value; } }
        public string DestinationName { get { return destinationName; } set { destinationName = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("applicationHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", creator=").Append(this.creator);
            sb.Append(", editor=").Append(this.editor);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", startTime=").Append(this.startTime);
            sb.Append(", editTime=").Append(this.editTime);
            sb.Append(", checkTime=").Append(this.checkTime);
            sb.Append(", initialHardware=").Append(this.initialHardware);
            sb.Append(", runningHardware=").Append(this.runningHardware);
            sb.Append(", runningHardwareAddress=").Append(this.runningHardwareAddress);
            sb.Append(", msb=").Append(this.msb);
            sb.Append(", memory=").Append(this.memory);
            sb.Append(", jmx=").Append(this.jmx);
            sb.Append(", destinationName=").Append(this.destinationName);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
