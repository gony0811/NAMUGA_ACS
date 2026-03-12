using Spring.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.Tibrv
{
    public class HostNameQueueDestination : QueueDestination
    {
        private const string HOST_NAME = "@{host}";

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public HostNameQueueDestination() throws com.tibco.tibrv.TibrvException
        public HostNameQueueDestination()
        {
        }

        public override void Init()
        {
            base.Init();
            if (!Name.Contains("@{host}"))
            {
                //logger.warn("name does not have @{host}, just apply name{" + Name + "}");
                return;
            }
            string hostName = null;
            try
            {
                hostName = Dns.GetHostName();
            }
            catch (Exception e)
            {
                logger.Error("failed to get hostName", e);

                hostName = "unknown" + Guid.NewGuid().ToString();
                hostName = hostName.Replace( "-", "");
            }

            Name = Name.Replace("@{host}", hostName);
            logger.Info("name was changed to {" + Name + "}");
        }
    }

}
