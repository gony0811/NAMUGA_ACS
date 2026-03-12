using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb
{
    public class ConvertException : Exception
    {
        public ConvertException(String messageName, String reason)
            : base("failed to convert message{" + messageName + "}, " + reason)
        {
            
        }
    }
}
