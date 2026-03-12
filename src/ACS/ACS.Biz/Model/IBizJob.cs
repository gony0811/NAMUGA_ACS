using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Context;
using ACS.Framework.Logging;

namespace ACS.Biz
{
    public abstract class BaseBizJob : IBizJob
    {
        private IApplicationContext applicationContext;
        public Logger Logger = Logger.GetLogger("BUSINESS_PROCESS");

        public BaseBizJob()
        {
            //_applicationContext = ContextRegistry.GetContext();
        }

        //public IApplicationContext ApplicationContext { get { return _applicationContext; } set { _applicationContext = value; } }


        public IApplicationContext ApplicationContext
        {
            get
            {
                return applicationContext;
            }
            set
            {
                applicationContext = value;
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
        IApplicationContext ApplicationContext { get; set; }
        int ExecuteJob(Object[] args);
    }
}
