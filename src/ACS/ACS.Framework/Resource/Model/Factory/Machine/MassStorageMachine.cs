using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model.Factory.Machine
{
    public class MassStorageMachine : StorageMachine
    {
        public static string IDREADDUPLICATEOPTION_REJECT = "0";
        public static string IDREADDUPLICATEOPTION_HOSTCONTROLLED = "1";
        public static string IDREADFAILUREOPTION_REJECT = "0";
        public static string IDREADFAILUREOPTION_HOSTCONTROLLED = "1";
        public static string IDREADMISMATCHOPTION_REJECT = "0";
        public static string IDREADMISMATCHOPTION_HOSTCONTROLLED = "1";
        private string combined = "F";
        private string idReadDuplicateOption = "1";
        private string idReadFailureOption = "1";
        private string idReadMismatchOption = "1";
        private string autoInBanned = "F";
        private string autoOutBanned = "F";
        private string storedBanned = "F";
        private string previousStoredBanned = "F";
        private string massStockOut = "F";

        public string Combined
        {
            get { return combined; }
            set { combined = value; }
        }

        public string IdReadDuplicationOption
        {
            get { return idReadDuplicateOption; }
            set { idReadDuplicateOption = value; }
        }

        public string IdReadFailureOption
        {
            get { return idReadFailureOption; }
            set { idReadFailureOption = value; }
        }

        public string IdReadMismatchOption
        {
            get { return idReadMismatchOption; }
            set { idReadMismatchOption = value; }
        }

        public string AutoInBanned
        {
            get { return autoInBanned; }
            set { autoInBanned = value; }
        }

        public string AutoOutBanned
        {
            get { return autoOutBanned; }
            set { autoOutBanned = value; }
        }

        public string StoredBanned
        {
            get { return storedBanned; }
            set { storedBanned = value; }
        }

        public string PreviousStoredBanned
        {
            get { return previousStoredBanned; }
            set { previousStoredBanned = value; }
        }

        public string MassStockOut
        {
            get { return massStockOut; }
            set { massStockOut = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("massStorageMachine{");
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
            sb.Append(", fullUp=").Append(this.fullUp);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", highWaterMark=").Append(this.highWaterMark);
            sb.Append(", lowWaterMark=").Append(this.lowWaterMark);
            sb.Append(", maxCommandPoolSize=").Append(this.maxCommandPoolSize);
            sb.Append(", combined=").Append(this.combined);
            sb.Append(", idReadDuplicateOption=").Append(this.idReadDuplicateOption);
            sb.Append(", idReadFailureOption=").Append(this.idReadFailureOption);
            sb.Append(", idReadMismatchOption=").Append(this.idReadMismatchOption);
            sb.Append(", autoInBanned=").Append(this.autoInBanned);
            sb.Append(", autoOutBanned=").Append(this.autoOutBanned);
            sb.Append(", massStockOut=").Append(this.massStockOut);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
