using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Emulator
{
    /// <summary>
    /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
    /// </summary>
    public class ServerIoConnector //: ThreadStart
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


        //public virtual void Run()
        //{
        //    this.abstractSocketService.Sleep(5000L);
        //    this.abstractSocketService.Connect();
        //}
    }
}
