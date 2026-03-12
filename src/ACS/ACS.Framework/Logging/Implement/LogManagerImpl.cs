using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using log4net.Core;
using ACS.Framework.Base.Interface;
using ACS.Framework.Logging;
using ACS.Framework.Logging.Model;
using ACS.Framework.Message;
using Spring.Core;
using Spring;

namespace ACS.Framework.Logging.Implement
{
    public class LogManagerImpl : ILogManager
    {
        private string loglevel;

        public IPersistentDao PersistentDao { get; set; }
        public ThreadLocal<object> ThreadLocal { get; set; }
        public MessageNode MessageNode { get; set; }

        public int TextSizeForInsert { get; set; }
        public int LargeTextSizeForInsert { get; set; }
        public int LogLevelInt { get; set; }
        public bool UseShortClassNameAtOperationName { get; set; }
        public bool UseAdoDotNetAppender { get; set; }
        public IList SkipLoggingMessages { get; set; }
        public IDictionary UseFriendlyCommunicationMessageNames { get; set; }
        public bool UsePhysicalPartitioningTable { get; set; }
        public bool UseFirstIterationLargeLogMessage { get; set; }

        public string LogLevel {
            get { return loglevel; }
            set
            {
                loglevel = value;

                if (loglevel.Equals("DEBUG")) this.LogLevelInt = 10000;
                else if (loglevel.Equals("INFO")) this.LogLevelInt = 20000;
                else if (loglevel.Equals("FINE")) this.LogLevelInt = 20010;
                else if (loglevel.Equals("WELL")) this.LogLevelInt = 20020;
                else if (loglevel.Equals("WARN")) this.LogLevelInt = 30000;
                else if (loglevel.Equals("ERROR")) this.LogLevelInt = 40000;
                else if (loglevel.Equals("FATAL")) this.LogLevelInt = 50000;
                else
                {
                    loglevel = "WELL";
                    LogLevelInt = 20020;
                }
            }
        }

        public LogManagerImpl()
        {
            ThreadLocal = new ThreadLocal<object>();
            UseFriendlyCommunicationMessageNames = new Dictionary<string, string>();
            TextSizeForInsert = 3000;
            LargeTextSizeForInsert = 4000;
        }

        public LargeLogMessage CreateLargeLogMessageInstance(int date)
        {
            throw new NotImplementedException();
        }

        public void CreateLogMessage(LoggingEvent loggingEvent)
        {
            try
            {
                LogMessage logMessage = (LogMessage)loggingEvent.MessageObject;

                if (this.SkipLoggingMessages.Contains(logMessage.MessageName))
                {
                    return;
                }

                string transactionId = logMessage.TransactionId;

                if (string.IsNullOrEmpty(transactionId) && this.ThreadLocal != null)
                {
                    transactionId = this.ThreadLocal.Value != null ? this.ThreadLocal.Value.ToString() : "";
                    logMessage.TransactionId = transactionId;
                }
                logMessage.Time = DateTime.Now;
                logMessage.ThreadName = loggingEvent.ThreadName;
                logMessage.OperationName = loggingEvent.LocationInformation.MethodName;
                logMessage.LogLevel = loggingEvent.Level.Name;
                ChangeCommunicationMessageName(logMessage);

                string text = logMessage.Text;

                if (string.IsNullOrEmpty(text) || text.Length <= this.TextSizeForInsert)
                {
                    logMessage.Text = text;

                    this.PersistentDao.Save(logMessage);
                }
                else
                {
                    int size = text.Length;
                    int fieldSize = this.LargeTextSizeForInsert;
                    int quotient = size / fieldSize;
                    int startIndex = 0;
                    int endIndex = 0;

                    for(int index = 0; index <= quotient; index++)
                    {
                        startIndex = index == 0 ? 0 : index * fieldSize;
                        endIndex = index == quotient ? size : (index + 1) * fieldSize;

                        string largeText = text.Substring(startIndex, endIndex);
                        if(index == 0)
                        {
                            if(this.UseFirstIterationLargeLogMessage)
                            {
                                logMessage.Text = largeText;
                            }
                            else
                            {
                                logMessage.Text = "";
                            }

                            this.PersistentDao.Save(logMessage);
                        }

                        LargeLogMessage largeLogMessage = CreateLargeLogMessageInstance(logMessage.PartitionId);
                        largeLogMessage.Sequence = index;
                        largeLogMessage.LogMessageId = logMessage.Id;
                        largeLogMessage.LargeText = largeText;
                        this.PersistentDao.Save(largeLogMessage);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected void ChangeCommunicationMessageName(LogMessage logMessage)
        {
            string communicationMessageName = logMessage.CommunicationMessageName;
            if (this.UseFriendlyCommunicationMessageNames.Contains(communicationMessageName))
            {
                logMessage.CommunicationMessageName = (string)UseFriendlyCommunicationMessageNames[communicationMessageName];
            }
        }

        public void CreateLogMessage(LogMessage logMessage, string threadName, string operationName, string logLevel)
        {
            try
            {
                if(this.SkipLoggingMessages.Contains(logMessage.MessageName))
                {
                    return;
                }

                string transactionId = logMessage.TransactionId;
                if(string.IsNullOrEmpty(transactionId) && this.ThreadLocal != null)
                {
                    transactionId = this.ThreadLocal.Value != null ? this.ThreadLocal.Value.ToString() : "";

                    logMessage.TransactionId = transactionId;
                }

                logMessage.Time = DateTime.Now;
                logMessage.ThreadName = threadName;
                logMessage.OperationName = operationName;
                logMessage.LogLevel = logLevel;
                ChangeCommunicationMessageName(logMessage);

                string text = logMessage.Text;
                if ((string.IsNullOrEmpty(text)) || (text.Length <= this.TextSizeForInsert))
                {
                    logMessage.Text = text;
                    
                    this.PersistentDao.Save(logMessage);
                }
                else
                {
                    int size = text.Length;
                    int fieldSize = this.LargeTextSizeForInsert;
                    int quotient = size / fieldSize;
                    int startIndex = 0;
                    int endIndex = 0;

                    for(int index = 0; index <= quotient; index++)
                    {
                        startIndex = index == 0 ? 0 : index * fieldSize;
                        endIndex = index == quotient ? size : (index + 1) * fieldSize;

                        string largeText = text.Substring(startIndex, endIndex);
                        if (index == 0)
                        {
                            if (this.UseFirstIterationLargeLogMessage)
                            {
                                logMessage.Text = largeText;
                            }
                            else
                            {
                                logMessage.Text = "";
                            }

                            this.PersistentDao.Save(logMessage);
                        }

                        LargeLogMessage largeLogMessage = CreateLargeLogMessageInstance(logMessage.PartitionId);
                        largeLogMessage.Sequence = index;
                        largeLogMessage.LogMessageId = logMessage.Id;
                        largeLogMessage.LargeText = largeText;
                        this.PersistentDao.Save(largeLogMessage);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public LogMessage CreateLogMessageInstance()
        {
            if (!this.UsePhysicalPartitioningTable)
            {
                return new LogMessage();
            }
            int date = DateTime.Now.Day;
            switch (date)
            {
                case 1:
                    return new LogMessage(date);
                case 2:
                    return new LogMessage(date);
                case 3:
                    return new LogMessage(date);
                case 4:
                    return new LogMessage(date);
                case 5:
                    return new LogMessage(date);
                case 6:
                    return new LogMessage(date);
                case 7:
                    return new LogMessage(date);
                case 8:
                    return new LogMessage(date);
                case 9:
                    return new LogMessage(date);
                case 10:
                    return new LogMessage(date);
                case 11:
                    return new LogMessage(date);
                case 12:
                    return new LogMessage(date);
                case 13:
                    return new LogMessage(date);
                case 14:
                    return new LogMessage(date);
                case 15:
                    return new LogMessage(date);
                case 16:
                    return new LogMessage(date);
                case 17:
                    return new LogMessage(date);
                case 18:
                    return new LogMessage(date);
                case 19:
                    return new LogMessage(date);
                case 20:
                    return new LogMessage(date);
                case 21:
                    return new LogMessage(date);
                case 22:
                    return new LogMessage(date);
                case 23:
                    return new LogMessage(date);
                case 24:
                    return new LogMessage(date);
                case 25:
                    return new LogMessage(date);
                case 26:
                    return new LogMessage(date);
                case 27:
                    return new LogMessage(date);
                case 28:
                    return new LogMessage(date);
                case 29:
                    return new LogMessage(date);
                case 30:
                    return new LogMessage(date);
                case 31:
                    return new LogMessage(date);
            }
            return new LogMessage(date);
        }

        public int GetLargeTextSizeForInsert()
        {
            return LargeTextSizeForInsert;
        }

        public MessageNode GetMessageNode()
        {
            return MessageNode;
        }

        public IPersistentDao GetPersistentDao()
        {
            return PersistentDao;
        }

        public bool IsGreaterOrEqual(int logLevel)
        {
            return logLevel >= this.LogLevelInt;
        }

        public bool IsUseAdoDotNetAppender()
        {
            return this.UseAdoDotNetAppender;
        }

        public bool IsUseShortClassNameAtOperationName()
        {
            return UseShortClassNameAtOperationName;
        }
    }
}
