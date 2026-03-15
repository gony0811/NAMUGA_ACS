using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Alarm.Model
{
    public class AlarmReport : Entity 
    {
        private String machineName = "";
        private String unitName = "";
        private String alarmId = "";
        private String alarmCode = "";
        private String alarmText = "";
        public static int ALARMCODE_SET_INT_VALUE = 128;
        protected DateTime? createTime = new DateTime(DateTime.Now.Millisecond);

        public string MachineName { get {return machineName;} set {machineName = value;}}
        public string UnitName { get {return unitName;} set {unitName = value;}}
        public string AlarmId { get {return alarmId;} set {alarmId = value;}}
        public string AlarmCode { get {return alarmCode;} set {alarmCode = value;}}
        public string AlarmText { get { return alarmText; } set { alarmText = value; } }

        public AlarmReport()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public AlarmReport(String machineName, String alarmId, String alarmCode, String alarmText)
        {
            this.Id = Guid.NewGuid().ToString();

            this.machineName = machineName;
            this.unitName = "";
            this.alarmId = alarmId;
            this.alarmCode = alarmCode;
            this.alarmText = alarmText;
        }

        public AlarmReport(String machineName, String unitName, String alarmId, String alarmCode, String alarmText)
        {
            this.Id = Guid.NewGuid().ToString();

            this.machineName = machineName;
            this.unitName = unitName;
            this.alarmId = alarmId;
            this.alarmCode = alarmCode;
            this.alarmText = alarmText;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("alarmReport{");
            sb.Append("machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", alarmId=").Append(this.alarmId);
            sb.Append(", alarmCode=").Append(this.alarmCode);
            sb.Append(", alarmText=").Append(this.alarmText);
            sb.Append(", createTime=").Append(this.createTime);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
