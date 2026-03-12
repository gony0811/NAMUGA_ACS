using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Stage
{
    public class StagePoint : Entity
    {
        public static string STAGE_POINT_TRANSFERINITIATED = "TransferInitiated";
        public static string STAGE_POINT_CARRIERTRANSFERRING = "CarrierTransferring";
        public static string STAGE_POINT_CARRIERWAITOUT_OP = "CarrierWaitOutOnOP";
        public static string STAGE_POINT_CARRIERWAITOUT_BP = "CarrierWaitOutOnBP";
        private string transportMachineName = "";
        private string toUnitName = "";
        private string used = "T";
        private string stagePointName = "";

        public string TransportMachineName { get { return transportMachineName; } set { transportMachineName = value; } }
        public string ToUnitName { get { return toUnitName; } set { toUnitName = value; } }
        public string Used { get { return used; } set { used = value; } }
        public string StagePointName { get { return stagePointName; } set { stagePointName = value; } }
    }
}
