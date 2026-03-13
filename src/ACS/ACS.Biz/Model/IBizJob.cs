using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using ACS.Framework.Logging;

namespace ACS.Biz
{
    public abstract class BaseBizJob : IBizJob
    {
        private ILifetimeScope lifetimeScope;
        public Logger Logger = Logger.GetLogger("BUSINESS_PROCESS");

        public BaseBizJob()
        {
        }

        public ILifetimeScope LifetimeScope
        {
            get
            {
                return lifetimeScope;
            }
            set
            {
                lifetimeScope = value;
            }
        }

        public virtual int ExecuteJob(object[] args)
        {
            return 0;
        }

        public int Execute(object[] args)
        {
            int result = 2;

            try
            {
                result = ExecuteJob(args);
                return result;
            }
            catch(NullReferenceException nullEx)
            {
                Logger.Error(nullEx.StackTrace, nullEx);
                return result;
            }
            catch(Exception ex)
            {
                Logger.Error(ex.StackTrace, ex);
                return result;
            }
        }
    }

    public interface IBizJob
    {
        ILifetimeScope LifetimeScope { get; set; }
        int ExecuteJob(Object[] args);
    }
}
