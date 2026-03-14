using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Utility
{
    public class PerformCommandException : Exception
    {
        public int ExitValue { get; set; }
        public string[] CommandAttributes { get; set; }

        public PerformCommandException(string message, int exitValue, string[] commandAttributes)
        : base(message)
        {
            this.ExitValue = exitValue;
            this.CommandAttributes = commandAttributes;
        }

        public PerformCommandException(string message, string[] commandAttributes)
        : base(message)
        {
            this.CommandAttributes = commandAttributes;
        }
    }
}
