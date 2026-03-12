using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Context;
using Spring.Context.Events;

namespace ACS.Application
{
    public class ApplicationEventListener : IApplicationEventListener
    {
        public AfterContextInitialized AfterContextInitialized { get; set; }


        public void HandleApplicationEvent(object sender, ApplicationEventArgs arg)
        {
            if((arg is ContextRefreshedEventArgs))
            {

            }
            else if(arg is ContextClosedEventArgs)
            {
               
            }
            else
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
                        this.AfterContextInitialized.AfterContextInitComplete((Executor)sender , afterContextInitializedEventArg.ApplicationContext);                      
                    }
                    //catch(Exception e)
                    //{
                    //    throw e;
                    //}
                }
            }
        }
    }
}
