using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ACS.Core.Logging.Model;

namespace ACS.Core.Logging
{
    public class LargeLogMessageTask
    {
        public ILogManager LogManager { get; set; }
        public string Text { get; set; }
        public LogMessage LogMessage { get; set; }
        public bool UseFirstIterationLargeLogMessage { get; set; }

        public LargeLogMessageTask(ILogManager logManager, LogMessage logMessage, string text)
        {
            //this.LogManager = logManager;
            //this.LogMessage = logMessage;

            //this.Text = text;

            //if((logManager is LogManagerImpl))
            //{
            //    this.UseFirstIterationLargeLogMessage = ((LogManagerImpl)this.LogManager).
            //}
        }

    }
}
