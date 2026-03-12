using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACS.Communication.Socket
{
    public class ClientIoConnector //: ThreadStart
    {
        protected AbstractSocketService abstractSocketService;

        public virtual AbstractSocketService AbstractSocketService
        {
            get
            {
                return this.abstractSocketService;
            }
            set
            {
                this.abstractSocketService = value;
            }
        }


        public virtual void Run()
        {
            this.abstractSocketService.Sleep(5000L);
            this.abstractSocketService.Connect();
        }
    }
}
