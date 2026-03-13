using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace ACS.Application
{
    public class ApplicationEventListener
    {
        public AfterContextInitialized AfterContextInitialized { get; set; }


        public void HandleApplicationEvent(object sender, AfterContextInitializedEventArg arg)
        {
            if(arg is AfterContextInitializedEventArg)
            {
                if(this.AfterContextInitialized == null)
                {
                    this.AfterContextInitialized = new AfterContextInitialized();
                }

                //try
                {
                     AfterContextInitializedEventArg afterContextInitializedEventArg = (AfterContextInitializedEventArg)arg;
                    this.AfterContextInitialized.AfterContextInitComplete((Executor)sender , afterContextInitializedEventArg.LifetimeScope);                      
                }
                //catch(Exception e)
                //{
                //    throw e;
                //}
            }
        }
    }
}
