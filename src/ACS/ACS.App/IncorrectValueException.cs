using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.App
{
    public class IncorrectValueException : Exception
    {
        public IncorrectValueException(string message)
            : base(message)
        {
            
        }
    }
}
