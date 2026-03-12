using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Route.Model
{
    public class FixedRoute : Entity
    {
        private string sourceMachineName = "";
        private string destMachineName = "";
        private string used = "T";
        private int priority = 1;
        private string routes;
        private int weight;

        public FixedRoute()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public string SourceMachineName { get { return sourceMachineName; } set { sourceMachineName = value; } }
        public string DestMachineName { get { return destMachineName; } set { destMachineName = value; } }
        public string Used { get { return used; } set { used = value; } }
        public int Priority { get { return priority; } set { priority = value; } }
        public string Routes { get { return routes; } set { routes = value; } }
        public int Weight { get { return weight; } set { weight = value; } }
    }
}
