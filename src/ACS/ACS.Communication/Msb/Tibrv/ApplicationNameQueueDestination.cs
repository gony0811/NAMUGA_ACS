using ACS.Communication.Msb.Tibrv;
using ACS.Framework.Application;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;
using ACS.Framework.Logging;

namespace ACS.Communication.Msb.Tibrv
{
    public class ApplicationNameQueueDestination : QueueDestination
    {

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public ApplicationNameQueueDestination() throws com.tibco.tibrv.TibrvException
        public ApplicationNameQueueDestination()
        {
            
        }


        //public void Init()
        public override void Init()
        {
            base.Init();

            if (!Name.Contains("@{application}"))
            {
                logger.Warn("name does not have @{application}, just apply name{" + Name + "}");
                
                return;
            }
            string applicationName = null;
            try
            {
                applicationName = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];
            }
            catch (Exception e)
            {
                logger.Error("failed to get applicationName", e);
                logger.Info("name{" + Name + "} is not changed");
                return;
            }
            Name = Name.Replace("@{application}", applicationName);
            logger.Info("name was changed to {" + Name + "}");
        }
    }
}
