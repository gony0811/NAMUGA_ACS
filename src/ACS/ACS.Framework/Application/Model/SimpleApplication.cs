using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Application.Model
{
    public class SimpleApplication : NamedEntity
    {
        private string type;
        private string state;

        public string Type { get { return type; } set { type = value; } }
        public string State { get { return state; } set { state = value; } }
    }
}
