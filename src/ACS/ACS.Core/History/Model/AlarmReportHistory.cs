using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class AlarmReportHistory: AbstractHistory
    {
        public virtual string MachineName { get; set; }
        public virtual string UnitName { get; set; }
        public virtual string AlarmId { get; set; }
        public virtual string AlarmCode { get; set; }
        public virtual string AlarmText { get; set; }
        public virtual string State { get; set; }

        public AlarmReportHistory()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }

        public AlarmReportHistory(int partitionId)
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

            sb.Append("alarmReportHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", machineName=").Append(this.MachineName);
            sb.Append(", unitName=").Append(this.UnitName);
            sb.Append(", alarmId=").Append(this.AlarmId);
            sb.Append(", alarmCode=").Append(this.AlarmCode);
            sb.Append(", alarmText=").Append(this.AlarmText);
            sb.Append(", state=").Append(this.State);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
