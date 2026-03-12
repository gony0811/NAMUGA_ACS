using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Application
{
    public class ApplicationNotStartedException : Exception
    {
        public ApplicationNotStartedException(string msg)
            : base(msg)
        {

        }
    }
}
