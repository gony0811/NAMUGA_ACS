using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Unit
{
    public class Vehicle : Unit
    {
        public static string STATE_INSTALLED = "INSTALLED";
        public static string STATE_REMOVED = "REMOVED";
        public static string STATE_CHARGING = "CHARGING";
        public static string STATE_CHARGED = "CHARGED";
        public static string SUBSTATE_ASSIGNED = "ASSIGNED";
        public static string SUBSTATE_ASSIGNED_ENROUTE = "ASSIGNED_ENROUTE";
        public static string SUBSTATE_ASSIGNED_PARKED = "ASSIGNED_PARKED";
        public static string SUBSTATE_ASSIGNED_ACQUIRING = "ASSIGNED_ACQUIRING";
        public static string SUBSTATE_ASSIGNED_DEPOSITING = "ASSIGNED_DEPOSITING";
        public static string SUBSTATE_NOTASSIGNED = "NOTASSIGNED";

        private string railPosition = "0";
        private string idReadState = "SUCCESS";
        private DateTime idReadTime = new DateTime();

        public string RailPosition
        {
            get { return railPosition; }
            set { railPosition = value; }
        }

        public string IdReadState
        {
            get { return idReadState; }
            set { idReadState = value; }
        }

        public DateTime IdReadTime
        {
            get { return idReadTime; }
            set { idReadTime = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicle{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(Name);
            sb.Append(", machineName=").Append(this.machineName);

            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", subState=").Append(this.subState);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", lastTransferTime=").Append(this.lastTransferTime);

            sb.Append(", railPosition=").Append(this.railPosition);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
