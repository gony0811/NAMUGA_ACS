using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Material.Model
{
    public class Substrate : Carrier
    {
        private string carrierName;
        private string slotNumber;

        protected string CarrierName { get { return carrierName; } set { carrierName =value; } }
        protected string SlotNumber { get { return slotNumber; } set { slotNumber = value; } }

        public Substrate()
        {
            this.Kind = "SUBSTRATE";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("substrate{");
            sb.Append("name=").Append(this.Name);
            sb.Append(", machineName=").Append(this.MachineName);
            sb.Append(", unitName=").Append(this.UnitName);
            sb.Append(", carrierName=").Append(this.carrierName);
            sb.Append(", slotNumber=").Append(this.slotNumber);
            sb.Append(", type=").Append(this.Type);
            sb.Append(", state=").Append(this.State);
            sb.Append(", reserved=").Append(this.Reserved);
            sb.Append(", holded=").Append(this.Holded);
            sb.Append(", installTime=").Append(this.InstallTime);
            sb.Append(", storedTime=").Append(this.StoredTime);
            sb.Append(", idReadTime=").Append(this.IdReadTime);
            sb.Append(", lotId=").Append(this.LotId);
            sb.Append(", batchId=").Append(this.BatchId);
            sb.Append(", stepId=").Append(this.StepId);
            sb.Append(", processId=").Append(this.ProcessId);
            sb.Append(", additionalInfo=").Append(this.AdditionalInfo);
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
