using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Factory.Machine
{
    public class RailStorageMachine : StorageMachine
    {
        public static string IDREADTYPE_INVENTORY = "INVENTORY";
        public static string IDREADTYPE_RFC = "RFC";
        public static string IDREADTYPE_TAGREADER = "TAGREADER";
        public static string IDREADTYPE_NONE = "NONE";
        private string idReadType = "RFC";

        public string IdReadType
        {
            get { return idReadType; }
            set { idReadType = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("railStorageMachine{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
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
            sb.Append(", fullUp=").Append(this.fullUp);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", highWaterMark=").Append(this.highWaterMark);
            sb.Append(", lowWaterMark=").Append(this.lowWaterMark);
            sb.Append(", maxCommandPoolSize=").Append(this.maxCommandPoolSize);
            sb.Append(", maxCommandRetryCount=").Append(this.maxCommandRetryCount);
            sb.Append(", heavyAlarmCount=").Append(this.heavyAlarmCount);
            sb.Append(", lightAlarmCount=").Append(this.lightAlarmCount);

            sb.Append(", idReadType=").Append(this.idReadType);

            sb.Append("}");
            return sb.ToString();
        }
    }
}
