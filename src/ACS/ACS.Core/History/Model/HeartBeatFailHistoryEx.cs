using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class HeartBeatFailHistoryEx : AbstractHistoryEx
    {
        public HeartBeatFailHistoryEx()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }
        public virtual string ApplicationName { get; set; }
        public virtual string Type { get; set; }
        public virtual string State { get; set; }
        public virtual DateTime? StartTime { get; set; }
        public virtual string InitialHardware { get; set; }
        public virtual string RunningHardware { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("heartBeatFailHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", applicationName=").Append(this.ApplicationName);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", state=").Append(this.State);
            sb.Append(", startTime=").Append(this.StartTime);
            sb.Append(", initialHardware=").Append(this.InitialHardware);
            sb.Append(", runningHardware=").Append(this.RunningHardware);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
