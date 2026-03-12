using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Context;
using Spring.Context.Support;
using Spring.Context.Events;

namespace ACS.Application
{
    public class AfterContextInitializedEventArg : ApplicationEventArgs
    {
        public AfterContextInitializedEventArg(object source, IApplicationContext applicationContext)
        {           
            // TODO: Complete member initialization
            this.Source = source;
            this.ApplicationContext = applicationContext;
        }
        public IApplicationContext ApplicationContext { get; private set; }
        public object Source { get; private set; }
    }
}
