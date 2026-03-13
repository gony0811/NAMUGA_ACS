using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace ACS.Application
{
    public class AfterContextInitializedEventArg : EventArgs
    {
        public AfterContextInitializedEventArg(object source, ILifetimeScope lifetimeScope)
        {           
            // TODO: Complete member initialization
            this.Source = source;
            this.LifetimeScope = lifetimeScope;
        }
        public ILifetimeScope LifetimeScope { get; private set; }
        public object Source { get; private set; }
    }
}
