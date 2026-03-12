using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public abstract class AbstractTibrvDqListener : AbstractTibrvListener
    {
        private CMQueueTransport cmQueueTransport;
        private int workerWeight = 1;
        private int workerTasks = 1;
        private int schedulerWeight = 1;
        private double schedulerHeartbeat = 1.0D;
        private double schedulerActivation = 3.5D;

        private Listener instance = null;

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
            
            if (tibrvTransport == null)
            {
                tibrvTransport = Transport.createTransport();
            }
            this.cmQueueTransport = new CMQueueTransport(tibrvTransport, queueName, (uint)this.workerWeight, (uint)this.workerTasks, (ushort)this.schedulerWeight, this.schedulerHeartbeat, this.schedulerActivation);

            //this.cmQueueTransport.Description = "cmListener{" + queueName + "}";

            //new Listener(((QueueDestination)this.destination).TibrvQueue, this, this.cmQueueTransport, ((QueueDestination)this.destination).Name, null);
            //new Listener(((QueueDestination)this.destination).TibrvQueue, this.cmQueueTransport, ((QueueDestination)this.destination).Name, null);
            instance = new Listener(((QueueDestination)this.Destination).TibrvQueue, onMsg,this.cmQueueTransport, ((QueueDestination)this.Destination).Name, null);

            logger.Info("succeeded in creating listener, destination{" + ((QueueDestination)this.destination).Name + "}, workerWeight{" + this.workerWeight + "}, workerTasks{" + this.workerTasks + "}, schedulerWeight{" + this.schedulerWeight + "}, schedulerHeartbeat{" + this.schedulerHeartbeat + "}, schedulerActivation{" + this.schedulerActivation + "}");
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

            sb.Append("tibrvDqListener{");
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

    }

}
