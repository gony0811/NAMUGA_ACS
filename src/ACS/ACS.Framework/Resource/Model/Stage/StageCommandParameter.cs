using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Resource.Model.Stage
{
    public class StageCommandParameter : TimedEntity
    {
        private string portName = "";
        private string machineName = "";
        private int expectedDuration = 40;
        private int noBlockingTime = 10;
        private int waitTimeOut = 10;
        private int priority = 50;
        private int replace = 0;

        protected string PortName { get { return portName; } set { portName = value; } }
        protected string MachineName { get { return machineName; } set { machineName = value; } }
        protected int ExpectedDuration { get { return expectedDuration; } set { expectedDuration = value; } }
        protected int NoBlockingTime { get { return noBlockingTime; } set { noBlockingTime = value; } }
        protected int WaitTimeOut { get { return waitTimeOut; } set { waitTimeOut = value; } }
        protected int Priority { get { return priority; } set { priority = value; } }
        protected int Replace { get { return replace; } set { replace = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("stageCommandParameter{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", portName=").Append(this.portName);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", expectedDuration=").Append(this.expectedDuration);
            sb.Append(", noBlockingTime=").Append(this.noBlockingTime);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", replace=").Append(this.replace);
            sb.Append(", waitTimeOut=").Append(this.waitTimeOut);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
