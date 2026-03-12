using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Quartz;
using ACS.Framework.Logging;

namespace ACS.Framework.Scheduling.Model
{
    public abstract class AbstractJob : IJob
    {
        public Logger logger = Logger.GetLogger(typeof(AbstractJob));

        public void Sleep(int millis)
        {
            try
            {
                Thread.Sleep(millis);
            }
            catch (ThreadInterruptedException e)
            {
                logger.Error(e.Message, e);
            }
        }

        protected void ExcuteInternal(JobExecutionContext context)
        {
            try
            {
                Execute(context);
            }
            catch(Exception e)
            {
                logger.Error(e.Message, e);
            }
        }

        public virtual void Execute(JobExecutionContext context)
        {

        }
    }
}
