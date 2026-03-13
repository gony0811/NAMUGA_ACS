using Mina.Core.Service;
using Mina.Core.Session;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Socket.Checker;
using ACS.Communication.Socket.Model;
using ACS.Framework.Logging;
using ACS.Workflow;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace ACS.Communication.Socket
{
    public class ClientIoHandler : IoHandlerAdapter
    {
        public Logger logger = Logger.GetLogger(typeof(ClientIoHandler));
        public Logger commLogger = Logger.GetLogger("AGVCommLogger");
        public AbstractSocketService abstractSocketService;
        public IWorkflowManager workflowManager;
        public DuplicateChecker duplicateChecker;
        public Nio nio;
        public ConcurrentQueue<object> executorService = new ConcurrentQueue<object>();
        public ClientIoConnector clientIoConnector = new ClientIoConnector();

        public virtual AbstractSocketService AbstractSocketService
        {
            get { return this.abstractSocketService; }
            set { this.abstractSocketService = value; }
        }

        public virtual IWorkflowManager WorkflowManager
        {
            get { return this.workflowManager; }
            set { this.workflowManager = value; }
        }

        public virtual DuplicateChecker DuplicateChecker
        {
            get { return this.duplicateChecker; }
            set { this.duplicateChecker = value; }
        }

        public virtual Nio Nio
        {
            get { return this.nio; }
            set { this.nio = value; }
        }

        public override void MessageReceived(IoSession session, object message)
        {
            //181004
            string ReceiveString = Encoding.UTF8.GetString((byte[])message);

            //Verify Alphabet or Number  
            if (!Regex.IsMatch(ReceiveString.Substring(1, ReceiveString.Length - 2), @"^[a-zA-Z0-9]+$"))
            {
                logger.Fatal("AGV Data is NG !! (No Alphabet or No number): " + ReceiveString.Substring(1, ReceiveString.Length - 2));
                return;
            }

#if BYTE12
            //Verify Length
            if(ReceiveString.Length != 12)
            {
                logger.Fatal("AGV Data Length is NG: Not 12 Digit!! - " + ReceiveString.ToString());
                return;
            }

            //Verify AGV Number is NG (No Integer)
            int agvNumber = 0;
            if (!int.TryParse(ReceiveString.Substring(ReceiveString.Length - 8, 2), out agvNumber))
            {
                return;
            }
#else
            //Verify Length
            if (ReceiveString.Length != 14)
            {
                logger.Fatal("AGV Data Length is NG: Not 14 Digit!! - " + ReceiveString.ToString());
                return;
            }

            //Verify AGV Number is NG (No Integer)
            int agvNumber = 0;
            if (!int.TryParse(ReceiveString.Substring(ReceiveString.Length - 12, 3), out agvNumber))
            {
                return;
            }
#endif

            ReceivePacket rcvPacket = new ReceivePacket((byte[])message);

            //200423 Ignore garbage Value in ES, Before Biz
            bool validate = this.duplicateChecker.Validate(rcvPacket);
            if (!validate)
            {
                // NG!! - validate false
                return;
            }
            //

            //20230314 Off checkSum
            //if (rcvPacket.GetChecksumResult())
            //
            {
                //string logMessage = string.Format("ES → AGV messageReceived - {0}", rcvPacket.ToString());

                commLogger.Info(rcvPacket.ToString());

                string messageName = "VEHICLE-MESSAGERECEIVED";

                bool duplicate = this.duplicateChecker.Duplicate(rcvPacket, this.nio.Id);
                if (!duplicate)
                {

                    //if(this.workflowManager.GetWorkflowType(messageName) == "System.Activities")
                    //{
                    //    Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
                    //    keyValuePairs.Add("rcvPacket", rcvPacket);
                    //    keyValuePairs.Add("argNio", this.nio);
                    //    this.workflowManager.Execute(messageName, keyValuePairs);                       
                    //}
                    //else
                    {
                        object[] args = new object[] { rcvPacket, this.nio };
                        this.workflowManager.Execute(messageName, args);
                    }
                }
            }
            //20230314 Off checkSum
            /*
            else
            {
                //200527 When CheckSum NG, Write Checksum Character
                //logger.Error("failed checksum, " + rcvPacket.ToData());
                logger.Error("failed checksum, " + rcvPacket.ToDataCheckSum());
                //
            }
            */
        }

        public override void SessionCreated(IoSession session)
        {
            logger.Info("sessionCreated, " + session);
            base.SessionCreated(session);
        }

        public override void SessionOpened(IoSession session)
        {
            logger.Info("sessionOpened, " + session);
            base.SessionOpened(session);

            this.abstractSocketService.IoSession = session;
            this.abstractSocketService.SessionOpened = true;
        }

        //protected bool running;    // = true;

        public override void SessionClosed(IoSession session)
        {
            logger.Info("sessionClosed, " + session);
            base.SessionClosed(session);

            this.abstractSocketService.OnDisconnected();

            this.clientIoConnector.AbstractSocketService = this.abstractSocketService;

            var consumer = Task.Factory.StartNew(() =>
            {
                //while (running)
                //{
                clientIoConnector.Run();
                //}
            });

            Task.WaitAll(consumer);
        }

        public override void SessionIdle(IoSession session, IdleStatus status)
        {
            logger.Info("sessionIdle, " + session);
            base.SessionIdle(session, status);
        }

        public override void ExceptionCaught(IoSession session, Exception cause)
        {
            logger.Info("exceptionCaught, " + session, cause);
            session.Close(true);
        }
        public override void MessageSent(IoSession session, object message)
        {
            //logger.Info("messageSent{" + CommonData.BytesToHex((byte[])message) + "}, " + session);

            SendPacket sendPacket = new SendPacket((byte[])message);          //Example: //{001AC987601} : AGVID_3Digit, SENDID_1Digit, COMMAND_1Digit, TagID_4Digit, SubOrder_2Digit
            //logger.Info("messageSend{" + sendPacket.ToString() + "}");
            base.MessageSent(session, sendPacket.ToString());
        }
    }
}
