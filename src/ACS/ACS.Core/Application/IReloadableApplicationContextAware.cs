using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace ACS.Core.Application
{
    public interface IReloadableApplicationContextAware
    {
        void SetApplicationContext(ILifetimeScope lifetimeScope);
    }
}
