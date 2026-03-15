using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Factory
{
    public class Module : NamedEntity
    {
        public static string STATE_INSERVICE = "INSERVICE";
        public static string STATE_OUTOFSERVICE = "OUTOFSERVICE";
        public static string STATE_PREVENTIVEMAINTENANCE = "PM";
        public static string PROCESSINGSTATE_IDLE = "IDLE";
        public static string PROCESSINGSTATE_PROCESSING = "PROCESSING";
        public static string PROCESSINGSTATE_DOWN = "DOWN";
        public static string ACCEPTABLECARRIERTYPE_TFT = "TFT";
        public static string ACCEPTABLECARRIERTYPE_CF = "CF";
        public static string ACCEPTABLECARRIERTYPE_CELL = "CELL";
        public static string ACCEPTABLECARRIERTYPE_MODULE = "MODULE";
        public static string ACCEPTABLECARRIERTYPE_TFT_CF = "TFT-CF";
        public static string ACCEPTABLECARRIERTYPE_TFT_CELL = "TFT-CELL";
        public static string ACCEPTABLECARRIERTYPE_TFT_MODULE = "TFT-MODULE";
        public static string ACCEPTABLECARRIERTYPE_CF_CELL = "CF-CELL";
        public static string ACCEPTABLECARRIERTYPE_CF_MODULE = "CF-MODULE";
        public static string ACCEPTABLECARRIERTYPE_CELL_MODULE = "CELL-MODULE";
        public static string ACCEPTABLECARRIERTYPE_TFT_CF_CELL = "TFT-CF-CELL";
        public static string ACCEPTABLECARRIERTYPE_TFT_CF_MODULE = "TFT-CF-MODULE";
        public static string ACCEPTABLECARRIERTYPE_CF_CELL_MODULE = "CF-CELL-MODULE";
        public static string ACCEPTABLECARRIERTYPE_ALL = "ALL";
        public static string NAME_STATE = "STATE";
        public static string NAME_PROCESSINGSTATE = "PROCESSINGSTATE";

        protected string type = "";
        protected string state = "INSERVICE";
        protected string processingState = "PROCESSING";
        protected string acceptableCarrierType = "ALL";
        protected string acceptableSubstrate = "F";

        public string State
        {
            get { return state; }
            set { state = value; }
        }

        public string ProcessingState
        {
            get { return processingState; }
            set { processingState = value; }
        }

        public string AcceptableCarrierType
        {
            get { return acceptableCarrierType; }
            set { acceptableCarrierType = value; }
        }

        public string AcceptableSubstrate
        {
            get { return acceptableSubstrate; }
            set { acceptableSubstrate = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }
    }
}
