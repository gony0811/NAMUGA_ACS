using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Position
{
    public class VehicleStage : MultiPosition
    {
        private string vehicleName = "";
        private string machineName = "";

        public string VehicleName
        {
            get { return vehicleName; }
            set { vehicleName = value; }
        }

        public string MachineName
        {
            get { return MachineName; }
            set { MachineName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicleStage{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", description=").Append(this.Description);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", editor=").Append(this.Editor);

            sb.Append(", vehicleName=").Append(this.vehicleName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", carrierName=").Append(this.carrierName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
