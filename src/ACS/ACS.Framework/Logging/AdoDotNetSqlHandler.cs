using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using ACS.Framework.Logging.Model;

namespace ACS.Framework.Logging
{
    public class AdoDotNetSqlHandler 
    {
        public string GetStatement(LoggingEvent loggingEvent)
        {
            object message = loggingEvent.MessageObject;

            string statement = null;

            if((message is LogMessage))
            {
                if(((LogMessage)message).SaveToDatabase)
                {
                    statement = "INSERT INTO NT_L_LOGMESSAGE (ID, PARTITIONID, PROCESSNAME, TRANSACTIONID, MESSAGENAME, THREADNAME, OPERATIONNAME, LOGLEVEL, TRANSPORTCOMMANDID, CARRIERNAME, MACHINENAME, UNITNAME, TEXT, TIME) VALUES ('" + ((LogMessage)message).Id + "', '" + ((LogMessage)message).PartitionId + "', '" + ((LogMessage)message).ProcessName + "', '" + ((LogMessage)message).TransactionId + "', '" + ((LogMessage)message).MessageName + "', '" + loggingEvent.ThreadName + "', '" + loggingEvent.LocationInformation.MethodName + "', '" + loggingEvent.Level + "', '" + ((LogMessage) message).TransportCommandId + "', '" + ((LogMessage) message).CarrierName + "', '" + ((LogMessage) message).MachineName + "', '" + ((LogMessage)message) + "', '" + "', " + "SYSTIMESTAMP(6)" + ")";
                }
            }

            return statement;
        }
    }
}
