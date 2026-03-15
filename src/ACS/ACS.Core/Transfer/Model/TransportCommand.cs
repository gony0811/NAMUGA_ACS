using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Transfer.Model
{
    public class TransportCommand : Command
    {
        public static String STATE_ALTERNATED = "ALTERNATED";
        public static String STATE_CANCELED = "CANCELED";
        public static String STATE_ABORTED = "ABORTED";
        public static String STATE_PAUSED = "PAUSED";
        public static String STATE_TERMINATED = "TERMINATED";
        public static String STATE_FAILED = "FAILED";
        public static String STATE_CANCELING = "CANCELING";
        public static String STATE_ABORTING = "ABORTING";
        public static String STATE_PAUSING = "PAUSING";
        public static String STATE_CANCELSTARTED = "CANCELSTARTED";
        public static String STATE_ABORTSTARTED = "ABORTSTARTED";
        public static String STATE_CANCELRESERVED = "CANCELRESERVED";
        public static String STATE_ABORTRESERVED = "ABORTRESERVED";
        public static String STATE_TRANSFERFAILED = "TRANSFERFAILED";
        public static String STATE_CANCELFAILED = "CANCELFAILED";
        public static String STATE_ABORTFAILED = "ABORTFAILED";
        public static String STATE_QUEUED_NUM = "1";
        public static String STATE_TRANSFERRING_NUM = "2";
        public static String STATE_PAUSED_NUM = "3";
        public static String STATE_CANCELING_NUM = "4";
        public static String STATE_ABORTING_NUM = "5";
        private String transportJobId = "";
        private String transportCommandId = "";
        private String state = "CREATED";
        private String previousState = "NA";
        private String priority = "50";
        private int currentRetryCount = 0;
        private DateTime stateChangedTime { get; set; }
        private String isPriorityBoostUp = "F";
        private String newPriority = "";
        private String additionalInfo = "";

        public TransportCommand()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public TransportCommand(bool pure)
        {
            if (pure)
            {
                this.CarrierName = null;
                this.FromMachineName = null;
                this.FromUnitName = null;
                this.Id = null;
                this.previousState = null;
                this.priority = null;
                this.state = null;
                this.ToMachineName = null;
                this.ToType = null;
                this.ToUnitName = null;
                this.transportCommandId = null;
                this.transportJobId = null;
                this.TransportMachineName = null;
                this.TransportUnitName = null;
            }
            else
            {
                this.Id = Guid.NewGuid().ToString();
            }
        }

        public string TransportJobId { get { return transportJobId;} set { transportJobId = value; } }

        public string TransportCommandId { get { return transportCommandId; } set { transportCommandId = value; } }

        public string State { get { return state; } set { state = value; } }

        public string PreviousState { get { return previousState; } set { previousState = value; } }

        public string Priority { get { return priority; } set { priority = value; } }

        public int CurrentRetryCount { get { return currentRetryCount; } set { currentRetryCount = value; } }

        public DateTime StateChangedTime { get { return stateChangedTime; } set { stateChangedTime = value; } }

        public string IsPriorityBoostUp { get { return isPriorityBoostUp; } set { isPriorityBoostUp = value; } }

        public string NewPriority { get { return newPriority; } set { newPriority = value; } }

        public string AdditionalInfo { get { return additionalInfo; } set { additionalInfo = value; } }

        public string getStateToNum()
        {
            String StateNum = "0";
            if (this.state.Equals("QUEUED"))
            {
                StateNum = "1";
            }
            else if (this.state.Equals("TRANSFERRING"))
            {
                StateNum = "2";
            }
            else if (this.state.Equals("PAUSED"))
            {
                StateNum = "3";
            }
            else if (this.state.Equals("CANCELING"))
            {
                StateNum = "4";
            }
            else if (this.state.Equals("ABORTING"))
            {
                StateNum = "5";
            }
            return StateNum;
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("transportCommand{");
            sb.Append("transportCommandId=").Append(this.transportCommandId);
            sb.Append(", transportJobId=").Append(this.transportJobId);
            sb.Append(", carrierName=").Append(this.CarrierName);
            sb.Append(", commandMachineName=").Append(this.CommandMachineName);
            sb.Append(", transportMachineName=").Append(this.TransportMachineName);
            sb.Append(", transportUnitName=").Append(this.TransportUnitName);
            sb.Append(", fromManchineName=").Append(this.FromMachineName);
            sb.Append(", fromUnitName=").Append(this.FromUnitName);
            sb.Append(", fromContainerName=").Append(this.FromContainerName);
            sb.Append(", fromSlotNumber=").Append(this.FromSlotNumber);
            sb.Append(", toMachineName=").Append(this.ToMachineName);
            sb.Append(", toUnitName=").Append(this.ToUnitName);
            sb.Append(", toContainerName=").Append(this.ToContainerName);
            sb.Append(", toSlotNumber=").Append(this.ToSlotNumber);
            sb.Append(", toType=").Append(this.ToType);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", state=").Append(this.state);
            sb.Append(", previousState=").Append(this.previousState);
            sb.Append(", isPriorityBoostUp=").Append(this.isPriorityBoostUp);
            sb.Append(", newPriority=").Append(this.newPriority);
            sb.Append(", additionalInfo=").Append(this.additionalInfo);
            sb.Append(", stateChangedTime=").Append(this.stateChangedTime);
            sb.Append(", createTime=").Append(this.CreateTime);
            sb.Append(", creator=").Append(this.Creator);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", editor=").Append(this.Editor);
            sb.Append(", description=").Append(this.Description);
            sb.Append(", id=").Append(this.Id);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
