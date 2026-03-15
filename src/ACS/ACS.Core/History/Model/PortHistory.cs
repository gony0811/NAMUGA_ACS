using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class PortHistory: AbstractHistory
    {
        private string machineName = "";
        private string portName = "";
        private string state = "";
        private string subState = "";
        private string processingState = "";
        private string transferState = "";
        private string banned = "";
        /// <summary>
        /// @deprecated
        /// </summary>
        private string craneAvailable = "";
        private string transportUnitAccessible = "";
        private string inOutType = "";
        private string manual = "";
        private string idReadState = "";
        private string accessMode = "";
        private string occupied = "";
        private string reserved = "";

        public string MachineName { get {return machineName; } set { machineName = value; } }
        public string PortName { get {return portName; } set { portName = value; } }
        public string State { get {return state; } set { state = value; } }
        public string SubState { get {return subState; } set { subState = value; } }
        public string ProcessingState { get {return processingState; } set { processingState = value; } }
        public string TransferState { get {return transferState; } set { transferState = value; } }
        public string Banned { get {return banned; } set { banned = value; } }

        public string TransportUnitAccessible { get {return transportUnitAccessible; } set { transportUnitAccessible = value; } }
        public string InOutType { get {return inOutType; } set { inOutType = value; } }
        public string Manual { get {return manual; } set { manual = value; } }
        public string IdReadState { get {return idReadState; } set { idReadState = value; } }
        public string AccessMode { get {return accessMode; } set { accessMode = value; } }
        public string Occupied { get {return occupied; } set { occupied = value; } }
        public string Reserved { get { return reserved; } set { reserved = value; } }

        public PortHistory()
        {
            this.PartitionId = base.CreatePartitionIdByMonth();
        }

        public PortHistory(int partitionId)
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
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", state=").Append(this.state);
            sb.Append(", subState=").Append(this.subState);
            sb.Append(", transferState=").Append(this.transferState);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", transportUnitAccessible=").Append(this.transportUnitAccessible);
            sb.Append(", inOutType=").Append(this.inOutType);
            sb.Append(", manual=").Append(this.manual);
            sb.Append(", idReadState=").Append(this.idReadState);
            sb.Append(", accessMode=").Append(this.accessMode);
            sb.Append(", time=").Append(this.Time);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
