using com.miracom.transceiverx;
using com.miracom.transceiverx.message;
using com.miracom.transceiverx.session;
using System.Threading;
using System.Threading.Tasks;
using System;
using ACS.Framework.Logging;

namespace ACS.Communication.Msb.Highway101
{
    public class PullingThread : IDisposable
    {
        public static Logger logger = Logger.GetLogger(typeof(PullingThread));

        private Session session;
        private AbstractHighway101 abstractHighway101;
        private AbstractHighway101Listener highway101Listener;
        private bool isListener = false;
        private bool terminate = false;

        public Thread WorkerThread;

        public Session Session { get {return session;} set { session = value; }}
        public AbstractHighway101 AbstractHighway101 { get {return abstractHighway101;} set { abstractHighway101 = value; }}
        public AbstractHighway101Listener Highway101Listener { get {return highway101Listener;} set { highway101Listener = value; }}
        public bool IsListener { get {return isListener;} set { isListener = value; }}
        public bool Terminate { get { return terminate; } set { terminate = value; } }

        public PullingThread(Session session, AbstractHighway101 abstractHighway101)
        {
            this.session = session;
            this.abstractHighway101 = abstractHighway101;
        }     

        public bool Start()
        {
            Task<bool> task = StartAsync();
            task.Wait();

            return task.Result;
        }

        private Task<bool> StartAsync()
        {
            return Task<bool>.Factory.StartNew(StartProcess);
        }

        private bool StartProcess()
        {
            Stop();

            WorkerThread = CreateWorkerThread();
            //keepRunning = true;
            WorkerThread.Start();

            return true;
        }


        private Thread CreateWorkerThread()
        {
            this.terminate = false;
            Thread thread = new Thread(DoWork);
            thread.IsBackground = true;
            return thread;
        }

        public void Stop()
        {
            this.terminate = true;
            Task task = StopAsync();
            task.Wait();
        }

        private Task StopAsync()
        {
            return Task.Factory.StartNew(StopProcess);
        }

        private void StopProcess()
        {
            //keepRunning = false;
            if (WorkerThread != null && WorkerThread.IsAlive)
                WorkerThread.Join();
        }

        /// <summary>
        /// Receive Message
        /// </summary>
        private void DoWork()
        {
            if ((this.abstractHighway101 is AbstractHighway101Listener))
            {
                this.highway101Listener = ((AbstractHighway101Listener)this.abstractHighway101);
                this.isListener = true;
            }

            while (!this.terminate)
            {
                try
                {
                    com.miracom.transceiverx.message.Message msg = this.session.getMessage(this.abstractHighway101.PullTimeout);
                    if (msg != null)
                    {
                        if (this.isListener)
                        {
                            short delivery_type = msg.getDeliveryMode();
                            if (DeliveryType.isUnicast(delivery_type))
                            {
                                this.highway101Listener.onUnicast(this.session, msg);
                            }
                            else if (DeliveryType.isGuaranteedUnicast(delivery_type))
                            {
                                this.highway101Listener.onGUnicast(this.session, msg);
                            }
                            else if (DeliveryType.isMulticast(delivery_type))
                            {
                                this.highway101Listener.onMulticast(this.session, msg);
                            }
                            else if (DeliveryType.isRequest(delivery_type))
                            {
                                this.highway101Listener.onRequest(this.session, msg);
                            }
                            else
                            {
                                logger.Error("invalid message delivery mode: " + msg.getDeliveryMode());
                            }
                        }
                    }
                }
                catch (TrxException e)
                {
                    //Error
                    Thread.Sleep(3000);                  
                }
                //200116 H101 Remove finally
                /*
                finally
                {
                    //191027 Receive H101, Remove Thread.Sleep(10). so No Sleep.
                    //Thread.Sleep(10);
                    //
                }
                */
                //
            }
        }

        public bool IsAlive()
        {
            bool isAliveThread = false;

            isAliveThread = WorkerThread.IsAlive;

            return isAliveThread;
        }


        public void Dispose()
        {
            Stop();
        }
    }


}