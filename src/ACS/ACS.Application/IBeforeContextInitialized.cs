using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Application
{
    public interface IBeforeContextInitialized
    {
        void Execute(Executor executor);
    }
}
