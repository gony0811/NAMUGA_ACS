using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server.variable
{
    public class EnhancedVehicleVariable
    {
        private string vehicleName;
        private string vehicleState;
        private string vehicleStatus;
        private string vehicleLocation;

        protected internal string VehicleName { get { return vehicleName; } set { vehicleName = value; } }
        protected internal string VehicleState { get { return vehicleState; } set { vehicleState = value; } }
        protected internal string VehicleStatus { get { return vehicleStatus; } set { vehicleStatus = value; } }
        protected internal string VehicleLocation { get { return vehicleLocation; } set { vehicleLocation = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedVehicleVariable{");
            sb.Append("vehicleName=").Append(this.vehicleName);
            sb.Append(", vehicleState=").Append(this.vehicleState);
            sb.Append(", vehicleStatus=").Append(this.vehicleStatus);
            sb.Append(", vehicleLocation=").Append(this.vehicleLocation);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
