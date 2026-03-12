using ACS.Framework.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Model
{
    public class Nio : NamedEntity
    {
        private string applicationName;
        private string interfaceClassName;
        private string workflowManagerName;
        private string machineName;
        private string remoteIp;
        private int port = 7500;
        public const string NIO_STATE_LOADED = "LOADED";
        public const string NIO_STATE_CONNECTING = "CONNECTING";
        public const string NIO_STATE_CONNECED = "CONNECTED";
        public const string NIO_STATE_CLOSED = "CLOSED";
        public const string NIO_STATE_UNLOADED = "UNLOADED";
        public const string NIO_STATE_DISCONNECTED = "DISCONNECTED";
        private string state = "CONNECTING";

        public virtual string InterfaceClassName
        {
            get { return this.interfaceClassName; }
            set { this.interfaceClassName = value; }
        }

        public virtual string WorkflowManagerName
        {
            get { return this.workflowManagerName; }
            set { this.workflowManagerName = value; }
        }

        public virtual string ApplicationName
        {
            get { return this.applicationName; }
            set { this.applicationName = value; }
        }

        public virtual int Port
        {
            get { return this.port; }
            set { this.port = value; }
        }

        public virtual string RemoteIp
        {
            get { return this.remoteIp; }
            set { this.remoteIp = value; }
        }

        public virtual string MachineName
        {
            get { return this.machineName; }
            set { this.machineName = value; }
        }

        public virtual string State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        public virtual string getName()
        {
            return base.Name;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("nio{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", state=").Append(this.state);
            sb.Append(", remoteIp=").Append(this.remoteIp);
            sb.Append(", port=").Append(this.port);
            sb.Append(", editTime=").Append(this.EditTime);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", workflowManagerName=").Append(this.workflowManagerName);
            sb.Append(", interfaceClassName=").Append(this.interfaceClassName);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
