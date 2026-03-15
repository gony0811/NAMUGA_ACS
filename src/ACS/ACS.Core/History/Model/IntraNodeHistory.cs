using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class IntraNodeHistory: AbstractHistory
    {
        private string machineName = "";
        private string unitName = "";
        private string transportMachineName = "";
        private string portTransferState = "";
   
        public string MachineName { get {return machineName; } set { machineName = value; } }
        public string UnitName { get {return unitName; } set { unitName = value; } }
        public string TransportMachineName { get {return transportMachineName; } set { transportMachineName = value; } }
        public string PortTransferState { get { return portTransferState; } set { portTransferState = value; } }

        public IntraNodeHistory()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }

        public IntraNodeHistory(int partitionId)
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

            sb.Append("portHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", partitionId=").Append(this.PartitionId);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", transportMachineName=").Append(this.transportMachineName);
            sb.Append(", portTransferState=").Append(this.portTransferState);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
