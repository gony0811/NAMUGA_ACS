using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using ACS.Utility;
using System.Configuration;
using ACS.Framework.Application;

namespace ACS.Framework.Logging
{
    public class LogPropertyWatchDog : FileSystemWatcher
    {
        public ILogManager LogManager { get; set; }

        public LogPropertyWatchDog(string fileName)
        {
            base.Path = SystemUtility.GetFullPathName(ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE], @"config/@{site}/startup/acs/log");
            //base.Path = SystemUtility.GetFullPathName(@"config/startup/acs/log");
            base.Filter = @"log-TS01_P.xml";
            XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(fileName));
            base.NotifyFilter = NotifyFilters.LastWrite;
            base.Changed += new FileSystemEventHandler(OnChanged);
            
        }

        protected void OnChanged(object source, FileSystemEventArgs e)
        {
            
        }
    }
}
