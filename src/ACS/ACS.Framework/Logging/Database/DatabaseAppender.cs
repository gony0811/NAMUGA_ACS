using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Logging;
using ACS.Framework.Logging.Model;
using log4net.Appender;
using log4net.Core;

namespace ACS.Framework.Logging.Database
{
    public class DatabaseAppender : AppenderSkeleton 
    {
        public ILogManager LogManager { get; set; }
        public bool Ready { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if(this.Ready)
            {
                Object message = loggingEvent.MessageObject;
                if((message is LogMessage))
                {
                    if(((LogMessage)message).SaveToDatabase && (this.LogManager != null))
                    {
                        this.LogManager.CreateLogMessage(loggingEvent);
                    }
                }
            }
        }

        public new void Close()
        {
            this.Ready = false;
        }

        public bool RequireLayout()
        {
            return true;
        }
    }
}
