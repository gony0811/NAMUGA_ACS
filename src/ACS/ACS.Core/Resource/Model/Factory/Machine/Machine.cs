using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ACS.Core.Resource.Model.Factory.Machine
{
    public class Machine : Module
    {
        public static string CONTROLSTATE_OFFLINE = "OFFLINE";
        public static string CONTROLSTATE_ONLINELOCAL = "ONLINELOCAL";
        public static string CONTROLSTATE_ONLINEREMOTE = "ONLINEREMOTE";
        public static string CONNECTIONSTATE_CONNECTED = "CONNECTED";
        public static string CONNECTIONSTATE_DISCONNECTED = "DISCONNECTED";
        public static string TSCSTATE_INIT = "INIT";
        public static string TSCSTATE_PAUSED = "PAUSED";
        public static string TSCSTATE_PAUSING = "PAUSING";
        public static string TSCSTATE_AUTO = "AUTO";
        public static string TYPE_STOCKER = "STOCKER";
        public static string TYPE_STB = "STB";
        public static string TYPE_OHB = "OHB";
        public static string TYPE_AZFS = "AZFS";
        public static string TYPE_UTS = "UTS";
        public static string TYPE_AGV = "AGV";
        public static string TYPE_MGV = "MGV";
        public static string TYPE_OHT = "OHT";
        public static string TYPE_OHS = "OHS";
        public static string TYPE_BRIDGE = "BRIDGE";
        public static string TYPE_CONVEYOR = "CONVEYOR";
        public static string TYPE_LIFTER = "LIFTER";
        public static string TYPE_TRANSFER = "TRANSFER";
        public static string TYPE_PROCESS = "PROCESS";
        public static string TYPE_RAILBUFFER = "RAILBUFFER";
        public static string TYPE_MIC = "MIC";
        public static string TYPE_MPC = "MPC";
        public static string TYPE_BUFFERSTATION = "BUFFERSTATION";
        public static string TYPE_OPENER = "OPENER";
        public static string TYPE_INTERFABBUFFER = "INTERFABBUFFER";
        public static string KIND_STORAGE = "STORAGE";
        public static string KIND_MASSSTORAGE = "MASSSTORAGE";
        public static string KIND_RAILSTORAGE = "RAILSTORAGE";
        public static string KIND_RAIL = "RAIL";
        public static string KIND_INTERSTORAGE = "INTERSTORAGE";
        public static string KIND_INTERRAIL = "INTERRAIL";
        public static string KIND_PROCESS = "PROCESS";

        protected string kind = "STORAGE";
        protected string controlState = "OFFLINE";
        protected string connectionState = "CONNECTED";
        protected string connectedApplicationName;
        protected DateTime connectionStateChangedTime = new DateTime();
        protected string tscState = "AUTO";
        protected string controllerName = "";
        protected string factoryName = "";
        protected string shopName = "";
        protected string areaName = "";
        protected string bayName = "";
        protected string floorName = "";
        protected string carrierType = "ALL";
        protected string vendor = "";
        protected string handleTransportCommand = "T";
        protected string banned = "F";
        protected string additionalInfo = "";

        public string Vendor
        {
            get { return vendor; }
            set { vendor = value; }
        }

        public string ShopName
        {
            get { return shopName; }
            set { shopName = value; }
        }

        public string AreaName
        {
            get { return areaName; }
            set { areaName = value; }
        }
        public string BayName
        {
            get { return bayName; }
            set { bayName = value; }
        }

        public string CarrierType
        {
            get { return carrierType; }
            set { carrierType = value; }
        }

        public string ControllerName
        {
            get { return controllerName; }
            set { controllerName = value; }
        }

        public string ControlState
        {
            get { return controlState; }
            set { controlState = value; }
        }

        public string TscState
        {
            get { return tscState; }
            set { tscState = value; }
        }

        public string FactoryName
        {
            get { return factoryName; }
            set { factoryName = value; }
        }

        public string FloorName
        {
            get { return floorName; }
            set { floorName = value; }
        }

        public string HandleTransportCommand
        {
            get { return handleTransportCommand; }
            set { handleTransportCommand = value; }
        }

        public string Banned
        {
            get { return banned; }
            set { banned = value; }
        }

        public string Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        public DateTime ConnectionStateChangeTime
        {
            get { return connectionStateChangedTime; }
            set { connectionStateChangedTime = value; }
        }

        public string AdditionalInfo
        {
            get { return additionalInfo; }
            //set { additionalInfo = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("machine{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(base.Name);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", kind=").Append(this.kind);
            sb.Append(", connectionState=").Append(this.connectionState);
            sb.Append(", controlState=").Append(this.controlState);
            sb.Append(", tscState=").Append(this.tscState);
            sb.Append(", controllerName=").Append(this.controllerName);
            sb.Append(", factoryName=").Append(this.factoryName);
            sb.Append(", bayName=").Append(this.bayName);
            sb.Append(", areaName=").Append(this.areaName);
            sb.Append(", floorName=").Append(this.floorName);
            sb.Append(", carrierType=").Append(this.carrierType);
            sb.Append(", handleTransportCommand=").Append(this.handleTransportCommand);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", additionalInfo=").Append(this.additionalInfo);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
