using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Application;

namespace ACS.Application
{
    public class BeforeContextInitializedImplement : IBeforeContextInitialized
    {
        public void Execute(Executor executor)
        {
            if (executor.Msb.Equals("tibrv"))
            {
                //OpenTibrv(executor);
            }
        }
    }
}
