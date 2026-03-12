using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model.Factory.Zone;

namespace ACS.Framework.Message.Model.Server
{
    public class ZonesMessage: StatusVariableMessage
    {
        private List<Zone> zones = new List<Zone>();

        public List<Zone> Zones { get { return zones; } set { zones = value; } }
        public void Add(Zone zone)
        {
            this.Zones.Add(zone);
        }
    }
}
