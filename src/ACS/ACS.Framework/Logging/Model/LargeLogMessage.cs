using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Logging.Model
{
    public class LargeLogMessage : PartitionedEntity
    {
        public virtual string LogMessageId { get; set; }
        public virtual string LargeText { get; set; }
        public virtual int Sequence { get; set; }

        public LargeLogMessage()
        {
            this.PartitionId = base.CreatePartitionIdByDate();
        }

        public LargeLogMessage(int partitionId)
        {
            this.PartitionId = partitionId;
        }

        public virtual int GetPartitionId()
        {
            return base.PartitionId;
        }
    }
}
