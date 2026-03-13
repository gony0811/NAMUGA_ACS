using com.miracom.transceiverx;
using com.miracom.transceiverx.session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Msb;
using ACS.Framework.Logging;

namespace ACS.Communication.Msb.Highway101
{
    public class AbstractHighway101 : AbstractMsb
    {      
        public Logger logger = Logger.GetLogger(typeof(AbstractMsb));
        public const string CASTOPTION_UNICAST = "UNICAST";
        public const string CASTOPTION_MULTICAST = "MULTICAST";
        public const string CASTOPTION_GUARANTED_UNICAST = "GUNICAST";
        public const string CASTOPTION_GUARANTED_MULTICAST = "GMULTICAST";
        public const string STATIONMODE_INTERSTATION = "INTER";
        public const string STATIONMODE_INNERSTATION = "INNER";
        public const string DELIVERYMODE_PUSH = "PUSH";
        public const string DELIVERYMODE_PULL = "PULL";
        public const string PRIMARY_MESSAGE = "PRIMARYMESSAGE";

        private string connectUrl;
        private string sessionId;
        public string castOption;
        private string stationMode;
        private string deliveryMode;
        private long pullTimeout = 10000;
        private bool autoRecoveryOption;
        private long defaultTTL = 60000;
        private PullingThread pullingThread;

        public string ConnectUrl { get {return connectUrl;} set { connectUrl = value; }}
        public string SessionId { get {return sessionId;} set { sessionId = value; }}
        public string CastOption { get {return castOption;} set { castOption = value; }}
        public string StationMode { get {return stationMode;} set { stationMode = value; }}
        public string DeliveryMode { get {return deliveryMode;} set { deliveryMode = value; }}
        public long PullTimeout { get {return pullTimeout;} set { pullTimeout = value; }}
        public bool AutoRecoveryOption { get {return autoRecoveryOption;} set { autoRecoveryOption = value; }}
        public long DefaultTTL { get { return defaultTTL; } set { defaultTTL = value; } }
        public PullingThread PullingThread { get {return pullingThread;} set { pullingThread = value; }}
        public ILogManager LogManager { get; set; }

        public override void Init()
        {
            logger.logManager = LogManager;
        }

        public void CreatePullingThread(Session session, AbstractHighway101 abstractHighway101)
        {
            this.pullingThread = new PullingThread(session, abstractHighway101);
            this.pullingThread.Start();
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <returns></returns>
        public Session CreateSession()
        {
            Session session = null;

            try
            {
                if (stationMode.Equals("INNER"))
                {
                    if (deliveryMode.Equals("PULL"))
                        session = Transceiver.createSession(sessionId, 3);
                    else if (deliveryMode.Equals("PUSH"))
                    {
                        session = Transceiver.createSession(sessionId, 2);
                    }
                    else
                    {
                        logger.Error("deliveryMode should be one of {PULL | PUSH}");
                        throw new TrxException("4");
                    }
                }
                else if (stationMode.Equals("INTER"))
                {
                    if (deliveryMode.Equals("PULL"))
                        session = Transceiver.createSession(sessionId, 1);
                    else if (deliveryMode.Equals("PUSH"))
                    {
                        session = Transceiver.createSession(sessionId, 0);
                    }
                    else
                    {
                        logger.Error("deliveryMode should be one of {PULL | PUSH}");
                        throw new TrxException("4");
                    }
                }
                else
                {
                    logger.Error("stationMode should be one of {INNER | INTER}");
                    throw new TrxException("4");
                }

                session.connect(connectUrl);
                session.setAutoRecovery(autoRecoveryOption);
                session.setDefaultTTL(defaultTTL);
            }
            catch (TrxException e)
            {
                logger.Error(e.Message, e);
            }
            finally
            {

            }

            return session;
        }


    }
}
