using System;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Alarm;
using ACS.Core.Application;
using ACS.Core.History;
using ACS.Core.Material;
using System.Collections.Generic;
using ACS.Core.Alarm.Model;
using ACS.Core.Message.Model;
using ACS.Core.Message.Model.Ui;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer.Model;
using ACS.Core.Material.Model;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Transfer;
using ACS.Core.Host;
using ACS.Core.Logging;

namespace ACS.Core.Base
{
    public abstract class AbstractService
    {
        public Logger logger = Logger.GetLogger(typeof(AbstractService)); 
       
        public String name;
        public IApplicationManager ApplicationManager { get; set; }
        public IAlarmManagerEx AlarmManager { get; set; }
        public IMessageManagerEx MessageManager { get; set; }
        public IMaterialManagerEx MaterialManager { get; set; }
        public IResourceManagerEx ResourceManager { get; set; }
        public ITransferManagerEx TransferManager { get; set; }
        public IApplicationControlManager ControlManager { get; set; }
        //public EventManager eventManager;
        public IPathManagerEx PathManager { get; set; }
        public IHostMessageManager HostMessageManager { get; set; }
        public IHistoryManagerEx HistoryManager { get; set; }
        public ILogManager LogManager { get; set; }
        public String Name { get { return this.name; } set { this.name = value; } }

        public AbstractService()
        {
            //System.out.println((new StringBuilder("[")).append(TimeUtils.getTimeToMilliPrettyFormat()).append("] ").append(getClass().getName()).append(" will be created").toString());
        }

        public virtual void Init()
        {
            String time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff"); //TimeUtils.getTimeToMilliPrettyFormat();
            Console.WriteLine((new StringBuilder("[")).Append(time).Append("] ").Append(this.GetType().FullName).Append(" will be initialized").ToString());
            if(LogManager == null)
                Console.WriteLine((new StringBuilder("[")).Append(time).Append("] logManger is not used, because it is not wired at ").Append(this.GetType().FullName).ToString());
        }


    }
}
