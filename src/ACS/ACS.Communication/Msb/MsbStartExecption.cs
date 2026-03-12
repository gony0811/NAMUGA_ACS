using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb
{
    public class MsbStartExecption : Exception 
    {
        public MsbStartExecption()
            : base("failed to start msb")
        {
            
        }

        public MsbStartExecption(string message)
            : base(message)
        {
            
        }

    }
}
