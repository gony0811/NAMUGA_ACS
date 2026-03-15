using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Resource;
using ACS.Core.History;
using ACS.Core.Alarm;
using ACS.Core.Application;
using ACS.Core.History;
using ACS.Core.Host;
using ACS.Core.Logging;
using ACS.Core.Material;
using ACS.Core.Message;
using ACS.Core.Path;
using ACS.Core.Transfer;
using ACS.Core.Base;
using ACS.Core.Path;
using ACS.Core.Cache;
using ACS.Core.Message;

namespace ACS.Core.Base
{
    public abstract class AbstractServiceEx : AbstractService
    {

        public new Logger logger = Logger.GetLogger(typeof(AbstractServiceEx));

        //public String name;
        //public IApplicationManager ApplicationManager { get; set; }
        //public IAlarmManagerEx AlarmManager { get; set; }
        public new IMessageManagerExs MessageManager { get; set; }
        //public IMaterialManagerEx MaterialManager { get; set; }
        public new IResourceManagerExs ResourceManager { get; set; }
        ////public ITransferManagerEx TransferManager { get; set; }
        //public IApplicationControlManager ControlManager { get; set; }
        ////public EventManager eventManager;
        public new IHistoryManagerExs HistoryManager { get; set; }
        public new IPathManagerExs PathManager { get; set; }
        public ICacheManagerEx CacheManager { get; set; }
        //public IHostMessageManager HostMessageManager { get; set; }
        //public IHistoryManagerEx HistoryManager { get; set; }
        public new ILogManager LogManager { get; set; }
      
        //public String Name { get { return this.name; } set { this.name = value; } }

        public AbstractServiceEx()
        {
            //System.out.println((new StringBuilder("[")).append(TimeUtils.getTimeToMilliPrettyFormat()).append("] ").append(getClass().getName()).append(" will be created").toString());
        }

        public override void Init()
        {
            String time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff"); //TimeUtils.getTimeToMilliPrettyFormat();
            Console.WriteLine((new StringBuilder("[")).Append(time).Append("] ").Append(this.GetType().FullName).Append(" will be initialized").ToString());
            if (LogManager == null)
                Console.WriteLine((new StringBuilder("[")).Append(time).Append("] logManger is not used, because it is not wired at ").Append(this.GetType().FullName).ToString());
        }
    }
}
