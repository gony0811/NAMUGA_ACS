using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.History.Model
{
    public class TransactionHistoryEx : AbstractHistoryEx
    {
        private String transactionId;
        private String transactionName;
        private DateTime? startTime;
        private DateTime? endTime;
        private int elapsedTime;
        private int runningBpelProcessCount;
        private String applicationName;
        private String succeeded = "T";
        private String cause;
        private String originatedType;
        public static string PARTITION_TYPE_DAY = "DAY";
        public static string PARTITION_TYPE_MONTH = "MONTH";

        public string TransactionId { get { return transactionId; } set { transactionId = value; } }
        public string TransactionName { get { return transactionName; } set { transactionName = value; } }
        public DateTime? StartTime { get { return startTime; } set { startTime = value; } }
        public DateTime? EndTime { get { return endTime; } set { endTime = value; } }
        public int ElapsedTime { get { return elapsedTime; } set { elapsedTime = value; } }
        public int RunningBpelProcessCount { get { return runningBpelProcessCount; } set { runningBpelProcessCount = value; } }
        public string ApplicationName { get { return applicationName; } set { applicationName = value; } }
        public string Succeeded { get { return succeeded; } set { succeeded = value; } }
        public string Cause { get { return cause; } set { cause = value; } }
        public string OriginatedType { get { return originatedType; } set { originatedType = value; } }

        public TransactionHistoryEx()
        {
            this.PartitionId = base.CreatePartitionIdByDate();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transactionHistory{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", transactionId=").Append(this.transactionId);
            sb.Append(", transactionName=").Append(this.transactionName);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", startTime=").Append(this.startTime);
            sb.Append(", checkTime=").Append(this.endTime);
            sb.Append(", elapsedTime=").Append(this.elapsedTime);
            sb.Append(", runningBpelProcessCount=").Append(this.runningBpelProcessCount);
            sb.Append(", succeeded=").Append(this.succeeded);
            sb.Append(", cause=").Append(this.cause);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
