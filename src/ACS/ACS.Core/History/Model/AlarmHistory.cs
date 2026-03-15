using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class AlarmHistory : AbstractHistory
    {
        private string machineName = "";
        private string unitName = "";
        private string unitState = "";
        private string transportCommandId = "";
        private string errorNumber = "";
        private string errorId = "";
        private string recoveryOptions = "";
        private string state = "SET";

        public AlarmHistory()
        {
            this.PartitionId = base.CreatePartitionIdByDate();
        }

        public AlarmHistory(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string UnitName
        {
            get { return unitName; }
            set { unitName = value; }
        }

        public string UnitState
        {
            get { return unitState; }
            set { unitState = value; }
        }

        public string TransportCommandId
        {
            get { return transportCommandId; }
            set { transportCommandId = value; }
        }

        public string ErrorNumber
        {
            get { return errorNumber; }
            set { errorNumber = value; }
        }

        public string ErrorId
        {
            get { return errorId; }
            set { errorId = value; }
        }

        public string RecoveryOptions
        {
            get { return recoveryOptions; }
            set { recoveryOptions = value; }
        }

        public string State
        {
            get { return state; }
            set { state = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("alarmHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", unitState=").Append(this.unitState);

            sb.Append(", transportCommandId=").Append(this.transportCommandId);

            sb.Append(", errorNumber=").Append(this.errorNumber);
            sb.Append(", errorId=").Append(this.errorId);
            sb.Append(", recoveryOptions=").Append(this.recoveryOptions);

            sb.Append(", state=").Append(this.state);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
