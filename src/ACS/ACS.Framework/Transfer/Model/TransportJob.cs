using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Transfer.Model
{
    public class TransportJob : TimedEntity
    {
        public static String DEFAULT_PRIORITY = "50";
        public static String HIGHST_PRIORITY = "99";
        public static String CLEAR = "";
        public static String STATE_CREATED = "CREATED";
        public static String STATE_QUEUING = "QUEUING";
        public static String STATE_QUEUED = "QUEUED";

        public static String STATE_ABORTFAILED = "ABORTFAILED";
        public static String DEST_TYPE_NOTDESIGNATED = "NOTDESIGNATED";
        public static String DEST_TYPE_PORT = "PORT";
        public static String DEST_TYPE_SHELF = "SHELF";
        public static String DEST_TYPE_GROUP = "GROUP";
        public static String DEST_TYPE_ZONE = "ZONE";
        public static String DEST_UNIT_NOTSELECTED = "NOTSELECTED";
        public static String TRANSFER_TYPE_I = "TRANSFER-TYPE-I";
        public static String TRANSFER_TYPE_II = "TRANSFER-TYPE-II";
        public static String TRANSFER_TYPE_III = "TRANSFER-TYPE-III";
        public static String TRANSFER_TYPE_IV = "TRANSFER-TYPE-IV";
        public string CarrierName { get; set; }
        public string SourceMachineName { get; set; }
        public string SourceUnitName { get; set; }
        public string SourceContainerName { get; set; }
        public string SourceSlotNumber { get; set; }
        public string DestType { get; set; } 
        public string DestGroupName { get; set; }
        public string DestMachineName { get; set; }
        public string DestUnitName { get; set; }
        public string DestContainerName { get; set; }
        public string DestSlotNumber { get; set; }
        public string NewDestGroupName { get; set; }
        public string NewDestMachineName { get; set; }
        public string NewDestUnitName { get; set; }
        public string NewDestType { get; set; }
        public string Priority { get; set; }
        public string State { get; set; }
        public string PreviousState { get; set; }
        public string AlternateStorageName { get; set; }
        public string TroubleMachineName { get; set; }
        public string Reason { get; set; }
        public string FixedRoute { get; set; }
        public DateTime StateChangedTime { get; set; }
        public string CurrentRoutes { get; set; }
        public string ExpectedRoutes { get; set; }
        public string ProcessedRoutes { get; set; }
        public string Moving { get; set; }
        public int AwakeCount { get; set; }
        public string AdditionalInfo { get; set; }

        public TransportJob()
        {
            DestType = "NOTDESIGNATED";
        }
    }
}
