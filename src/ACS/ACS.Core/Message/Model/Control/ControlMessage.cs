using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Control
{
    public class ControlMessage : AbstractMessage
    {
        public static string MESSAGENAME_CONTROL_HEARTBEAT = "CONTROL-HEARTBEAT";
        public static string MESSAGENAME_CONTROL_RELOADWORKFLOW = "CONTROL-RELOADWORKFLOW";
        public static string MESSAGENAME_CONTROL_RELOADSERVICE = "CONTROL-RELOADSERVICE";
        public static string MESSAGENAME_CONTROL_REFRESHCACHE = "CONTROL-REFRESHCACHE";
        public static string MESSAGENAME_CONTROL_STARTSECSCONTROLLABLE = "CONTROL-STARTSECSCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_STOPSECSCONTROLLABLE = "CONTROL-STOPSECSCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_LOADSECSCONTROLLABLE = "CONTROL-LOADSECSCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_UNLOADSECSCONTROLLABLE = "CONTROL-UNLOADSECSCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_UPDATESECSCONTROLLABLE = "CONTROL-UPDATESECSCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_START = "CONTROL-START";
        public static string MESSAGENAME_CONTROL_STOP = "CONTROL-STOP";
        public static string MESSAGENAME_CONTROL_KILL = "CONTROL-KILL";
        public static string MESSAGENAME_CONTROL_GETPROCESSID = "CONTROL-GETPROCESSID";
        public static string MESSAGENAME_CONTROL_GC = "CONTROL-GC";
        public static string MESSAGENAME_CONTROL_DELETEFILELOG = "CONTROL-DELETEFILELOG";
        public static string MESSAGENAME_CONTROL_SYSTEMCHECK = "CONTROL-SYSTEMCHECK";
        public static string MESSAGENAME_CONTROL_COREDUMP = "CONTROL-COREDUMP";
        public static string NAME_APPLICATIONNAME = "APPLICATIONNAME";
        public static string NAME_APPLICATIONTYPE = "APPLICATIONTYPE";
        public static string NAME_SECSNAME = "SECSNAME";
        public static string XPATH_NAME_APPLICATIONNAME = "/MESSAGE/DATA/APPLICATIONNAME";
        public static string XPATH_NAME_APPLICATIONTYPE = "/MESSAGE/DATA/APPLICATIONTYPE";
        public static string XPATH_NAME_SECSNAME = "/MESSAGE/DATA/SECSNAME";

        protected string applicationName;
        protected string applicationType;
        protected string secsName;

        public string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
        }

        public string ApplicationType
        {
            get { return applicationType; }
            set { applicationType = value; }
        }

        public string SecsName
        {
            get { return secsName; }
            set { secsName = value; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("message{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", applicationType=").Append(this.applicationType);
            sb.Append(", secsName=").Append(this.secsName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
