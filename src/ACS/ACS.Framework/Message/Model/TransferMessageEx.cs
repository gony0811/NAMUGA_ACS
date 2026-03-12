using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model;
using ACS.Framework.Path.Model;
using ACS.Framework.Material.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Resource.Model;

namespace ACS.Framework.Message.Model
{
    public class TransferMessageEx : AbstractMessage
    {
        public static string CHECKRESULT_SUCCESS = "SUCCESS";
        public static string UI_NG_MESSAGE_VALIDATION = "This TransportCommand Message is Wrong";
        public static string UI_NG_STATION_VALIDATION = "This Source, Dest port is Worng";
        public static string UI_NG_JOB_EXIST = "This TRANSPORTJOB Alreadt Exist";
        public static string UI_NG_CARRIER_CREATE = "Fail to Create CARRIER";
        public static string UI_NG_JOB_CREATE = "Fail to Create TRANSPORTJOB";
        public static string UI_NG_PATH_VALIDATION = "This Path is wrong";
        public static string UI_NG_VEHICLE_VALIDATION = "Can not find available VEHICLE, This TRANSPORTJOB is Queued";
        private string transportCommandId;
        private string sourceMachine;
        private string sourceUnit;
        private string destMachine;
        private string destUnit;
        private string carrierId;
        private string carrierType;
        private int priority = 3;
        private string vehicleId;
        private VehicleEx vehicle;
        private CarrierEx carrier;
        private TransportCommandEx transportCommand;
        private PathInfoEx pathInfo;
        private string bayId;
        private string eqpId;
        private string portId;
        private string agvName;
        private string jobType;
        private string batchId;
        private string midLoc;
        private string midPortId;
        private string originLoc;
        private string description;
        private string destNodeId;
        private string userId;

        //181001
        private string processId;
        private string productId;

        //181020
        private string stepId;

        public string EqpId { get {return eqpId;} set { eqpId = value; } }
        public string PortId { get {return portId;} set { portId = value; } }
        public string AgvName { get {return agvName;} set { agvName = value; } }
        public string JobType { get {return jobType;} set { jobType = value; } }
        public string MidLoc { get {return midLoc;} set { midLoc = value; } }
        public string MidPortId { get {return midPortId;} set { midPortId = value; } }
        public string OriginLoc { get {return originLoc;} set { originLoc = value; } }
        public string Description { get {return description;} set {description = value; } }
        public string DestNodeId { get {return destNodeId;} set {destNodeId = value; } }
        public string TransportCommandId { get {return transportCommandId;} set { transportCommandId = value; } }
        public string Cause { get { return base.Cause; } set { base.Cause = value; } }
        public object ReceivedMessage { get { return base.ReceivedMessage; } set { base.ReceivedMessage = value; } }
        public string SourceMachine { get {return sourceMachine;} set { sourceMachine = value; } }
        public string SourceUnit { get {return sourceUnit;} set { sourceUnit = value; } }
        public string DestMachine { get {return destMachine;} set {destMachine = value; } }
        public string DestUnit { get {return destUnit;} set {destUnit = value; } }
        public string CarrierId { get {return carrierId;} set {carrierId = value; } }

        public string BatchID { get { return batchId; } set { batchId = value; } }
        public string CarrierType { get {return carrierType;} set {carrierType = value; } }
        public int Priority { get {return priority;} set {priority = value; } }
        public string VehicleId { get {return vehicleId; } set {vehicleId = value; } }
        public VehicleEx Vehicle { get { return vehicle; } set { vehicle = value; } }
        public CarrierEx Carrier { get{ return carrier; } set { carrier = value; } }
        public TransportCommandEx TransportCommand { get { return transportCommand; } set { transportCommand = value; } }
        public PathInfoEx PathInfo { get{ return pathInfo; } set { pathInfo = value; } }
        public string BayId { get{ return bayId; } set { bayId = value; } }
        public string UserId { get { return userId; } set { userId = value; } }
        public string CarrierName { get; set; }
        public string ReplyCode { get; set; }

        //181001
        public string ProcessId { get { return processId; } set { processId = value; } }
        public string ProductId { get { return productId; } set { productId = value; } }
        //181020
        public string StepId { get { return stepId; } set { stepId = value;} } 

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transferMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierId=").Append(this.carrierId);
            sb.Append(", carrierType=").Append(this.carrierType);
            sb.Append(", transportCommandId=").Append(this.transportCommandId);
            sb.Append(", vehicleId=").Append(this.vehicleId);
            sb.Append(", sourceMachine=").Append(this.sourceMachine);
            sb.Append(", sourceUnit=").Append(this.sourceUnit);
            sb.Append(", destMachine=").Append(this.destMachine);
            sb.Append(", destUnit=").Append(this.destUnit);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", eqpId=").Append(this.eqpId);
            sb.Append(", portId=").Append(this.portId);
            sb.Append(", agvName=").Append(this.agvName);
            sb.Append(", jobType=").Append(this.jobType);
            sb.Append(", midLoc=").Append(this.midLoc);
            sb.Append(", midPortId=").Append(this.midPortId);
            sb.Append(", userId=").Append(this.userId);
            sb.Append(", originLoc=").Append(this.originLoc);
            sb.Append(", description=").Append(this.description);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
