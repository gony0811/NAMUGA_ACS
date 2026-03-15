using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Unit
{
    public class Unit : Module
    {
        public static string TYPE_SHELF = "SHELF";
        public static string TYPE_PORT = "PORT";
        public static string TYPE_CRANE = "CRANE";
        public static string TYPE_VEHICLE = "VEHICLE";
        public static string TYPE_SHUTTLE = "SHUTTLE";
        public static string TYPE_PNP = "PNP";
        public static string NAME_IDREADSTATE = "IDREADSTATE";
        public static string NAME_SUBSTATE = "SUBSTATE";
        public static string IDREADSTATE_SUCCESS = "SUCCESS";
        public static string IDREADSTATE_FAILURE = "FAILURE";
        public static string IDREADSTATE_DUPLICATE = "DUPLICATE";
        public static string IDREADSTATE_MISMATCH = "MISMATCH";

        protected string machineName = "";
        protected int maxCapacity = 1;
        protected int currentCapacity = 0;
        protected int permittedCapacity = 1;
        protected DateTime lastTransferTime = new DateTime();
        protected string occupied = "F";
        protected string reserved = "F";
        protected string banned = "F";
        protected string subState = "NA";
        protected string additionalInfo = "";
        protected string floorName = "0";

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public int MaxCapacity
        {
            get { return maxCapacity; }
            set { maxCapacity = value; }
        }

        public int CurrentCapacity
        {
            get { return currentCapacity; }
            set { currentCapacity = value; }
        }

        public int PermittedCapacity
        {
            get { return permittedCapacity; }
            set { permittedCapacity = value; }
        }

        public DateTime LastTransferTime
        {
            get { return lastTransferTime; }
            set { lastTransferTime = value; }
        }

        public string Occupied
        {
            get { return occupied; }
            set { occupied = value; }
        }

        public string Reserved
        {
            get { return reserved; }
            set { reserved = value; }
        }

        public string Banned
        {
            get { return banned; }
            set { banned = value; }
        }

        public string SubState
        {
            get { return subState; }
            set { subState = value; }
        }

        public string AdditionalInfo
        {
            get { return additionalInfo; }
            set { additionalInfo = value; }
        }

        public string FloorName
        {
            get { return floorName; }
            set { floorName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("unit{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(Name);
            sb.Append(", machineName=").Append(this.machineName);

            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", subState=").Append(this.subState);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", permittedCapacity=").Append(this.permittedCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", floorName=").Append(this.floorName);
            sb.Append(", lastTransferTime=").Append(this.lastTransferTime);
            sb.Append(", additionalInfo=").Append(this.additionalInfo);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
