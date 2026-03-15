using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Material.Model
{
    public class Carrier : NamedEntity
    {
        public static string CARRIER_TYPE_TFT = "TFT";
        public static string CARRIER_TYPE_CF = "CF";
        public static string CARRIER_TYPE_CELL = "CELL";
        public static string CARRIER_TYPE_MODULE = "MODULE";
        public static string CARRIER_TYPE_ALL = "ALL";
        public static string CARRIER_TYPE_AGV = "AGV";
        public static string CARRIER_TYPE_FULL = "FULL";
        public static string CARRIER_TYPE_EMPTY = "EMPTY";
        public static string CARRIER_TYPE_CLEANREQUEST = "CLEAN REQUEST";
        public static string CARRIER_TYPE_ERROR = "ERROR";
        public static string CARRIER_TYPE_SHIPMENTREQUIRMENT = "SHIPMENT REQUIRMENT";
        public static string CARRIER_TYPE_LORDTOLFT = "LORD TO LFT";
        public static string CARRIER_TYPE_FORCEDOUTPUT = "FORCED OUTPUT";

        public static string STATE_ALTERNATING = "ALTERNATING";
        public static string STATE_WAITIN = "WAITIN";
        public static string STATE_WAITOUT = "WAITOUT";
        public static string STATE_TRANSFERRING = "TRANSFERRING";
        public static string STATE_COMPLETED = "COMPLETED";
        public static string STATE_ALTERNATE = "ALTERNATE";
        public static string STATE_NA = "NA";
        public static string STATE_EVICTED = "EVICTED";
        public static string UNKNOWN_TYPE_UNKNOWN = "UNKNOWN";
        public static string UNKNOWN_TYPE_BCR = "BCR";
        public static string UNKNOWN_TYPE_DUP = "DUP";
        public static string UNKNOWN_TYPE_MIS = "MIS";
        public static string UNKNOWN_TYPE_UNK = "UNK";
        public static string UNKNOWN_TYPE_INV = "INV";
        public static string UNKNOWN_TYPE_DBL = "DBL";
        public static string UNKNOWN_TYPE_NORMAL = "NORMAL";
        public static string KIND_CARRIER = "CARRIER";
        public static string KIND_SUBSTRATE = "SUBSTRATE";
        public static string IDREADSTATUS_SUCCESS = "SUCCESS";
        public static string IDREADSTATUS_FAILURE = "FAILURE";
        public static string IDREADSTATUS_DUPLICATE = "DUPLICATE";
        public static string IDREADSTATUS_MISMATCH = "MISMATCH";

        private string machineName = "";
        private string unitName = "";
        private string type = "NA";
        private string kind = "";
        private string state = "INSTALLED";
        private string reserved = "F";
        private string holded = "F";
        private DateTime installTime = new DateTime();
        private DateTime storedTime = new DateTime();
        private DateTime idReadTime = new DateTime();
        private DateTime locationChangedTime = new DateTime();
        private string lotId = "";
        private string batchId = "";
        private string stepId = "";
        private string processId = "";
        private string idReadStatus = "";
        private int duplicatedCount = 0;
        private DateTime lastCleanTime = new DateTime();
        private int usedCount = -1;
        private string productEmpty = "NA";
        private int productQuantity = -1;
        private int maxCapa = -1;
        private DateTime latestJobRequestedTime = new DateTime();
        private DateTime latestCommandCreatedTime = new DateTime();
        private string additionalInfo = "";

        public string MachineName { get { return machineName; } set { machineName = value; } }
        public string UnitName { get { return unitName; } set { unitName = value; } }
        public string Type { get { return type; } set { type = value; } }
        public string Kind { get { return kind; } set { kind = value; } }
        public string State { get { return state; } set { state = value; } }
        public string Reserved { get { return reserved; } set { reserved = value; } }
        public string Holded { get { return holded; } set { holded = value; } }
        public DateTime InstallTime { get { return installTime; } set { installTime = value; } }
        public DateTime StoredTime { get { return storedTime; } set { storedTime = value; } }
        public DateTime IdReadTime { get { return idReadTime; } set { idReadTime = value; } }
        public DateTime LocationChangedTime { get { return locationChangedTime; } set { locationChangedTime = value; } }
        public string LotId { get { return lotId; } set { lotId = value; } }
        public string BatchId { get { return batchId; } set { batchId = value; } }
        public string StepId { get { return stepId; } set { stepId = value; } }
        public string ProcessId { get { return processId; } set { processId = value; } }
        public string IdReadStatus { get { return idReadStatus; } set { idReadStatus = value; } }
        public int DuplicatedCount { get { return duplicatedCount; } set { duplicatedCount = value; } }
        public DateTime LastCleanTime { get { return lastCleanTime; } set { lastCleanTime = value; } }
        public int UsedCount { get { return usedCount; } set { usedCount = value; } }
        public string ProductEmpty { get { return productEmpty; } set { productEmpty = value; } }
        public int ProductQuantity { get { return productQuantity; } set { productQuantity = value; } }
        public int MaxCapa { get { return maxCapa; } set { maxCapa = value; } }
        public DateTime LatestJobRequestedTime { get { return latestJobRequestedTime; } set { latestJobRequestedTime = value; } }
        public DateTime LatestCommandCreatedTime { get { return latestCommandCreatedTime; } set { latestCommandCreatedTime = value; } }
        public string AdditionalInfo { get { return additionalInfo; } set { additionalInfo = value; } }

        public Carrier()
        {
            this.kind = "CARRIER";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("carrier{");
            sb.Append("name=").Append(this.Name);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", unitName=").Append(this.unitName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", holded=").Append(this.holded);
            sb.Append(", installTime=").Append(this.installTime);
            sb.Append(", storedTime=").Append(this.storedTime);
            sb.Append(", idReadTime=").Append(this.idReadTime);
            sb.Append(", locationChangedTime=").Append(this.locationChangedTime);
            sb.Append(", lotId=").Append(this.lotId);
            sb.Append(", batchId=").Append(this.batchId);
            sb.Append(", stepId=").Append(this.stepId);
            sb.Append(", processId=").Append(this.processId);
            sb.Append(", idReadStatus=").Append(this.idReadStatus);
            sb.Append(", duplicatedCount=").Append(this.duplicatedCount);
            sb.Append(", lastCleanTime=").Append(this.lastCleanTime);
            sb.Append(", usedCount=").Append(this.usedCount);
            sb.Append(", productEmpty=").Append(this.productEmpty);
            sb.Append(", productQuantity=").Append(this.productQuantity);
            sb.Append(", maxCapa=").Append(this.maxCapa);
            sb.Append(", additionalInfo=").Append(this.additionalInfo);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", description=").Append(this.Description);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
