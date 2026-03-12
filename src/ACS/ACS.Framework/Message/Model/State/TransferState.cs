using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.State
{
    public class TransferState
    {
        //protected internal static readonly Logger logger = Logger.getLogger(typeof(TransferState));
        public const string CARRIERISNOTAVAILABLE = "CARRIERISNOTAVAILABLE";
        public const string DOESNOTNEEDALTERNATETRANSPORTCOMMAND = "DOESNOTNEEDALTERNATETRANSPORTCOMMAND";
        public const string NOTAVAILABLE = "NOTAVAILABLE";
        public const string CURRENTMACHINEPROCESSTYPEISNOTAVAILABLE = "CURRENTMACHINEPROCESSTYPEISNOTAVAILABLE";
        public const string CURRENTUNITPROCESSTYPEISNOTAVAILABLE = "CURRENTUNITPROCESSTYPEISNOTAVAILABLE";
        public const string DESTMACHINEPROCESSTYPEISNOTAVAILABLE = "DESTMACHINEPROCESSTYPEISNOTAVAILABLE";
        public const string DESTUNITPROCESSTYPEISNOTAVAILABLE = "DESTUNITPROCESSTYPEISNOTAVAILABLE";
        public const string DESTZONEPROCESSTYPEISNOTAVAILABLE = "DESTZONEPROCESSTYPEISNOTAVAILABLE";
        public const string ALTERNATEMACHINEPROCESSTYPEISNOTAVAILABLE = "ALTERNATEMACHINEPROCESSTYPEISNOTAVAILABLE";
        public const string CURRENTISNOTAVAILABLE = "CURRENTISNOTAVAILABLE";
        public const string CURRENTMACHINEISNOTAVAILABLE = "CURRENTMACHINEISNOTAVAILABLE";
        public const string CURRENTUNITISNOTAVAILABLE = "CURRENTUNITISNOTAVAILABLE";
        public const string DESTISNOTAVAILABLE = "DESTISNOTAVAILABLE";
        public const string DESTMACHINEISNOTAVILABLE = "DESTMACHINEISNOTAVAILABLE";
        public const string DESTUNITISNOTAVAILABLE = "DESTUNITISNOTAVAILABLE";
        public const string ALTERNATEMACHINEISNOTAVAILABLE = "ALTERNATEMACHINEISNOTAVAILABLE";
        public const string ALTERNATEZONEISNOTAVAILABLE = "ALTERNATEZONEISNOTAVAILABLE";
        public const string ALTERNATEUNITISNOTAVAILABLE = "ALTERNATEUNITISNOTAVAILABLE";
        public const string RECOVERYMACHINEISNOTAVAILABLE = "RECOVERYMACHINEISNOTAVAILABLE";
        public const string DESTUNITISNOTSELETECTED = "DESTUNITISNOTSELETECTED";
        public const string DESTMACHINECAPACITYISOUTOFRANGE = "DESTMACHINECAPACITYISOUTOFRANGE";
        public const string DESTZONECAPACITYISOUTOFRANGE = "DESTZONECAPACITYISOUTOFRANGE";
        public const string ROUTEISNOTAVAILABLE = "ROUTEISNOTAVAILABLE";
        public const string PROCESSTYPENOTMATCHED = "PROCESSTYPENOTMATCHED";
        public const string LINKEDAUTOPORTNOTEXIST = "LINKEDAUTOPORTNOTEXIST";
        public const string SUITABLEAUTOPORTNOTEXIST = "SUITABLEAUTOPORTNOTEXIST";
        public const string SUITABLEBIDIRECTIONALAUTOPORTNOTEXIST = "SUITABLEBIDIRECTIONALAUTOPORTNOTEXIST";
        public const string EXCEEDPORTCAPACITY = "EXCEEDPORTCAPACITY";
        public const string BIDIRECTIONALCOMMANDCAPACITYEXCEED = "BIDIRECTIONALCOMMANDCAPACITYEXCEED";
        public const string ROUTEISNOTSUFFICIENT = "ROUTEISNOTSUFFICIENT";
        /// <summary>
        /// @deprecated
        /// </summary>
        public const string DIRECTIONNOTACCEPTABLE = "DIRECTIONNOTACCEPTABLE";
        public const string CURRENTZONEAUTOOUTBANNED = "CURRENTZONEAUTOOUTBANNED";
        public const string DESTZONEAUTOINBANNED = "DESTZONEAUTOINBANNED";
        private bool needToAlternateTransportCommand = true;
        private bool carrierAvailable = true;
        private bool currentMachineProcessTypeAvailable = true;
        private bool currentUnitProcessTypeAvailable = true;
        private bool destMachineProcessTypeAvailable = true;
        private bool destUnitProcessTypeAvailable = true;
        private bool destZoneProcessTypeAvailable = true;
        private bool alternateMachineProcessTypeAvailable = true;
        private bool currentMachineAvailable = true;
        private bool currentUnitAvailable = true;
        private bool destMachineAvailable = true;
        private bool destUnitAvailable = true;
        private bool alternateMachineAvailable = true;
        private bool alternateZoneAvailable = true;
        private bool alternateUnitAvailable = true;
        private bool recoveryMachineAvailable = true;
        private bool destUnitSelected = true;
        private bool destMachineCapacityInRange = true;
        private bool processTypeMatched = true;
        private bool bestRouteAvailable = true;
        private bool routeExist = true;
        private bool linkedAutoPortExist = true;
        private bool suitableAutoPortExist = true;
        private bool suitableBidirectionalAutoPortExist = true;
        private bool exceedBidirectionalCommandCapacity = false;
        private bool needToWaitForPortInOutTypeChangeCompleted = false;
        private bool exceedPortCapacity = false;
        private bool currentZoneAutoOutBanned = false;
        private bool destZoneAutoInBanned = false;
        private bool bestRouteSufficient = true;

        public bool NeedToAlternateTransportCommand { get { return needToAlternateTransportCommand; } set { needToAlternateTransportCommand = value; } }
        public bool CarrierAvailable { get { return carrierAvailable; } set { carrierAvailable = value; } }
        public bool CurrentMachineProcessTypeAvailable { get { return currentMachineProcessTypeAvailable; } set { currentMachineProcessTypeAvailable = value; } }
        public bool CurrentUnitProcessTypeAvailable { get { return currentUnitProcessTypeAvailable; } set { currentUnitProcessTypeAvailable = value; } }
        public bool DestMachineProcessTypeAvailable { get { return destMachineProcessTypeAvailable; } set { destMachineProcessTypeAvailable = value; } }
        public bool DestUnitProcessTypeAvailable { get { return destUnitProcessTypeAvailable; } set { destUnitProcessTypeAvailable = value; } }
        public bool DestZoneProcessTypeAvailable { get { return destZoneProcessTypeAvailable; } set { destZoneProcessTypeAvailable = value; } }
        public bool AlternateMachineProcessTypeAvailable { get { return alternateMachineProcessTypeAvailable; } set { alternateMachineProcessTypeAvailable = value; } }
        public bool CurrentMachineAvailable { get { return currentMachineAvailable; } set { currentMachineAvailable = value; } }
        public bool CurrentUnitAvailable { get { return currentUnitAvailable; } set { currentUnitAvailable = value; } }
        public bool DestMachineAvailable { get { return destMachineAvailable; } set { destMachineAvailable = value; } }
        public bool DestUnitAvailable { get { return destUnitAvailable; } set { destUnitAvailable = value; } }
        public bool AlternateMachineAvailable { get { return alternateMachineAvailable; } set { alternateMachineAvailable = value; } }
        public bool AlternateZoneAvailable { get { return alternateZoneAvailable; } set { alternateZoneAvailable = value; } }
        public bool AlternateUnitAvailable { get { return alternateUnitAvailable; } set { alternateUnitAvailable = value; } }
        public bool RecoveryMachineAvailable { get { return recoveryMachineAvailable; } set { recoveryMachineAvailable = value; } }
        public bool DestUnitSelected { get { return destUnitSelected; } set { destUnitSelected = value; } }
        public bool DestMachineCapacityInRange { get { return destMachineCapacityInRange; } set { destMachineCapacityInRange = value; } }
        public bool ProcessTypeMatched { get { return processTypeMatched; } set { processTypeMatched = value; } }
        public bool BestRouteAvailable { get { return bestRouteAvailable; } set { bestRouteAvailable = value; } }
        public bool RouteExist { get { return routeExist; } set { routeExist = value; } }
        public bool LinkedAutoPortExist { get { return linkedAutoPortExist; } set { linkedAutoPortExist = value; } }
        public bool SuitableAutoPortExist { get { return suitableAutoPortExist; } set { suitableAutoPortExist = value; } }
        public bool SuitableBidirectionalAutoPortExist { get { return suitableBidirectionalAutoPortExist; } set { suitableBidirectionalAutoPortExist = value; } }
        public bool ExceedBidirectionalCommandCapacity { get { return exceedBidirectionalCommandCapacity; } set { exceedBidirectionalCommandCapacity = value; } }
        public bool NeedToWaitForPortInOutTypeChangeCompleted { get { return needToWaitForPortInOutTypeChangeCompleted; } set { needToWaitForPortInOutTypeChangeCompleted = value; } }
        public bool ExceedPortCapacity { get { return exceedPortCapacity; } set { exceedPortCapacity = value; } }
        public bool CurrentZoneAutoOutBanned { get { return currentZoneAutoOutBanned; } set { currentZoneAutoOutBanned = value; } }
        public bool DestZoneAutoInBanned { get { return destZoneAutoInBanned; } set { destZoneAutoInBanned = value; } }
        public bool BestRouteSufficient { get { return bestRouteSufficient; } set { bestRouteSufficient = value; } }

        public void Reset()
        {
            //logger.Info("transferState will be reset");

            this.needToAlternateTransportCommand = true;

            this.carrierAvailable = true;

            this.currentMachineProcessTypeAvailable = true;
            this.currentUnitProcessTypeAvailable = true;
            this.destMachineProcessTypeAvailable = true;
            this.destUnitProcessTypeAvailable = true;
            this.destZoneProcessTypeAvailable = true;
            this.alternateMachineProcessTypeAvailable = true;

            this.currentMachineAvailable = true;
            this.currentUnitAvailable = true;

            this.destMachineAvailable = true;
            this.destUnitAvailable = true;

            this.alternateMachineAvailable = true;
            this.alternateZoneAvailable = true;
            this.alternateUnitAvailable = true;

            this.recoveryMachineAvailable = true;

            this.destUnitSelected = true;

            this.destMachineCapacityInRange = true;

            this.processTypeMatched = true;
            this.bestRouteAvailable = true;
            this.routeExist = true;
            this.linkedAutoPortExist = true;
            this.suitableAutoPortExist = true;
            this.suitableBidirectionalAutoPortExist = true;
            this.exceedBidirectionalCommandCapacity = false;
            this.needToWaitForPortInOutTypeChangeCompleted = false;
            this.exceedPortCapacity = false;

            this.currentZoneAutoOutBanned = false;
            this.destZoneAutoInBanned = false;

            this.bestRouteSufficient = true;
        }

        public bool IsProcessTypeAvailable()
        {
            if ((CurrentMachineProcessTypeAvailable) || (CurrentUnitProcessTypeAvailable) || (DestMachineProcessTypeAvailable) || (DestUnitProcessTypeAvailable) || (DestZoneProcessTypeAvailable))
            {
                return false;
            }
            return true;
        }

        public virtual string ToStringTransferState()
        {
            string transferState = "NOTAVAILABLE";
            if (DestMachineAvailable)
            {
                transferState = "DESTMACHINEISNOTAVAILABLE";
            }
            else if (DestUnitAvailable)
            {
                transferState = "DESTUNITISNOTAVAILABLE";
            }
            else if (CurrentMachineAvailable)
            {
                transferState = "CURRENTMACHINEISNOTAVAILABLE";
            }
            else if (CurrentUnitAvailable)
            {
                transferState = "CURRENTUNITISNOTAVAILABLE";
            }
            else if (DestMachineCapacityInRange)
            {
                transferState = "DESTMACHINECAPACITYISOUTOFRANGE";
            }
            else if (DestUnitSelected)
            {
                transferState = "DESTUNITISNOTSELETECTED";
            }
            else if (ProcessTypeMatched)
            {
                transferState = "PROCESSTYPENOTMATCHED";
            }
            else if (BestRouteAvailable)
            {
                transferState = "ROUTEISNOTAVAILABLE";
            }
            else if (BestRouteSufficient)
            {
                transferState = "ROUTEISNOTSUFFICIENT";
            }
            else if (CurrentMachineProcessTypeAvailable)
            {
                transferState = "CURRENTMACHINEPROCESSTYPEISNOTAVAILABLE";
            }
            else if (CurrentUnitProcessTypeAvailable)
            {
                transferState = "CURRENTUNITPROCESSTYPEISNOTAVAILABLE";
            }
            else if (DestMachineProcessTypeAvailable)
            {
                transferState = "DESTMACHINEPROCESSTYPEISNOTAVAILABLE";
            }
            else if (DestUnitProcessTypeAvailable)
            {
                transferState = "DESTUNITPROCESSTYPEISNOTAVAILABLE";
            }
            else if (DestZoneProcessTypeAvailable)
            {
                transferState = "DESTZONEPROCESSTYPEISNOTAVAILABLE";
            }
            else if (LinkedAutoPortExist)
            {
                transferState = "LINKEDAUTOPORTNOTEXIST";
            }
            else if (SuitableAutoPortExist)
            {
                transferState = "SUITABLEAUTOPORTNOTEXIST";
            }
            else if (SuitableBidirectionalAutoPortExist)
            {
                transferState = "SUITABLEBIDIRECTIONALAUTOPORTNOTEXIST";
            }
            else if (CurrentZoneAutoOutBanned)
            {
                transferState = "CURRENTZONEAUTOOUTBANNED";
            }
            else if (DestZoneAutoInBanned)
            {
                transferState = "DESTZONEAUTOINBANNED";
            }
            return transferState;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transferState{");
            sb.Append("currentMachineAvailable=").Append(this.currentMachineAvailable);
            sb.Append(", currentUnitAvailable=").Append(this.currentUnitAvailable);
            sb.Append(", destMachineAvailable=").Append(this.destMachineAvailable);
            sb.Append(", destUnitAvailable=").Append(this.destUnitAvailable);
            sb.Append(", bestRouteAvailable=").Append(this.bestRouteAvailable);
            sb.Append(", routeExist=").Append(this.routeExist);
            sb.Append(", linkedAutoPortExist=").Append(this.linkedAutoPortExist);
            sb.Append(", suitableAutoPortExist=").Append(this.suitableAutoPortExist);
            sb.Append(", suitableBidirectionalAutoPortExist=").Append(this.suitableBidirectionalAutoPortExist);
            sb.Append(", exceedBidirectionalCommandCapacity=").Append(this.exceedBidirectionalCommandCapacity);
            sb.Append(", needToWaitForPortInOutTypeChangeCompleted=").Append(this.needToWaitForPortInOutTypeChangeCompleted);
            sb.Append(", bestRouteSufficient=").Append(this.bestRouteSufficient);
            sb.Append(", destUnitSelected=").Append(this.destUnitSelected);
            sb.Append(", destMachineCapacityInRange=").Append(this.destMachineCapacityInRange);
            sb.Append(", alternateMachineAvailable=").Append(this.alternateMachineAvailable);
            sb.Append(", alternateZoneAvailable=").Append(this.alternateZoneAvailable);
            sb.Append(", alternateUnitAvailable=").Append(this.alternateUnitAvailable);
            sb.Append(", currentMachineProcessTypeAvailable=").Append(this.currentMachineProcessTypeAvailable);
            sb.Append(", currentUnitProcessTypeAvailable=").Append(this.currentUnitProcessTypeAvailable);
            sb.Append(", destMachineProcessTypeAvailable=").Append(this.destMachineProcessTypeAvailable);
            sb.Append(", destUnitProcessTypeAvailable=").Append(this.destUnitProcessTypeAvailable);
            sb.Append(", destZoneProcessTypeAvailable=").Append(this.destZoneProcessTypeAvailable);
            sb.Append(", alternateMachineProcessTypeAvailable=").Append(this.alternateMachineProcessTypeAvailable);
            sb.Append(", currentZoneAutoOutBanned=").Append(this.currentZoneAutoOutBanned);
            sb.Append(", destZoneAutoInBanned=").Append(this.destZoneAutoInBanned);
            sb.Append("}");
            return sb.ToString();
        }


    }
}
