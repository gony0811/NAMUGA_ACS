using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Logging;
using RabbitMQ.Client;

namespace ACS.Communication.Msb.RabbitMQ
{
    public class AbstractRabbitMQ : AbstractMsb
    {
        public Logger logger = Logger.GetLogger(typeof(AbstractMsb));

        //simplist unicast 1:1
        public const string CASTOPTION_SIMPLECAST = "SIMPLECAST";
        //Distributing tasks among workers (the competing consumers pattern) 
        public const string CASTOPTION_WORKQUEUES = "WORKQUEUES";
        //Sending messages to many consumers at once 
        public const string CASTOPTION_PUBLISHSUBSCRIBE = "PUBLISHSUBSCRIBE";
        //Receiving messages selectively 
        public const string CASTOPTION_ROUTING = "ROUTING";
        //Receiving messages based on a pattern (topics) 
        public const string CASTOPTION_TOPICS = "TOPICS";
        //Request/reply pattern example 
        public const string CASTOPTION_RPC_SERVER = "RPCSERVER";
        public const string CASTOPTION_RPC_CLIENT = "RPCCLIENT";
        //Reliable publishing with publisher confirms 
        public const string CASTOPTION_PUBLISHERCONFIRMS = "PUBLISHERCONFIRMS";

        public const string CASTOPTION_UNICAST = "UNICAST";
        public const string CASTOPTION_MULTICAST = "MULTICAST";
        public const string CASTOPTION_GUARANTED_UNICAST = "GUNICAST";
        public const string CASTOPTION_GUARANTED_MULTICAST = "GMULTICAST";
        public const string STATIONMODE_INTERSTATION = "INTER";
        public const string STATIONMODE_INNERSTATION = "INNER";
        public const string DELIVERYMODE_PUSH = "PUSH";
        public const string DELIVERYMODE_PULL = "PULL";
        public const string PRIMARY_MESSAGE = "PRIMARYMESSAGE";

        // Summary:
        //     The host to connect to.
        private string hostName;

        private string queueName;
        //First, we need to make sure that the queue will survive a RabbitMQ node restart. 
        //In order to do so, we need to declare it as durable:
        private bool durable;

        private bool exclusive;

        private bool autoDelete;

        private IDictionary<string, object> arguments;

        //Quality of Service options [BasicQoS()]
        private int prefetchSize;
        private int prefetchCount;
        private bool global;
        private bool persistent;
        private string castOption;
        private long defaultTTL;
        private string userName;
        private string password;

        public IConnection Connection { get; set; }
        public IModel Session { get; set; }
        public string HostName { get { return hostName; } set { hostName = value; } }
        public string UserName { get { return userName; } set { userName = value; } }
        public string Password { get { return password; } set { password = value; } }
        public string QueueName { get { return queueName; } set { queueName = value; } }
        public bool Durable { get { return durable; } set { durable = value; } }
        public bool Exclusive { get { return exclusive; } set { exclusive = value; } }
        public bool AutoDelete { get { return autoDelete; } set { autoDelete = value; } }
        public bool Persistent { get { return persistent; } set { persistent = value; } }
        public string CastOption { get { return castOption; } set { castOption = value; } }

        public string StationMode { get; set; }

        public long DefaultTTL { get { return defaultTTL; } set { defaultTTL = value; } }

        public IDictionary<string, object> Arguments { get { return arguments; } set { arguments = value; } }

        public ILogManager LogManager { get; set; }

        public override void Init()
        {
            logger.logManager = LogManager;
            prefetchCount = 1;
            prefetchSize = 0;
            global = false;
            defaultTTL = 60000L;
        }

        public IModel CreateSession()
        {
            IModel session = null;

            try
            {
                var factory = new ConnectionFactory();
                factory.UserName = UserName;
                factory.Password = Password;

                var endpoints = new System.Collections.Generic.List<AmqpTcpEndpoint> { 
                    new AmqpTcpEndpoint("hostname"),
                    new AmqpTcpEndpoint(HostName)
                };
                Connection = factory.CreateConnection(endpoints);
                session = Connection.CreateModel();

                Arguments = new Dictionary<String, Object>();
                Arguments.Add("message-ttl", defaultTTL);
            }
            catch (Exception e)
            {
                logger.Error(
                    $"[ACS] RabbitMQ 연결 실패 (Host: {HostName}, User: {UserName}). " +
                    $"RabbitMQ 서버가 실행 중인지 확인하세요. " +
                    $"시작 명령: 'rabbitmqctl start_app' 또는 'brew services start rabbitmq' (macOS) / 'systemctl start rabbitmq-server' (Linux)", e);
                throw new ApplicationException(
                    $"[ACS] RabbitMQ 서버에 연결할 수 없습니다. (Host: {HostName}) " +
                    $"RabbitMQ가 실행 중인지 확인하세요. " +
                    $"시작 명령: 'rabbitmqctl start_app' 또는 'brew services start rabbitmq' (macOS) / 'systemctl start rabbitmq-server' (Linux)", e);
            }

            return session;
        }

    }
}
