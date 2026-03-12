using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Transfer.Model
{
    public class Command : TimedEntity
    {
        public static String TO_TYPE_NOTDESIGNATED = "NOTDESIGNATED";
        public static String TO_TYPE_PORT = "PORT";
        public static String TO_TYPE_SHELF = "SHELF";
        public static String TO_TYPE_ZONE = "ZONE";
        private String carrierName = "";
        private String commandMachineName = "";
        private String transportMachineName = "";
        private String transportUnitName = "";
        private String fromMachineName = "";
        private String fromUnitName = "";
        private String fromContainerName = "";
        private String fromSlotNumber = "";
        private String toMachineName = "";
        private String toUnitName = "";
        private String toContainerName = "";
        private String toSlotNumber = "";
        private String toType = "NOTDESIGNATED";
        private bool valid = true;

        public string CommandMachineName { get { return commandMachineName; } set { commandMachineName = value; } }
        public string TransportMachineName  { get { return transportMachineName; } set { transportMachineName = value; } }
        public string TransportUnitName  { get { return transportUnitName; } set { transportUnitName = value; } }
        public string ToMachineName  { get { return toMachineName; } set { toMachineName = value; } }
        public string ToUnitName  { get { return toUnitName; } set { toUnitName = value; } }
        public string ToContainerName  { get { return toContainerName; } set { toContainerName = value; } }
        public string ToSlotNumber  { get { return toSlotNumber; } set { toSlotNumber = value; } }
        public string FromMachineName  { get { return fromMachineName; } set { fromMachineName = value; } }
        public string FromUnitName  { get { return fromUnitName; } set { fromUnitName = value; } }
        public string FromContainerName  { get { return fromContainerName; } set { fromContainerName = value; } }
        public string FromSlotNumber  { get { return fromSlotNumber; } set { fromSlotNumber = value; } }
        public string CarrierName  { get { return carrierName; } set { carrierName = value; } }
        public string ToType { get { return toType; } set { toType = value; } }
        public bool ToTypeUnit()
        {
            if ((this.ToType.Equals("PORT")) || (this.ToType.Equals("SHELF")))
            {
                return true;
            }
            return false;
        }
    }
}
