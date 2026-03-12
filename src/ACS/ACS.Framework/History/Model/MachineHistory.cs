using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.History.Model
{
    public class MachineHistory : AbstractHistory
    {
        private string controlState = "";
        private string connectionState = "";
        private string tscState = "";
        private string state = "";
        private string processingState = "";
        private string banned = "";
        private string machineName = "";
        
        public string ControlState { get {return controlState; } set { controlState = value; } }
        public string ConnectionState { get {return connectionState; } set { connectionState = value; } }
        public string TscState { get {return tscState; } set { tscState = value; } }
        public string State { get {return state; } set { state = value; } }
        public string ProcessingState { get {return processingState; } set { processingState = value; } }
        public string Banned { get {return banned; } set { banned = value; } }
        public string MachineName { get { return machineName; } set { machineName = value; } }

        public MachineHistory()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }

        public MachineHistory(int partitionId)
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

            sb.Append("machineHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", connectionState=").Append(this.connectionState);
            sb.Append(", controlState=").Append(this.controlState);
            sb.Append(", tscState=").Append(this.tscState);
            sb.Append(", state=").Append(this.state);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
