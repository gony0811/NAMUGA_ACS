using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public class CmTransport : Transport
    {
        private CMTransport cmTransport;
        private string cmName;
        private int workerWeight = 1;
        private int workerTasks = 1;
        private int schedulerWeight = 1;
        private double schedulerHeartbeat = 1.0D;
        private double schedulerActivation = 3.5D;

        public virtual string CmName
        {
            get
            {
                return this.cmName;
            }
            set
            {
                this.cmName = value;
            }
        }

        public virtual CMTransport getCmTransport()
        {
            return this.cmTransport;
        }

        public virtual int WorkerWeight
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


        public virtual int WorkerTasks
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

        public virtual int SchedulerWeight
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


        public virtual double SchedulerHeartbeat
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


        public virtual double SchedulerActivation
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

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void init() throws com.tibco.tibrv.TibrvException
        public virtual void init()
        {
            base.init();

            this.cmTransport = new CMQueueTransport(TibrvTransport, this.cmName, (uint)this.workerWeight, (uint)this.workerTasks, (ushort)this.schedulerWeight, this.schedulerHeartbeat, this.schedulerActivation);

            //this.cmTransport.Description = "cmListener{" + this.cmName + "}";

            logger.Info("created cmtransport{" + this.cmName + "}, workerWeight{" + this.workerWeight + "}, workerTasks{" + this.workerTasks + "}, schedulerWeight{" + this.schedulerWeight + "}, schedulerHeartbeat{" + this.schedulerHeartbeat + "}, schedulerActivation{" + this.schedulerActivation + "}");
        }

    }

}
