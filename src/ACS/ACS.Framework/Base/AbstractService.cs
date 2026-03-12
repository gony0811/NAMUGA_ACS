using System;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Alarm;
using ACS.Framework.Application;
using ACS.Framework.History;
using ACS.Framework.Material;
using System.Collections.Generic;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Material.Model;
using ACS.Framework.Path;
using ACS.Framework.Path.Model;
using ACS.Framework.Message;
using ACS.Framework.Resource;
using ACS.Framework.Transfer;
using ACS.Framework.Host;
using ACS.Framework.Logging;
using log4net;

namespace ACS.Framework.Base
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
