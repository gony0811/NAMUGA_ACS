using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.Convert.Mapping
{
    public class SendMessageMapping
    {
        public static string XPATH_NAME_SEND_MESSAGE = "//send";
        public static string NAME = "name";
        public static string CLASS = "class";
        public static string METHOD = "method";
        public static string HOSTMESSAGENAME = "host-message-name";
        public static string USE = "use";
        private string name;
        private string className;
        private string methodName;
        private string hostMessageName;
        private bool use;

        public SendMessageMapping(string name, string className, string methodName, string hostMessageName, bool use)
        {
            this.name = name;
            this.className = className;
            this.methodName = methodName;
            this.hostMessageName = hostMessageName;
            this.use = use;
        }

        public string Name { get {return name;} set { name = value; }}
        public string ClassName { get {return className;} set { className = value; }}
        public string MethodName { get {return methodName;} set { methodName = value; }}
        public string HostMessageName { get {return hostMessageName;} set { hostMessageName = value; }}
        public bool Use { get { return use; } set { use = value; } }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("eventMapping{");
            sb.Append("name=").Append(this.name);
            sb.Append(", className=").Append(this.className);
            sb.Append(", methodName=").Append(this.methodName);
            sb.Append(", hostMessageName=").Append(this.hostMessageName);
            sb.Append(", use=").Append(this.use);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
