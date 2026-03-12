using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.Tibrv
{
    public class TransportParameter
    {
        private string service;
        private string network;
        private string daemon;

        public string Daemon
        {
            get
            {
                return this.daemon;
            }
            set
            {
                this.daemon = value;
            }
        }


        public string Network
        {
            get
            {
                return this.network;
            }
            set
            {
                this.network = value;
            }
        }


        public string Service
        {
            get
            {
                return this.service;
            }
            set
            {
                this.service = value;
            }
        }

    }

}
