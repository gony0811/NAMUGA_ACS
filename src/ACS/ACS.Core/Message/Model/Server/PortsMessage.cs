using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource.Model.Factory.Unit;

namespace ACS.Core.Message.Model.Server
{
    public class PortsMessage: StatusVariableMessage
    {
        private IList ports = new ArrayList();
        private IList portNames = new ArrayList();

        protected internal IList Ports { get {return ports;} set {ports = value;}}
        protected internal IList PortNames { get { return portNames; } set { portNames = value; } }


        public void Add(Port port)
        {
            this.Ports.Add(port);
        }

        public int AddPortName(String portName)
        {
            return this.PortNames.Add(portName);
        }

    }
}
