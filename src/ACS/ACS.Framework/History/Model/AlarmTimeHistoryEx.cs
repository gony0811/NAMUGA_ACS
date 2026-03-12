using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.History.Model;

namespace ACS.Framework.History.Model
{
    public class AlarmTimeHistoryEx : AbstractHistoryEx
    {
        public AlarmTimeHistoryEx()
        {
            this.PartitionId = CreatePartitionIdByDate();
        }

        public AlarmTimeHistoryEx(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public virtual string AlarmCode { get; set; }
        public virtual string AlarmText { get; set; }

        public virtual string VehicleId { get; set; }
        public virtual string AlarmId { get; set; }
        public virtual DateTime? CreateTime { get; set; }

        public virtual DateTime? ClearTime { get; set; }
        public virtual string TransportCommandId { get; set; }

        public virtual string NearAgv { get; set; }

        public virtual string BayId { get; set; }
        public virtual string IsCross { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("AlarmTimetHistory{");
            sb.Append("Id=").Append(this.Id);
            sb.Append(", AlarmCode=").Append(this.AlarmCode);
            sb.Append(", AlarmText=").Append(this.AlarmText);
            sb.Append(", VehicleId=").Append(this.VehicleId);
            sb.Append(", CreateTime=").Append(this.CreateTime);
            sb.Append(", ClearTime=").Append(this.ClearTime);
            sb.Append(", TransportCommandId=").Append(this.TransportCommandId);
            sb.Append(", NearAgv=").Append(this.NearAgv);
            sb.Append(", BayId=").Append(this.BayId);
            sb.Append(", IsCross=").Append(this.IsCross);
            sb.Append(", Time=").Append(this.Time);
            sb.Append("}");

            return sb.ToString();
        }

    }
}
