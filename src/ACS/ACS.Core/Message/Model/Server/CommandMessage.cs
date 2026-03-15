using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource.Model.Factory.Zone;

namespace ACS.Core.Message.Model.Server
{
    public class CommandMessage : TransferMessage
    {
        private string resultCode;
        private string carrierZoneName;
        private string lotId;
        private string carrierType;
        private string nextToUnitName;
        private Zone carrierZone;

        public string ResultCode { get { return resultCode; } set { resultCode = value; } }  
        public string CarrierZoneName { get { return carrierZoneName; } set { carrierZoneName = value; } }  
        public string LotId { get { return lotId; } set { lotId = value; } }  
        public string CarrierType { get { return carrierType; } set { carrierType = value; } }  
        public string NextToUnitName { get { return nextToUnitName; } set { nextToUnitName = value; } }
        public Zone CarrierZone { get { return carrierZone; } set { carrierZone = value; } }  

        public string GetResultCodeToString()
        {
            return GetResultCodeToString(this.resultCode);
        }

        public string GetResultCodeToString(string resultCode)
        {
            string resultCodeToString = "nonstandard";
            if (resultCode.Equals("0"))
            {
                resultCodeToString = "success";
            }
            else if (resultCode.Equals("1"))
            {
                resultCodeToString = "otherError";
            }
            else if (resultCode.Equals("2"))
            {
                resultCodeToString = "shelfZoneIsFull";
            }
            else if (resultCode.Equals("3"))
            {
                resultCodeToString = "duplicateId";
            }
            else if (resultCode.Equals("4"))
            {
                resultCodeToString = "mismatchId";
            }
            else if (resultCode.Equals("5"))
            {
                resultCodeToString = IsRailMessage() ? "sourceInterLockError" : "failureToReadId";
            }
            else if (resultCode.Equals("6"))
            {
                resultCodeToString = resultCodeToString + "." + resultCode;
            }
            else
            {
                resultCodeToString = resultCodeToString + "." + resultCode;
            }
            return resultCodeToString;
        }

        public string GetResultCodeToReason()
        {
            return GetResultCodeToReason(this.resultCode);
        }

        public string GetResultCodeToReason(string resultCode)
        {
            string resultCodeToReason = "NONSTANDARD";
            if (resultCode.Equals("0"))
            {
                resultCodeToReason = "";
            }
            else if (resultCode.Equals("1"))
            {
                resultCodeToReason = "COMMANDNOTEXIST";
            }
            else if (resultCode.Equals("2"))
            {
                resultCodeToReason = "ZONEISFULL";
            }
            else if (resultCode.Equals("3"))
            {
                resultCodeToReason = "CARRIERDUPLICATED";
            }
            else if (resultCode.Equals("4"))
            {
                resultCodeToReason = "CARRIERMISMATCHED";
            }
            else if (resultCode.Equals("5"))
            {
                resultCodeToReason = IsRailMessage() ? "SOURCEINTERLOCKERROR" : "CARRIERFAILTOREAD";
            }
            else if (resultCode.Equals("6"))
            {
                resultCodeToReason = resultCodeToReason + "." + resultCode;
            }
            else
            {
                resultCodeToReason = resultCodeToReason + "." + resultCode;
            }
            return resultCodeToReason;
        }

        public string GetCommandReplyResultCodeToString()
        {
            return GetCommandReplyResultCodeToString(this.resultCode);
        }

        public string GetCommandReplyResultCodeToString(string resultCode)
        {
            string resultCodeToString = "nonstandard";
            if (resultCode.Equals("0"))
            {
                resultCodeToString = "success";
            }
            else if (resultCode.Equals("1"))
            {
                resultCodeToString = "commandDoesNotexist";
            }
            else if (resultCode.Equals("2"))
            {
                resultCodeToString = "currentlyNotAbleToExecute";
            }
            else if (resultCode.Equals("3"))
            {
                resultCodeToString = "atLeastOneParameterIsNotValid";
            }
            else if (resultCode.Equals("4"))
            {
                resultCodeToString = "confirmed";
            }
            else if (resultCode.Equals("5"))
            {
                resultCodeToString = "rejectedAlreadyRequested";
            }
            else if (resultCode.Equals("6"))
            {
                resultCodeToString = "objectDoesNotExist";
            }
            else
            {
                resultCodeToString = resultCodeToString + "." + resultCode;
            }
            return resultCodeToString;
        }

        public string GetCommandReplyResultCodeToReason()
        {
            return GetCommandReplyResultCodeToReason(this.resultCode);
        }

        public string GetCommandReplyResultCodeToReason(string resultCode)
        {
            string resultCodeToReason = "NONSTANDARD";
            if (resultCode.Equals("0"))
            {
                resultCodeToReason = "";
            }
            else if (resultCode.Equals("1"))
            {
                resultCodeToReason = "COMMANDNOTEXIST";
            }
            else if (resultCode.Equals("2"))
            {
                resultCodeToReason = "COMMANDNOTABLETOEXECUTE";
            }
            else if (resultCode.Equals("3"))
            {
                resultCodeToReason = "COMMANDPARAMETERNOTVALID";
            }
            else if (resultCode.Equals("4"))
            {
                resultCodeToReason = "";
            }
            else if (resultCode.Equals("5"))
            {
                resultCodeToReason = "COMMANDALREADYREQUESTED";
            }
            else if (resultCode.Equals("6"))
            {
                resultCodeToReason = "OBJECTNOTEXIST";
            }
            else
            {
                resultCodeToReason = resultCodeToReason + "." + resultCode;
            }
            return resultCodeToReason;
        }

        protected bool IsRailMessage()
        {
            return this.MessageName.StartsWith("RAIL");
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("commandMessage{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", currentMachineName=").Append(this.CurrentMachineName);
            sb.Append(", currentUnitName=").Append(this.CurrentUnitName);
            sb.Append(", originatedType=").Append(this.OriginatedType);
            sb.Append(", originatedName=").Append(this.OriginatedName);
            sb.Append(", connectionId=").Append(this.ConnectionId);
            sb.Append(", userName=").Append(this.UserName);
            sb.Append(", resultCode=").Append(this.resultCode);
            sb.Append(", carrierZoneName=").Append(this.carrierZoneName);
            sb.Append(", lotId=").Append(this.lotId);
            sb.Append(", carrierType=").Append(this.carrierType);
            sb.Append(", nextToUnitName=").Append(this.nextToUnitName);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
