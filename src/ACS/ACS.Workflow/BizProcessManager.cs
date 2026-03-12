using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using Spring.Context;
using ACS.Framework.Logging;

namespace ACS.Workflow
{
    public class BizProcessManager : IDisposable
    {
        protected Logger logger = Logger.GetLogger(typeof(BizProcessManager));
        private BlockingCollection<BizProcessContext> reservedProcess = new BlockingCollection<BizProcessContext>();
        private BizProcessContext currentRunningProcess;
        public BizProcessContext CurrentRunningProcess { get { return currentRunningProcess; } }
        public bool Terminate { get; set; }
        public ILogManager LogManager { get; set; }
        public IApplicationContext ApplicationContext { get; set; }

        public BizProcessManager()
        {
            Terminate = false;
            logger.logManager = LogManager;
        }

        //public int RequestExecuteJob(BizProcessContext reserveProcess)
        //{
        //    if(reservedProcess.TryAdd(reserveProcess, 1000))
        //    {
        //        return reservedProcess.Count;
        //    }

        //    return -1;
        //}
        public void Init()
        {

        }
        public void Run(int milisecPeriod)
        {
            try
            {
                while (!Terminate)
                {
                    currentRunningProcess = reservedProcess.Take();
                    currentRunningProcess.ApplicationContext = ApplicationContext;

                    if (currentRunningProcess.AsyncMode)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                        {
                            currentRunningProcess.Execute();
                        }));
                    }
                    else
                    {
                        currentRunningProcess.Execute();//(string command, object args, bool isAsyncMode)
                    }                  

                    Thread.Sleep(milisecPeriod);
                }
            }
            catch (Exception e)
            {
                logger.Fatal(e.Message, e);
            }
            finally
            {

            }
        }

        public void Dispose()
        {
            Terminate = true;
        }
    }
}
