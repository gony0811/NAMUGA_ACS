using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Context;

namespace ACS.Framework.Application
{
    public interface IReloadableApplicationContextAware
    {
        void SetApplicationContext(IApplicationContext applicationContext);
    }
}
