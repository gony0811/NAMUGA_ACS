using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Machine
{
    public class TransportMachine : Machine
    {
        public static int MAX_CAPACITY_DEFAULT = 100;
        public static int UNLIMITED_VALUE = -1;
        public static string CONTROLLERTRANSPORTERTYPE_CONTROLLER = "CONTROLLER";
        public static string CONTROLLERTRANSPORTERTYPE_TRANSPORTER = "TRANSPORTER";
        public static string CONTROLLERTRANSPORTERTYPE_BOTH = "BOTH";
        protected string reconciling = "F";
        protected int maxCapacity = -1;
        protected int currentCapacity = 0;
        protected int maxCommandPoolSize = -1;
        protected int maxCommandRetryCount = -1;
        protected int heavyAlarmCount = 0;
        protected int lightAlarmCount = 0;
        protected string controllerTransporterType = "BOTH";
        protected string controllerMachineName;

        public string Recociling
        {
            get { return reconciling; }
            set { reconciling = value; }
        }

        public int MaxCapacity
        {
            get { return maxCapacity; }
            set { maxCapacity = value; }
        }

        public int CurrentCapacity
        {
            get { return currentCapacity; }
            set { currentCapacity = value; }
        }

        public int MaxCommandPoolSize
        {
            get { return maxCommandPoolSize; }
            set { maxCommandPoolSize = value; }
        }

        public int MaxCommandRetryCount
        {
            get { return maxCommandRetryCount; }
            set { maxCommandRetryCount = value; }
        }

        public int HeavyAlarmCount
        {
            get { return heavyAlarmCount; }
            set { heavyAlarmCount = value; }
        }

        public int LightAlarmCount
        {
            get { return lightAlarmCount; }
            set { lightAlarmCount = value; }
        }

        public string ControllerTransporterType
        {
            get { return controllerTransporterType; }
            set { controllerTransporterType = value; }
        }

        public string ControllerMachineName
        {
            get { return controllerMachineName; }
            set { controllerMachineName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transportMachine{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(base.Name);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", kind=").Append(this.kind);
            sb.Append(", connectionState=").Append(this.connectionState);
            sb.Append(", controlState=").Append(this.controlState);
            sb.Append(", tscState=").Append(this.tscState);
            sb.Append(", reconciling=").Append(this.reconciling);
            sb.Append(", controllerName=").Append(this.controllerName);
            sb.Append(", factoryName=").Append(this.factoryName);
            sb.Append(", bayName=").Append(this.bayName);
            sb.Append(", areaName=").Append(this.areaName);
            sb.Append(", floorName=").Append(this.floorName);
            sb.Append(", carrierType=").Append(this.carrierType);

            sb.Append(", handleTransportCommand=").Append(this.handleTransportCommand);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);

            sb.Append(", maxCommandPoolSize=").Append(this.maxCommandPoolSize);
            sb.Append(", maxCommandRetryCount=").Append(this.maxCommandRetryCount);
            sb.Append(", heavyAlarmCount=").Append(this.heavyAlarmCount);
            sb.Append(", lightAlarmCount=").Append(this.lightAlarmCount);
            sb.Append(", controllerTransporterType=").Append(this.controllerTransporterType);
            sb.Append(", controllerMachineName=").Append(this.controllerMachineName);

            sb.Append("}");
            return sb.ToString();
        }

    }
}
