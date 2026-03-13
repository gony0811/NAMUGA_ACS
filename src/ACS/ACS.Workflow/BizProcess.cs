using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Autofac;
using ACS.Framework.Logging;
using ACS.Framework.Logging.Model;
using System.Globalization;

namespace ACS.Workflow 
{
    public class BizProcessContext
    {
        private Logger logger = Logger.GetLogger(typeof(BizProcessContext));

        public string Command {get; private set;    }

        public Tuple<Type, object, bool> BizProcessJob { get; private set; }

        public bool AsyncMode { get; private set; }

        public List<string> TypeNames { get; private set; }

        public Object[] Arguments { get; private set; }

        public int JobResult { get; private set; }
        private Dictionary<string, Tuple<Type, object>> commandJobList;
        public ILifetimeScope LifetimeScope { get; set; }
        //public BizFileRepository BizFileRepository { get; set; }

        public BizProcessContext(string command, Tuple<Type, object, bool> bizContext, object[] args, bool isAsyncMode = false)
        {
            Command = command;
            BizProcessJob = bizContext;
            Arguments = args;
            AsyncMode = isAsyncMode;
        }

        public void Init()
        {
        }

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        { 
            get { return commandJobList;}  
            set { commandJobList = value;} 
        }

        private void WriteLog(string strLogMessage, string messageName, string transactionId)
        {
            LogMessage logMessage = logger.CreateLogMessage(strLogMessage, messageName);          
            logMessage.ThreadName = "ThreadId=" + Thread.CurrentThread.ManagedThreadId.ToString();
            logMessage.TransactionId = transactionId;
            logMessage.MessageName = messageName;
            logMessage.CommunicationMessageName = messageName;
            logMessage.SaveToDatabase = true;
            logMessage.Text = strLogMessage;
            logger.Info(logMessage);
        }


        public bool Execute()//string command, object args, bool isAsyncMode)
        {
            Type _type = BizProcessJob.Item1;//CommandJobList[Command].Item1;
            object _obj = BizProcessJob.Item2;// CommandJobList[Command].Item2;
            bool _asyncMode = BizProcessJob.Item3;
            eJobResult jobResult = eJobResult.ASYNCJOB;
            //logger.logManager = (ILogManager)ApplicationContext.GetObject("LogManager");
            //DateTime wfStartTime;
            //DateTime wfEndTime;
            //TimeSpan wfExeTimeSpan;


            if (BizProcessJob == null) return false;// || BizProcessJob.Count <= 0 || !BizProcessJob.ContainsKey(Command)) return false;

            // WorkflowManager default setting or biz config xml setting, if anyone true is true
            if (_asyncMode || AsyncMode)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
                {
                    try
                    {
                        
                        PropertyInfo pi = _type.GetProperty("LifetimeScope");
                        MethodInfo mi = _type.GetMethod("Execute");
                        object[] param = { Arguments };
                        pi.SetValue(_obj, LifetimeScope);
                        //string logText = string.Format("workflow execute : {0}", Command);
                        //string dateCode = DateTime.Now.ToString("yyyyMMddHHmmssfff", new CultureInfo("en-US"));
                        //string transactionId = string.Format(@"{0}.{1}", dateCode, Thread.GetDomainID().ToString());
                        //WriteLog(logText, _type.Name, transactionId);
                        //wfStartTime = DateTime.Now;
                        jobResult = (eJobResult)mi.Invoke(_obj, param);
                        //wfEndTime = DateTime.Now;
                        //wfExeTimeSpan = wfEndTime - wfStartTime;
                        //logText = string.Format("workflow execute result : {0} | {1}", Command, wfExeTimeSpan.TotalMilliseconds);
                        //WriteLog(logText, _type.Name, transactionId);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                        return;
                    }
                }));

                return true;
            }
            else
            {
                try
                {


                    PropertyInfo pi = _type.GetProperty("LifetimeScope");
                    MethodInfo mi = _type.GetMethod("Execute");
                    object[] param = { Arguments };
                    pi.SetValue(_obj, LifetimeScope);
                    //string logText = string.Format("workflow execute : {0}", _type.Name);
                    //string dateCode = DateTime.Now.ToString("yyyyMMddHHmmssfff", new CultureInfo("en-US"));
                    //string transactionId = string.Format(@"{0}.{1}", dateCode, Thread.GetDomainID().ToString());
                    //WriteLog(logText, _type.Name, transactionId);
                    //wfStartTime = DateTime.Now;
                    jobResult = (eJobResult)mi.Invoke(_obj, param);
                    //wfEndTime = DateTime.Now;
                    //wfExeTimeSpan = wfEndTime - wfStartTime;
                    //logText = string.Format("workflow execute result : {0} | {1}", _type.Name, wfExeTimeSpan.TotalMilliseconds);
                    //WriteLog(logText, _type.Name, transactionId);
                    if (jobResult == eJobResult.SUCCESS) return true;
                }
                catch(Exception e)
                {
                    logger.Error(e);
                    return false;
                }
            }


            return false;
        }
    }
}
