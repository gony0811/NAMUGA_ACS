using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model.Factory.Unit;
using ACS.Framework.Resource.Model.Factory.Machine;
using ACS.Framework.Resource.Model.Factory.Zone;
using ACS.Framework.Message.Model.State;
using ACS.Framework.Message.Model;



namespace ACS.Framework.Message.Model.Server
{
    public class TransferMessage: BaseMessage
    {
        public const string CHECKRESULT_SUCCESS = "SUCCESS";
        private string destGroupName;
        private string destPortGroupName;
        private string destMachineName;
        private string destUnitName;
        private string destContainerName;
        private string destSlotNumber;
        private string fromUnitName;
        private string toUnitName;
        private string priority = "50";
        private string destType = "NOTDESIGNATED";
        private string toType = "NOTDESIGNATED";
        private string carrierKind;
        private Machine sourceMachine;
        private Machine destMachine;
        private Unit sourceUnit;
        private string sourceContainerName;
        private string sourceSlotNumber;
        private Unit destUnit;
        private Zone destZone;
        private Machine fromMachine;
        private Machine toMachine;
        private Unit fromUnit;
        private Unit toUnit;
        private Zone toZone;
        private TransferState transferState;
        private int maxHoppingCount = -1;
        private int maxTotalCost = -1;
        /// <summary>
        /// @deprecated
        /// </summary>
        //private int maxTotalWeight = this.maxTotalCost;
        //private StageCommand stageCommand;
        //private StageDeleteCommand stageDeleteCommand;
        //private ScanCommand scanCommand;
        //private MaterialCommand materialCommand;
        //private ResourceCommand resourceCommand;
        private bool useInternalRoute = false;
        private string description = "success";
        private string troubleMachineName;

        protected internal string DestGroupName { get { return destGroupName; } set { destGroupName = value; } }
        protected internal string DestPortGroupName { get { return destPortGroupName; } set { destPortGroupName = value; } }
        protected internal string DestMachineName { get { return destMachineName; } set { destMachineName = value; } }
        protected internal string DestUnitName { get { return destUnitName; } set { destUnitName = value; } }
        protected internal string DestContainerName { get { return destContainerName; } set { destContainerName = value; } }
        protected internal string DestSlotNumber { get { return destSlotNumber; } set { destSlotNumber = value; } }
        protected internal string FromUnitName { get { return fromUnitName; } set { fromUnitName = value; } }
        protected internal string ToUnitName { get { return toUnitName; } set { toUnitName = value; } }
        protected internal string Priority { get { return priority; } set { priority = value; } }
        protected internal string DestType { get { return destType; } set { destType = value; } }
        protected internal string ToType { get { return toType; } set {toType = value; } }
        protected internal string CarrierKind { get { return carrierKind; } set { carrierKind = value; } }
        public Machine SourceMachine { get { return sourceMachine; } set { sourceMachine = value; } }
        public Machine DestMachine { get { return destMachine; } set { destMachine = value; } }
        public Unit SourceUnit { get { return sourceUnit; } set { sourceUnit = value; } }
        public string SourceContainerName { get { return sourceContainerName; } set { sourceContainerName = value; } }
        public string SourceSlotNumber { get { return sourceSlotNumber; } set { sourceSlotNumber = value; } }
        public Unit DestUnit { get { return destUnit; } set { destUnit = value; } }
        public Zone DestZone { get { return destZone; } set { destZone = value; } }
        public Machine FromMachine { get { return fromMachine; } set { fromMachine = value; } }
        public Machine ToMachine { get { return toMachine; } set { toMachine = value; } }
        public Unit FromUnit { get { return fromUnit; } set { fromUnit = value; } }
        public Unit ToUnit { get { return toUnit; } set { toUnit = value; } }
        public Zone ToZone { get { return toZone; } set { toZone = value; } }
        public TransferState TransferState { get { return transferState; } set { transferState = value; } }
        protected internal int MaxHoppingCount { get { return maxHoppingCount; } set { maxHoppingCount = value; } }
        protected internal int MaxTotalCost { get { return maxTotalCost; } set { maxTotalCost = value; } }
        protected internal int MaxTotalWeight { get { return MaxTotalWeight; } set { MaxTotalWeight = value; } }
        //protected internal StageCommand StageCommand { get => stageCommand; set => stageCommand = value; }
        //protected internal StageDeleteCommand StageDeleteCommand { get => stageDeleteCommand; set => stageDeleteCommand = value; }
        //protected internal ScanCommand ScanCommand { get => scanCommand; set => scanCommand = value; }
        //protected internal MaterialCommand MaterialCommand { get => materialCommand; set => materialCommand = value; }
        //protected internal ResourceCommand ResourceCommand { get => resourceCommand; set => resourceCommand = value; }
        public bool UseInternalRoute { get { return UseInternalRoute; } set { UseInternalRoute = value; } }
        public string Description { get { return description; } set { description = value; } }
        public string TroubleMachineName { get { return troubleMachineName; } set { troubleMachineName = value; } }

        public TransferMessage()
        {
            this.transferState = new TransferState();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transferMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", destMachineName=").Append(this.destMachineName);
            sb.Append(", destType=").Append(this.destType);
            sb.Append(", destUnitName=").Append(this.destUnitName);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", fromUnitName=").Append(this.fromUnitName);
            sb.Append(", toUnitName=").Append(this.toUnitName);
            sb.Append(", toType=").Append(this.toType);
            sb.Append(", troubleMachineName=").Append(this.troubleMachineName);
            sb.Append("}");

            return sb.ToString();
        }

    }
}
