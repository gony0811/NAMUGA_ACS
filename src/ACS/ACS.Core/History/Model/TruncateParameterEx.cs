using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.History.Model
{
    public class TruncateParameterEx : TimedEntity
    {
        public virtual string TableName { get; set; }
        public virtual string NativeSql { get; set; }
        public virtual string PartitioningBase { get; set; }
        public virtual int SavePeriod { get; set; }
        public virtual string TruncateSingleOrMulti { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("truncateParameter{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", tableName=").Append(this.TableName);
            sb.Append(", nativeSql=").Append(this.NativeSql);
            sb.Append(", partitioningBase=").Append(this.PartitioningBase);
            sb.Append(", savePeriod=").Append(this.SavePeriod);
            sb.Append(", truncateSingleOrMulti=").Append(this.TruncateSingleOrMulti);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
