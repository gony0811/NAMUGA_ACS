using ACS.Communication.Msb;
using ACS.Communication.Msb.Highway101;
using ACS.Core.Application;
using ACS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public class QueueDestination : ChannelDestination
    {
        public Logger logger = Logger.GetLogger(typeof(QueueDestination));
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

        //public override string Name
        //{
        //    get
        //    {
        //        return this.name;
        //    }
        //    set
        //    {
        //        this.name = value;
        //    }
        //}


        public Queue TibrvQueue { get; private set; }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public QueueDestination() throws com.tibco.tibrv.TibrvException
        public QueueDestination()
        {
            

            //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //Console.WriteLine("[" + DateTime.Now.Millisecond + "] " + this.GetType().FullName + " will be created");
            createTibrvQueue();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public QueueDestination(String name) throws com.tibco.tibrv.TibrvException
        public QueueDestination(string name)
        {
            //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //Console.WriteLine("[" + DateTime.Now.Millisecond + "] " + this.GetType().FullName + "{" + name + "} will be created");
            this.Name = name;

            createTibrvQueue();
        }

        public override void Init()
        {
            //string time = DateTime.Now.Millisecond.ToString();

            string time = DateTime.UtcNow.ToLongDateString();

            //1PC 2ACS Test
            this.Name = this.Name.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);

            this.Name = this.Name.Replace(".", "/");

            if (!this.Name.StartsWith(@"/"))
            {
                this.Name = @"/" + this.Name;
            }


            //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //Console.WriteLine("[" + time + "] " + this.GetType().FullName + "{" + this.name + "} will be initialized");

            //if (this.logManager != null)
            //{
            //    logger.LogManager = this.logManager;
            //}
            //else
            //{
            //    //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //    Console.WriteLine("[" + time + "] logManger is not used, because it is not wired at " + this.GetType().FullName);
            //}
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void createTibrvQueue() throws com.tibco.tibrv.TibrvException
        public void createTibrvQueue()
        {
            TIBCO.Rendezvous.Environment.Open();

            //if ((this.tibrvQueue == null) || (!this.tibrvQueue.Valid))
            try
            {
                if (this.TibrvQueue == null)
                {
                    this.TibrvQueue = new Queue();
                }
            }
            catch (RendezvousException exception)
            {
                Console.Error.WriteLine("Failed to create Queue:");
                Console.Error.WriteLine(exception.StackTrace);
                System.Environment.Exit(1);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("queueDestination{");
            sb.Append("name=").Append(this.Name);
            sb.Append("}");
            return sb.ToString();
        }

    }
}


