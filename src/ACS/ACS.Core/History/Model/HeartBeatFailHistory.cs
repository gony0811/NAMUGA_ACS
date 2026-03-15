using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class HeartBeatFailHistory: AbstractHistory
    {
        private string applicationName = "";
        private string type = "";
        private string state = "";
        private DateTime startTime;
        private string initialHardware = "";
        private string runningHardware = "";
      
        public string ApplicationName { get {return applicationName; } set { applicationName = value; } }
        public string Type { get {return type; } set { type = value; } }
        public string State { get {return state; } set { state = value; } }
        public DateTime StartTime { get {return startTime; } set { startTime = value; } }
        public string InitialHardware { get {return initialHardware; } set { initialHardware = value; } }
        public string RunningHardware { get { return runningHardware; } set { runningHardware = value; } }

        public HeartBeatFailHistory()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }

        public HeartBeatFailHistory(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public int GetPartitionId()
        {
            return base.PartitionId;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("heartBeatFailHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", startTime=").Append(this.startTime);
            sb.Append(", initialHardware=").Append(this.initialHardware);
            sb.Append(", runningHardware=").Append(this.runningHardware);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }


    }
}
