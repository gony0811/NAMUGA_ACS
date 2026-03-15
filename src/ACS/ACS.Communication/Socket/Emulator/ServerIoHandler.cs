using Mina.Core.Service;
using Mina.Core.Session;
using ACS.Communication.Socket.Checker;
using ACS.Communication.Socket.Model;
using ACS.Core.Logging;
using ACS.Core.Workflow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Emulator
{
    /// <summary>
    /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
    /// Event Handler
    /// </summary>
    public class ServerIoHandler : IoHandlerAdapter
    {
        public Logger logger = Logger.GetLogger(typeof(ClientIoHandler));
        public Logger commLogger = Logger.GetLogger("AGVCommLogger");
        public AbstractSocketService abstractSocketService;
        public IWorkflowManager workflowManager;
        public DuplicateChecker duplicateChecker;
        public Nio nio;
        public ConcurrentQueue<object> executorService = new ConcurrentQueue<object>();
        public ServerIoConnector serverIoConnector = new ServerIoConnector();

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

        /// <summary>
        /// Server Receive Message From Client 
        /// </summary>
        /// <param name="session">Client</param>
        /// <param name="message"></param>
        public override void MessageReceived(IoSession session, object message)
        {
            string ReceiveString = Encoding.UTF8.GetString((byte[])message);                                    //"\u0002001AH135620b\u0003"

            //Verify Alphabet or Number  
            //if (!Regex.IsMatch(ReceiveString.Substring(1, ReceiveString.Length - 2), @"^[a-zA-Z0-9]+$"))        //001AH135620b
            //{
            //    logger.Fatal("ES Data is NG !! (No Alphabet or No number): " + ReceiveString.Substring(1, ReceiveString.Length - 2));
            //    return;
            //}

            //Verify AGV Number is NG (No Integer)
            int agvNumber = 0;

            if (!int.TryParse(ReceiveString.Substring(1, 3), out agvNumber))
            {
                logger.Fatal("ES Data is Not AGV number : " + ReceiveString.Substring(1, ReceiveString.Length - 2));
                return;
            }

            ReceivePacketEmulator rcvPacket = new ReceivePacketEmulator((byte[])message);

            if (rcvPacket.GetChecksumResult())
            {
                commLogger.Info(rcvPacket.ToString());

                string messageName = "EMUL-MESSAGERECEIVED";

                //bool duplicate = this.duplicateChecker.Duplicate(rcvPacket, this.nio.Id);

                //if (!duplicate)
                {

                    //if (this.workflowManager.GetWorkflowType(messageName) == "System.Activities")
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
            else
            {
                logger.Error("failed checksum, " + rcvPacket.ToData());
            }
        }

        /// <summary>
        /// Call when the client is connected
        /// </summary>
        /// <param name="session"></param>
        public override void SessionOpened(IoSession session)
        {
            logger.Info("sessionOpened, " + session.LocalEndPoint);
            base.SessionOpened(session);

            this.abstractSocketService.IoSession = session;
            this.abstractSocketService.SessionOpened = true;

            this.serverIoConnector.AbstractSocketService = this.abstractSocketService;

            string messageName = "EMUL-ONLINECONNECTED";
            object[] args = new object[] { this.nio };

            this.workflowManager.Execute(messageName, args);


            //object name = this.serverIoConnector.AbstractSocketService.IoSession.GetAttribute("Name");

            //serverIoConnector.AddClient();

            //var consumer = Task.Factory.StartNew(() =>
            //{
            //    serverIoConnector.Run();
            //});

            //Task.WaitAll(consumer);
        }

        /// <summary>
        /// Call when the client is disConnected
        /// </summary>
        /// <param name="session"></param>
        public override void SessionClosed(IoSession session)
        {
            logger.Info("sessionClosed, " + session.LocalEndPoint);

            //object name = session.GetAttribute("Name");

            //base.SessionClosed(session);

            //this.serverIoConnector.AbstractSocketService = this.abstractSocketService;

            //serverIoConnector.RemoveClient();

            //var consumer = Task.Factory.StartNew(() =>
            //{
            //    serverIoConnector.Run();
            //});

            //Task.WaitAll(consumer);
        }

        /// <summary>
        /// Call when the Server Send Message To Client
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        public override void MessageSent(IoSession session, object message)
        {

        }
    }
}
