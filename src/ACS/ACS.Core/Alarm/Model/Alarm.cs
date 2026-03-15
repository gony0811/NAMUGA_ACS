using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Alarm.Model
{
    public class Alarm : Entity
    {
        private String alarmCode = "";
        private String alarmText = "";
        private String vehicleId = "";
        private String alarmId = "";
        private String transportCommandId = "";

        protected DateTime? createTime = new DateTime(DateTime.UtcNow.Ticks);

        public Alarm()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string AlarmCode
        {
            get { return alarmCode; }
            set { alarmCode = value; }
        }

        public string AlarmText
        {
            get { return alarmText; }
            set { alarmText = value; }
        }

        public string VehicleId
        {
            get { return vehicleId; }
            set { vehicleId = value; }
        }

        public string TransportCommandId
        {
            get { return transportCommandId; }
            set { transportCommandId = value; }
        }

        public string AlarmId
        {
            get { return alarmId; }
            set { alarmId = value; }
        }

        public DateTime? CreateTime
        {
            get { return createTime; }
            set { createTime = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarm{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmCode=").Append(this.alarmCode);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append(", vehicleId=").Append(this.vehicleId);
            sb.Append(", createTime=").Append(this.createTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
