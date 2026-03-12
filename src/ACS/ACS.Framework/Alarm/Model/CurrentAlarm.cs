using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Alarm.Model
{
    public class CurrentAlarm : Entity
    {
        public static string ERRORID_SOURCEEMPTY = "SourceEmpty";
        public static string ERRORID_DESTOCCUPIED = "DestOccupied";
        public static string RECOVERYOPTIONS_RETRY = "RETRY";
        public static string RECOVERYOPTIONS_ABORT = "ABORT";
        private string machineName;
        private string unitName;
        private string unitState;
        private string commandId;
        private string errorId;
        private string errorNumber;
        private string recoveryOptions;
        private DateTime? createTime = new DateTime();

        public CurrentAlarm()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string UnitState { get { return unitState; } set { unitState = value; } }
        public string CommandId { get { return commandId; } set { commandId = value; } }
        public string ErrorId { get { return errorId; } set { errorId = value; } }
        public string ErrorNumber { get { return errorNumber; } set { errorNumber = value; } }
        public string RecoveryOptions { get { return recoveryOptions; } set { recoveryOptions = value; } }
        public DateTime? CreateTime { get { return createTime; } set { createTime = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("currentAlarm{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);

            sb.Append(", unitState=").Append(this.unitState);
            sb.Append(", commandId=").Append(this.commandId);
            sb.Append(", errorId=").Append(this.errorId);
            sb.Append(", errorNumber=").Append(this.errorNumber);
            sb.Append(", recoveryOptions=").Append(this.recoveryOptions);
            sb.Append(", createTime=").Append(this.createTime);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
