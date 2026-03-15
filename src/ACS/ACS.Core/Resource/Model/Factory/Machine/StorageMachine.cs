using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Machine
{
    public class StorageMachine : TransportMachine
    {
        public static string WATERMARK_DEFAULT_HIGH = "80";
        public static string WATERMARK_DEFAULT_LOW = "60";
        public static string NEAR_FULL_DEFAULT = "70";
        protected string fullUp = "F";
        protected string nearFull = "F";
        protected string highWaterMark = "80";
        protected string lowWaterMark = "60";
        protected string nearFullMark = "70";

        public string FullUp
        {
            get { return fullUp; }
            set { fullUp = value; }
        }

        public string HighWaterMark
        {
            get { return highWaterMark; }
            set { highWaterMark = value; }
        }

        public string LowWaterMark
        {
            get { return lowWaterMark; }
            set { lowWaterMark = value; }
        }

        public string NearFull
        {
            get { return nearFull; }
            set { nearFull = value; }
        }

        public string NearFullMark
        {
            get { return nearFullMark; }
            set { nearFullMark = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("storageMachine{");
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
            sb.Append(", fullUp=").Append(this.fullUp);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", highWaterMark=").Append(this.highWaterMark);
            sb.Append(", lowWaterMark=").Append(this.lowWaterMark);
            sb.Append(", maxCommandPoolSize=").Append(this.maxCommandPoolSize);
            sb.Append(", maxCommandRetryCount=").Append(this.maxCommandRetryCount);
            sb.Append(", heavyAlarmCount=").Append(this.heavyAlarmCount);
            sb.Append(", lightAlarmCount=").Append(this.lightAlarmCount);

            sb.Append("}");
            return sb.ToString();
        }
    }
}
