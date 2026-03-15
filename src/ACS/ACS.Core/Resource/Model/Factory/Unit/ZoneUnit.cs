using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Unit
{
    public class ZoneUnit : Unit
    {
        protected string zoneName = "";
        protected string transportUnitAccessible = "T";

        public string ZoneName
        {
            get { return zoneName; }
            set { zoneName = value; }
        }

        public string TransportUnitAccessible
        {
            get { return transportUnitAccessible; }
            set { transportUnitAccessible = value; }
        }
    }
}
