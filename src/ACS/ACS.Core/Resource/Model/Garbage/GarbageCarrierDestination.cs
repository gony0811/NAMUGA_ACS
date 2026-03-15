using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Garbage
{
    public class GarbageCarrierDestination : Entity
    {
        public static string GARBAGE_DESTINATION_TYPE_STORED_AND_SCAN = "StoredAndIdReadCommand";
        public static string GARBAGE_DESTINATION_TYPE_AUTO_READING_PORT = "AutoIdReadUnit";
        public static string GARBAGE_DESTINATION_TYPE_OPERATOR_MANUAL_PORT = "OperatorManualPort";
        private string machineName;
        private string garbageDestinationType;
        private string used = "T";
        private string order;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string GarbageDestinationType
        {
            get { return garbageDestinationType; }
            set { garbageDestinationType = value; }
        }

        public string Used
        {
            get { return used; }
            set { used = value; }
        }

        public string Order
        {
            get { return order; }
            set { order = value; }
        }
    }
}
