using ACS.Framework.Application;
using ACS.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public class Transport
    {
        //protected internal static readonly Logger logger = Logger.getLogger(typeof(Transport));
        //protected internal LogManager logManager;

        public Logger logger = Logger.GetLogger(typeof(Transport));
        public ILogManager LogManager { get; set; }
        private NetTransport tibrvTransport;
        private TransportParameter transportParameter;
        private TransportParameter secondaryTransportParameter;
        private string transportParameterName;
        public const string TRANSPORTPARAMETER_PRIMARY = "PRIMARY";
        public const string TRANSPORTPARAMETER_SECONDARY = "SECONDARY";
        private string description;

        //public virtual LogManager LogManager
        //{
        //    get
        //    {
        //        return this.logManager;
        //    }
        //    set
        //    {
        //        this.logManager = value;
        //    }
        //}


        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }


        public TransportParameter TransportParameter
        {
            get
            {
                return this.transportParameter;
            }
            set
            {
                this.transportParameter = value;
            }
        }


        public TransportParameter SecondaryTransportParameter
        {
            get
            {
                return this.secondaryTransportParameter;
            }
            set
            {
                this.secondaryTransportParameter = value;
            }
        }


        public NetTransport TibrvTransport
        {
            set
            {
                this.tibrvTransport = value;
            }

            get
            {
                return this.tibrvTransport;
            }
        }

        public Transport()
        {
            //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //Console.WriteLine("[" + DateTime.Now.Millisecond + "] " + this.GetType().FullName + " will be created");
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void init() throws TibrvException
        public virtual void init()
        {
            //string time = TimeUtils.TimeToMilliPrettyFormat;
            string time = DateTime.Now.Millisecond.ToString();

            //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //Console.WriteLine("[" + time + "] " + this.GetType().FullName + " will be initialized");

            //if (this.logManager != null)
            //{
            //    logger.LogManager = this.logManager;
            //}
            //else
            //{
            //    //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //    Console.WriteLine("[" + time + "] logManger is not used, because it is not wired at " + this.GetType().FullName);
            //}

            createTransport();

            logger.Info("created transport{service=" + this.transportParameter.Service + ", network=" + this.transportParameter.Network + ", daemon=" + this.transportParameter.Daemon + ", description=" + this.description + ", objectId=" + base.ToString() + "}");
        }

        public string TransportParameterName
        {
            get
            {
                return this.transportParameterName;
            }
            set
            {
                this.transportParameterName = value;
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public TibrvTransport createTransport() throws TibrvException
        public NetTransport createTransport()
        {
            TIBCO.Rendezvous.Environment.Open();

            //string applicationName = applicationName System.getProperty(Executor.SYSTEM_PROPERTY_KEY_ID_VALUE);
            string applicationName = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];

            try
            {
                this.tibrvTransport = new NetTransport(this.transportParameter.Service, this.transportParameter.Network, this.transportParameter.Daemon);

                this.transportParameterName = "PRIMARY";
                this.tibrvTransport.Description = applicationName + "-" + this.description;
            }
            //catch (RendezvousException tibrvException)
            catch (Exception tibrvException)
            {
                if (this.secondaryTransportParameter != null)
                {
                    //logger.fine("default transportParameter can not be applicable, secondary will be used");

                    this.transportParameter = this.secondaryTransportParameter;
                    this.tibrvTransport = new NetTransport(this.transportParameter.Service, this.transportParameter.Network, this.transportParameter.Daemon);

                    this.transportParameterName = "SECONDARY";
                    this.tibrvTransport.Description = applicationName + "-" + this.description;
                }
                else
                {
                    throw tibrvException;
                }
            }
            finally
            {

            }
            return this.tibrvTransport;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("transport{");
            if (this.transportParameter != null)
            {
                sb.Append("service=").Append(this.transportParameter.Service);
                sb.Append(", network=").Append(this.transportParameter.Network);
                sb.Append(", daemon=").Append(this.transportParameter.Daemon);
            }
            sb.Append(", description=").Append(this.description);
            sb.Append(", objectId=").Append(base.ToString());
            sb.Append("}");
            return sb.ToString();
        }
    }
}
