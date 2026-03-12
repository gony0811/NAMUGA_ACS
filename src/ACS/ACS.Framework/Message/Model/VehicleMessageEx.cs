using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model;
using ACS.Framework.Path.Model;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;

namespace ACS.Framework.Message.Model
{
    public class VehicleMessageEx : AbstractMessage
    {
        public static string COMMAND_CODE_C = "C";
        public static string COMMAND_CODE_S = "S";
        public static string COMMAND_CODE_T = "T";
        public static string COMMAND_CODE_E = "E";
        public static string COMMAND_CODE_R = "R";
        public static string COMMAND_CODE_L = "L";
        public static string COMMAND_CODE_U = "U";
        public static string COMMAND_CODE_M = "M";
        public static string COMMAND_CODE_H = "H";
        public static string COMMAND_CODE_O = "O";
        public static string MESSAGE_TYPE_VEHICLEDEPARTED = "VEHICLEDEPARTED";
        public static string MESSAGE_TYPE_ACQUIRECOMPLETED = "ACQUIRECOMPLETED";
        public static string MESSAGE_TYPE_TRANSFERCOMPLETED = "TRANSFERCOMPLETED";
        public static string RUNSTATE_RUN = "RUN";
        public static string RUNSTATE_STOP = "STOP";
        public static string FULLSTATE_FULL = "FULL";
        public static string FULLSTATE_EMPTY = "EMPTY";
        public static string TRUE = "T";
        public static string FALSE = "F";
        public static string T_CODE_TYPE_REPORT = "0";
        public static string T_CODE_TYPE_COMMAND = "1";
        public static string T_CODE_TYPE_REPLY = "3";
        public static string C_CODE_TYPE_LEFTLOAD = "01";
        public static string C_CODE_TYPE_LEFTUNLOAD = "02";
        //KSB
        public static string C_CODE_TYPE_RIGHTLOAD = "03";
        public static string C_CODE_TYPE_RIGHTUNLOAD = "04";
        public static string C_CODE_TYPE_MANUAL = "05";
        //KSB
        public static string C_CODE_TYPE_LEFTLOAD_TURN = "06";
        public static string C_CODE_TYPE_LEFTUNLOAD_TURN = "07";
        public static string C_CODE_TYPE_RIGHTLOAD_TURN = "08";
        public static string C_CODE_TYPE_RIGHTUNLOAD_TURN = "09";

        public static string ALARM_SEVERITY_LIGHT = "LIGHT";
        public static string ALARM_SEVERITY_HEAVY = "HEAVY";

        private string vehicleId;
        private string nodeId;
        private string stationId;
        private string occupied;
        private string runState;
        private string tCodeType;
        private string fullState;
        private string cCodeType;
        private string rCodeType;
        private string mCodeType;
        private string transferState;
        private string transportCommandId;
        private string carrierId;
        private string commandId;
        private string destPortId;
        private string destNodeId;
        private string priority;
        private string carrierType;
        private string resultCode;
        private string errorCode;
        private string errorId;
        private string errorText;
        private string errorLevel;
        private float batteryVoltage;
        private int batteryRate;
        private string keyData;
        private VehicleEx vehicle;
        private NodeEx node;
        private TransportCommandEx transportCommand;
        private string bayId;
        private IList vehicles;
        private IList currentPath;

        public string VehicleId { get{ return vehicleId; } set { vehicleId = value; } }
        public string NodeId { get{ return nodeId; } set { nodeId = value; } }
        public string StationId { get{ return stationId; } set { stationId = value; } }
        public string Occupied { get{ return occupied; } set { occupied = value; } }
        public string RunState { get{ return runState; } set { runState = value; } }
        public string TCodeType { get{ return tCodeType; } set { tCodeType = value; } }
        public string FullState { get{ return fullState; } set { fullState = value; } }
        public string CCodeType { get { return cCodeType; } set { cCodeType = value; } }
        public string RCodeType { get{ return rCodeType; } set { rCodeType = value; } }
        public string MCodeType { get{ return mCodeType; } set { mCodeType = value; } }
        public string TransferState { get{ return transferState; } set { transferState = value; } }
        public string TransportCommandId { get{ return transportCommandId; } set { transportCommandId = value; } }
        public string Cause { get { return base.Cause; } set { base.Cause = value; } }
        public object ReceivedMessage { get { return base.ReceivedMessage; } set { base.ReceivedMessage = value; } }
        public string CarrierId { get{ return carrierId; } set { carrierId = value; } }
        public string CommandId { get{ return commandId; } set { commandId = value; } }
        public string DestPortId { get{ return destPortId; } set { destPortId = value; } }
        public string DestNodeId { get{ return destNodeId; } set { destNodeId = value; } }
        public string Priority { get{ return priority; } set { priority = value; } }
        public string CarrierType { get{ return carrierId; } set { carrierId = value; } }
        public string ResultCode { get{ return resultCode; } set { resultCode = value; } }
        public string ErrorCode { get{ return errorCode; } set { errorCode = value; } }
        public string ErrorId { get{ return errorId; } set { errorId = value; } }
        public string ErrorText { get{ return errorText; } set { errorText = value; } }
        public string ErrorLevel { get{ return errorLevel; } set { errorLevel = value; } }
        public float BatteryVoltage { get{ return batteryVoltage; } set { batteryVoltage = value; } }
        public int BatteryRate { get{ return batteryRate; } set { batteryRate = value; } }
        public string KeyData { get{ return keyData; } set { keyData = value; } }
        public VehicleEx Vehicle { get{ return vehicle; } set { vehicle = value; } }
        public NodeEx Node { get{ return node; } set { node = value; } }
        public TransportCommandEx TransportCommand { get{ return transportCommand; } set { transportCommand = value; } }
        public string BayId { get{ return bayId; } set { bayId = value; } }
        public IList Vehicles { get { return vehicles; } set { vehicles = value; } }

        public IList CurrentPath { get { return currentPath; } set { currentPath = value; } }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicleMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", vehicleId=").Append(this.vehicleId);
            sb.Append(", nodeId=").Append(this.nodeId);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", runState=").Append(this.runState);
            sb.Append(", transferState=").Append(this.transferState);
            sb.Append(", transportCommandId=").Append(this.transportCommandId);
            sb.Append(", carrierId=").Append(this.carrierId);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", bayId=").Append(this.bayId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
