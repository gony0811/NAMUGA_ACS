using ACS.Core.Message.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public abstract class AbstractTibrvCmListener : AbstractTibrvListener
    {
        //private CMQueueTransport cmQueueTransport;
        private CMTransport cmQueueTransport;
        private int workerWeight = 1;
        private int workerTasks = 1;
        private int schedulerWeight = 1;
        private double schedulerHeartbeat = 1.0D;
        private double schedulerActivation = 3.5D;

        private CMListener instance = null;

        //public CMQueueTransport CmQueueTransport
        public CMTransport CmQueueTransport
        {
            get
            {
                return this.cmQueueTransport;
            }
            set
            {
                this.cmQueueTransport = value;
            }
        }


        public int WorkerWeight
        {
            get
            {
                return this.workerWeight;
            }
            set
            {
                this.workerWeight = value;
            }
        }


        public int WorkerTasks
        {
            get
            {
                return this.workerTasks;
            }
            set
            {
                this.workerTasks = value;
            }
        }


        public int SchedulerWeight
        {
            get
            {
                return this.schedulerWeight;
            }
            set
            {
                this.schedulerWeight = value;
            }
        }

        public double SchedulerHeartbeat
        {
            get
            {
                return this.schedulerHeartbeat;
            }
            set
            {
                this.schedulerHeartbeat = value;
            }
        }

        public double SchedulerActivation
        {
            get
            {
                return this.schedulerActivation;
            }
            set
            {
                this.schedulerActivation = value;
            }
        }

        public override void createListener()
        {
            string queueName = "CMQ" + ((QueueDestination)Destination).Name.Replace("*", "ALL");

            NetTransport tibrvTransport = Transport.TibrvTransport;

            //if (!tibrvTransport.Valid)
            if (tibrvTransport == null)
            {
                tibrvTransport = Transport.createTransport();
            }
            this.cmQueueTransport = new CMQueueTransport(tibrvTransport, 
                queueName, (uint)this.workerWeight, (uint)this.workerTasks, (ushort)this.schedulerWeight, this.schedulerHeartbeat, this.schedulerActivation);
            //Queue queue = null;
            //CMTransport eueTransport = new CMTransport(tibrvTransport, queueName, true);
            //this.cmQueueTransport.Description = "cmListener{" + queueName + "}";

            //new CMListener(((QueueDestination)this.destination).TibrvQueue, this, this.cmQueueTransport, ((QueueDestination)this.destination).Name, null);
            //Queue dqueue = new Queue();
            //new CMListener(dqueue, eueTransport, ((QueueDestination)this.destination).Name, null);

            //new CMListener(((QueueDestination)this.destination).TibrvQueue, this.cmQueueTransport, ((QueueDestination)this.destination).Name, null);
            //new CMListener(((QueueDestination)this.Destination).TibrvQueue, this.cmQueueTransport, ((QueueDestination)this.Destination).Name, null).MessageReceived
            //    += new MessageReceivedEventHandler(onMsg);

            instance = new CMListener(((QueueDestination)this.Destination).TibrvQueue, onMsg, this.cmQueueTransport, ((QueueDestination)this.Destination).Name, null);

            instance.SetExplicitConfirmation();

             //= new CMListener(((QueueDestination)this.Destination).TibrvQueue, this.cmQueueTransport, ((QueueDestination)this.Destination).Name, null);
            logger.Info("succeeded in creating cmlistener, destination{" + ((QueueDestination)this.destination).Name + "}, workerWeight{" + this.workerWeight + "}, workerTasks{" + this.workerTasks + "}, schedulerWeight{" + this.schedulerWeight + "}, schedulerHeartbeat{" + this.schedulerHeartbeat + "}, schedulerActivation{" + this.schedulerActivation + "}");
        }

        public override void close()
        {
            base.close();
            //if ((this.cmQueueTransport != null) && (this.cmQueueTransport.Valid))
            if(this.cmQueueTransport != null)
            {
                this.cmQueueTransport.Destroy();

                logger.Info("succeeded in destroying cmQueueTransport, " + this);
                //logger.fine("succeeded in destroying cmQueueTransport, " + this);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("tibrvCmListener{");
            sb.Append("destination=").Append(this.destination);
            sb.Append(", transport=").Append(this.transport);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", useDataField=").Append(this.useDataField);
            sb.Append(", dataFieldName=").Append(this.dataFieldName);
            sb.Append(", open=").Append(this.open);
            sb.Append(", dataFormat=").Append(this.DataFormat).Append("{").Append(GetDataFormatTostring()).Append("}");
            sb.Append(", msbConverter=").Append(this.msbConverter);
            sb.Append(", workerWeight=").Append(this.workerWeight);
            sb.Append(", workerTasks=").Append(this.workerTasks);
            sb.Append(", schedulerWeight=").Append(this.schedulerWeight);
            sb.Append(", schedulerHeartbeat=").Append(this.schedulerHeartbeat);
            sb.Append(", schedulerActivation=").Append(this.schedulerActivation);
            sb.Append("}");

            return sb.ToString();
        }

        //public override void onMessage(XmlDocument paramDocument, String paramString) { }
        //public override void onMessage(String paramString1, String paramString2, XmlDocument paramDocument) { }
        //public override void onMessage(AbstractMessage paramAbstractMessage) { }
        //public override void onAwakeMessage(String paramString1, String paramString2, XmlDocument paramDocument) { }
        //public override void onAwakeMessage(AbstractMessage paramAbstractMessage) { }
    }

}
