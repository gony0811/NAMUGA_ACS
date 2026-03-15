using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ACS.Core.Base
{
    public class PartitionedEntity : Entity
    {
        private int partitionId;
        private DateTime? time = new DateTime(DateTime.UtcNow.Ticks);
        
        public PartitionedEntity()
        {
            Id = Guid.NewGuid().ToString();
        }


        public virtual int PartitionId {get;set;}

        public virtual DateTime? Time { get; set; }

        public virtual int CreatePartitionIdByMonth()
        {
            DateTime dt = DateTime.Now;
            return dt.Month;
        }

        public virtual int CreatePartitionIdByDate()
        {
            DateTime dt = DateTime.Now;
            return dt.Day;
        }
    }
}
