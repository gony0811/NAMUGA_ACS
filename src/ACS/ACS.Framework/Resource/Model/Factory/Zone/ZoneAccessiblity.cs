using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Factory.Zone
{
    public class ZoneAccessiblity : Entity
    {
        public static string ACCESSTYPE_PRIMARY = "PRIMARY";
        public static string ACCESSTYPE_SECONDARY = "SECONDARY";
        string machineName = "";
        string zoneName = "";
        string transportMachineName = "";
        string transportUnitName = "";
        string accessible = "T";
        string accessType = "";

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; }
        }

        public string TransportMachineName
        {
            get { return transportMachineName; }
            set { transportMachineName = value; }
        }

        public string TransportUnitName
        {
            get { return transportUnitName; }
            set { transportUnitName = value; }
        }

        public string Accessible
        {
            get { return accessible; }
            set { accessible = value; }
        }

        public string AccessType
        {
            get { return accessType; }
            set { accessType = value; }
        }
    }
}
