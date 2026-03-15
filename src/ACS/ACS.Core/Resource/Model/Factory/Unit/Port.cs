using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Unit
{
    public class Port : ZoneUnit 
    {
        public static string INOUT_TYPE_IN = "IN";
        public static string INOUT_TYPE_OUT = "OUT";
        public static string INOUT_TYPE_BOTH = "BOTH";
        public static string PORT_TYPE_LP = "LP";
        public static string PORT_TYPE_OP = "OP";
        public static string PORT_TYPE_BP = "BP";
        public static string SUBSTATE_READYTOLOAD = "READYTOLOAD";
        public static string SUBSTATE_READYTOUNLOAD = "READYTOUNLOAD";
        public static string SUBSTATE_TRANSFERBLOCKED = "TRANSFERBLOCKED";
        public static string SUBSTATE_READY = "READY";
        public static string TRANSFERSTATE_INSERVICE = "INSERVICE";
        public static string TRANSFERSTATE_OUTOFSERVICE = "OUTOFSERVICE";
        public static string ACCESSMODE_MANUAL = "MANUAL";
        public static string ACCESSMODE_AUTO = "AUTO";
        public static string NAME_INOUTTYPE = "INOUTTYPE";
        public static string NAME_ACCESSMODE = "ACCESSMODE";
        public static string NAME_TRANSFERSTATE = "TRANSFERSTATE";
        public static string NAME_USERFREADER = "USERFREADER";

        private string inOutType = "IN";
        private string portType = "LP";
        private string manual = "F";
        private string accessMode = "AUTO";
        private string transferState = "INSERVICE";
        private string autoRemovable = "T";
        private string removable = "T";
        private string deleteCarrier = "F";
        private string combined = "F";
        private string forcedTransfer = "F";
        private string useRfReader = "T";
        private string multiLoading = "T";
        private string storable = "T";
        private string reportLocation = "T";
        private int positionX;
        private int positionY;
        private int positionZ;
        private string idReadState = "SUCCESS";
        private DateTime idReadTime = new DateTime();

        public Port()
        {
            this.subState = "READY";
        }

        public string PortType
        {
            get { return portType; }
            set { portType = value; }
        }
        
        public string DeleteCarrier
        {
            get { return deleteCarrier; }
            set { deleteCarrier = value; }
        }

        public string ForcedTransfer
        {
            get { return forcedTransfer; }
            set { forcedTransfer = value; }
        }

        public string Combined
        {
            get { return combined; }
            set { combined = value; }
        }

        public string MultiLoading
        {
            get { return multiLoading; }
            set { multiLoading = value; }
        }

        public string Removable
        {
            get { return removable; }
            set { removable = value; }
        }

        public string UseRfReader
        {
            get { return useRfReader; }
            set { useRfReader = value; }
        }

        public string Manual
        {
            get { return manual; }
            set { manual = value; }
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

        public string AccessMode
        {
            get { return accessMode; }
            set { accessMode = value; }
        }

        public string InOutType
        {
            get { return inOutType; }
            set { inOutType = value; }
        }

        public string AutoRemovable
        {
            get { return autoRemovable; }
            set { autoRemovable = value; }
        }

        public int PositionX
        {
            get { return positionX; }
            set { positionX = value; }
        }

        public int PositionY
        {
            get { return positionY; }
            set { positionY = value; }
        }

        public int PositionZ
        {
            get { return positionZ; }
            set { positionZ = value; }
        }

        public string Storable
        {
            get { return storable; }
            set { storable = value; }
        }

        public string TransferState
        {
            get { return transferState; }
            set { transferState = value; }
        }

        public string ReportLocation
        {
            get { return reportLocation; }
            set { reportLocation = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("port{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(base.Name);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", subState=").Append(this.subState);
            sb.Append(", transferState=").Append(this.transferState);

            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", permittedCapacity=").Append(this.permittedCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", lastTransferTime=").Append(this.lastTransferTime);

            sb.Append(", zoneName=").Append(this.zoneName);

            sb.Append(", autoRemovable=").Append(this.autoRemovable);
            sb.Append(", removable=").Append(this.removable);
            sb.Append(", deleteCarrier=").Append(this.deleteCarrier);
            sb.Append(", portType=").Append(this.portType);
            sb.Append(", manual=").Append(this.manual);
            sb.Append(", accessMode=").Append(this.accessMode);

            sb.Append(", inOutType=").Append(this.inOutType);

            sb.Append(", multiLoading=").Append(this.multiLoading);
            sb.Append(", combined=").Append(this.combined);
            sb.Append(", useRfReader=").Append(this.useRfReader);
            sb.Append(", storable=").Append(this.storable);
            sb.Append(", reportLocation=").Append(this.reportLocation);
            sb.Append(", idReadState=").Append(this.idReadState);
            sb.Append(", idReadTime=").Append(this.idReadTime);

            sb.Append("}");
            return sb.ToString();
        }
    }
}
