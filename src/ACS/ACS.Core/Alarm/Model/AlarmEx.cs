using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Alarm.Model
{
    public class AlarmEx
    {
        public AlarmEx()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public virtual string Id { get ; set;}
        public virtual string AlarmCode { get; set; }
        public virtual string AlarmText { get; set; }
        public virtual string VehicleId { get; set; }
        public virtual DateTime? CreateTime { get; set; }
        public virtual string AlarmId { get; set; }
        public virtual string TransportCommandId { get; set; }

        public virtual string NearAgv { get; set; }

        public virtual string IsCross { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarm{");
            sb.Append("id=").Append(Id);
            sb.Append(", alarmId=").Append(this.AlarmId);
            sb.Append(", alarmCode=").Append(this.AlarmCode);
            sb.Append(", alarmText=").Append(this.AlarmText);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", nearAgv=").Append(this.NearAgv);
            sb.Append(", isCross=").Append(this.IsCross);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
