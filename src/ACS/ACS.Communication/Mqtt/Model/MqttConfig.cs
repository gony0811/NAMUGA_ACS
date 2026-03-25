using ACS.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// MQTT 브로커 접속 설정 런타임 모델 (NA_C_MQTT 테이블)
    /// </summary>
    public class MqttConfig : NamedEntity
    {
        private string applicationName;
        private string workflowManagerName;
        private string brokerIp;
        private int brokerPort = 1883;
        private string topicPrefix = "amr/";
        private string clientId;
        private string userName;
        private string password;
        private int keepAliveSeconds = 30;
        private int reconnectDelayMs = 5000;
        private string state = STATE_LOADED;

        public const string STATE_LOADED = "LOADED";
        public const string STATE_CONNECTING = "CONNECTING";
        public const string STATE_CONNECTED = "CONNECTED";
        public const string STATE_CLOSED = "CLOSED";
        public const string STATE_DISCONNECTED = "DISCONNECTED";

        public virtual string ApplicationName
        {
            get { return this.applicationName; }
            set { this.applicationName = value; }
        }

        public virtual string WorkflowManagerName
        {
            get { return this.workflowManagerName; }
            set { this.workflowManagerName = value; }
        }

        public virtual string BrokerIp
        {
            get { return this.brokerIp; }
            set { this.brokerIp = value; }
        }

        public virtual int BrokerPort
        {
            get { return this.brokerPort; }
            set { this.brokerPort = value; }
        }

        public virtual string TopicPrefix
        {
            get { return this.topicPrefix; }
            set { this.topicPrefix = value; }
        }

        public virtual string ClientId
        {
            get { return this.clientId; }
            set { this.clientId = value; }
        }

        public virtual string UserName
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

        public virtual string Password
        {
            get { return this.password; }
            set { this.password = value; }
        }

        public virtual int KeepAliveSeconds
        {
            get { return this.keepAliveSeconds; }
            set { this.keepAliveSeconds = value; }
        }

        public virtual int ReconnectDelayMs
        {
            get { return this.reconnectDelayMs; }
            set { this.reconnectDelayMs = value; }
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
            sb.Append("mqttConfig{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", state=").Append(this.state);
            sb.Append(", brokerIp=").Append(this.brokerIp);
            sb.Append(", brokerPort=").Append(this.brokerPort);
            sb.Append(", topicPrefix=").Append(this.topicPrefix);
            sb.Append(", clientId=").Append(this.clientId);
            sb.Append(", applicationName=").Append(this.applicationName);
            sb.Append(", workflowManagerName=").Append(this.workflowManagerName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
