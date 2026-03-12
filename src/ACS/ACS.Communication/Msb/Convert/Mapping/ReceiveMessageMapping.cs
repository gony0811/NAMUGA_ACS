using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.Convert.Mapping
{
    public class ReceiveMessageMapping
    {
        public static string XPATH_NAME_RECEIVE_MESSAGE = "//receive";
        public static string NAME = "name";
        public static string CLASS = "class";
        public static string HOSTMESSAGENAME = "host-message-name";
        public static string USE = "use";

        private string name;
        private string className;
        private string hostMessageName;
        private bool use;

        public ReceiveMessageMapping(string name, string className, string hostMessageName, bool use)
        {
            this.name = name;
            this.className = className;
            this.hostMessageName = hostMessageName;
            this.use = use;
        }

        public string Name { get {return name;} set { name = value; }}
        public string ClassName { get {return className;} set { className = value; }}
        public string HostMessageName { get {return hostMessageName;} set { hostMessageName = value; }}
        public bool Use { get { return use; } set { use = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("commandMapping{");
            sb.Append("name=").Append(this.name);
            sb.Append(", className=").Append(this.className);
            sb.Append(", hostMessageName=").Append(this.hostMessageName);
            sb.Append(", use=").Append(this.use);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
