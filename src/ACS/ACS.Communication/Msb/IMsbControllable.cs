using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication;

namespace ACS.Communication.Msb
{
    public interface IMsbControllable : IControllable
    {
        string GetMsbControllerName();
    }
}
