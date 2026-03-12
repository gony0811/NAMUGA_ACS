using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server.variable
{
    public class EnhancedCarrierVariable
    {
        private string carrierState = "0";
        private string carrierName;
        private string carrierZoneName;
        private string carrierLoc;
        private string installTime;
        private string vehicleName;
        private string carrierType;

        public string CarrierState { get { return carrierState; } set { carrierState = value; } }
        public string CarrierName { get { return carrierName; } set { carrierName = value; } }
        public string CarrierZoneName { get { return carrierZoneName; } set { carrierZoneName = value; } }
        public string CarrierLoc { get { return carrierLoc; } set { carrierLoc = value; } }
        public string InstallTime { get { return installTime; } set { installTime = value; } }
        public string VehicleName { get { return vehicleName; } set { vehicleName = value; } }
        public string CarrierType { get { return carrierType; } set { carrierType = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enhancedCarrierVariable{");
            sb.Append("carrierName=").Append(this.carrierName);
            sb.Append(", carrierLoc=").Append(this.carrierLoc);
            sb.Append(", installTime=").Append(this.installTime);
            sb.Append(", carrierState=").Append(this.carrierState);
            sb.Append(", carrierZoneName=").Append(this.carrierZoneName);
            sb.Append(", carrierType=").Append(this.carrierType);
            sb.Append(", vehicleName=").Append(this.vehicleName);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
