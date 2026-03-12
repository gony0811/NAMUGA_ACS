using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Control
{
    public class ControlMessageEx : ControlMessage
    {
        public static string MESSAGENAME_CONTROL_STARTNIOCONTROLLABLE = "CONTROL-STARTNIOCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_STOPNIOCONTROLLABLE = "CONTROL-STOPNIOCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_LOADNIOCONTROLLABLE = "CONTROL-LOADNIOCONTROLLABLE";
        public static string MESSAGENAME_CONTROL_UNLOADNIOCONTROLLABLE = "CONTROL-UNLOADNIOCONTROLLABLE";
        public static string NAME_NIONAME = "NIONAME";
        public static string XPATH_NAME_NIONAME = "/MESSAGE/DATA/NIONAME";

        private string nioName;

        public string NioName
        {
            get { return nioName; }
            set { nioName = value; }
        }
        public object ReceivedMessage { get { return base.ReceivedMessage; } set { base.ReceivedMessage = value; } }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("message{");
            sb.Append("messageName=").Append(this.MessageName);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", applicationType=").Append(this.applicationType);
            sb.Append(", nioName=").Append(this.nioName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
