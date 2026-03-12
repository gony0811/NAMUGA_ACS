using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource;
using ACS.Framework.History;
using ACS.Framework.Alarm;
using ACS.Framework.Application;
using ACS.Framework.History;
using ACS.Framework.Host;
using ACS.Framework.Logging;
using ACS.Framework.Material;
using ACS.Framework.Message;
using ACS.Framework.Path;
using ACS.Framework.Transfer;
using ACS.Framework.Base;
using ACS.Framework.Path;
using ACS.Framework.Cache;
using ACS.Framework.Message;

namespace ACS.Framework.Base
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
