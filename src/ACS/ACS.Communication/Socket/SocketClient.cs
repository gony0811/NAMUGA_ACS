using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Mina.Core;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Logging;
using Mina.Core.Filterchain;
using Mina.Transport.Socket;
using ACS.Communication.Socket.Checker;
using ACS.Communication.Socket.Model;
using ACS.Communication;
using ACS.Workflow;
using log4net;
namespace ACS.Communication.Socket
{
    public class SocketClient : AbstractSocketService, IControllable
    {
        public ILog logger = log4net.LogManager.GetLogger(typeof(SocketClient)); 
        private IoConnector ioConnector;
        private long connectionTimeout = 10000L;
        private Nio nio;
        private IWorkflowManager workflowManager;
        private DuplicateChecker duplicateChecker;

        public virtual DuplicateChecker DuplicateChecker
        {
            get { return this.duplicateChecker; }
            set { this.duplicateChecker = value; }
        }

        public virtual IoConnector IoConnector
        {
            get { return this.ioConnector; }
            set { this.ioConnector = value; }
        }

        public virtual Nio Nio
        {
            get { return this.nio; }
            set { this.nio = value; }
        }

        public virtual IWorkflowManager WorkflowManager
        {
            get { return this.workflowManager; }
            set { this.workflowManager = value; }
        }

        public virtual int Initialize()
        {
            //AsyncSocketConnector nioSocketConnector = new AsyncSocketConnector();
            IoConnector nioSocketConnector = new AsyncSocketConnector();

            //190731 IP Check 
            IPEndPoint address = null;

            try
            {
                address = new IPEndPoint(IPAddress.Parse(this.nio.RemoteIp), this.nio.Port);
                nioSocketConnector.DefaultRemoteEndPoint = address;
            }
            catch (Exception e)
            {
                logger.Fatal("public virtual int Initialize() NG IP : " + this.nio.RemoteIp + ":" + this.nio.Port + ", " + e.ToString());
            }
            finally
            {

            }
            //IPEndPoint address = new IPEndPoint(IPAddress.Parse(this.nio.RemoteIp), this.nio.Port);
            //nioSocketConnector.DefaultRemoteEndPoint = address;
            //

            ClientIoHandler clientIoHandler = new ClientIoHandler();
            clientIoHandler.WorkflowManager = this.workflowManager;
            clientIoHandler.DuplicateChecker = this.duplicateChecker;
            clientIoHandler.AbstractSocketService = this;
            clientIoHandler.Nio = this.nio;
            nioSocketConnector.Handler = clientIoHandler;

            ProtocolCodecFilter codecFilter = new ProtocolCodecFilter(new CommonPacketFactory());
            LoggingFilter loggingFilter = new LoggingFilter();
            DefaultIoFilterChainBuilder filterChainBuilder = new DefaultIoFilterChainBuilder();
            nioSocketConnector.FilterChainBuilder = filterChainBuilder;
            nioSocketConnector.FilterChain.AddLast("codecFilter", codecFilter);
            nioSocketConnector.FilterChain.AddLast("loggingFilter", loggingFilter);

            IoConnector = nioSocketConnector;

            return 0;
        }

        public bool Open()
        {
            IoServiceStatistics ioServiceStatistics = this.ioConnector.Statistics;
            logger.Info(ioServiceStatistics.ToString());

            return this.ioConnector.Active;
        }

        public bool Start()
        {
            bool result = false;

            //191101 if Connect NG, Sleep(5000)
            //Task.Run(() => Connect());
            Task.Factory.StartNew(() => Connect(), TaskCreationOptions.LongRunning);
            //

            return true;
        }

        public bool Stop()
        {
            bool result = false;
            if (this.ioSession != null)
            {
                if (this.ioSession.Connected)
                {
                    ICloseFuture closeFuture = this.ioSession.Close(true);
                    closeFuture.Await();
                    if (closeFuture.Closed)
                    {
                        this.nio.State = "CLOSED";
                        logger.Info("disconnected " + this.ioConnector.DefaultRemoteEndPoint);
                        result = true;
                    }
                }
                else
                {
                    if (this.nio.State.Equals("CONNECTING"))
                    {
                        this.nio.State = "CLOSED";
                    }
                    logger.Info("already disconnected " + this.ioConnector.DefaultRemoteEndPoint);
                    result = true;
                }
            }
            else
            {
                this.nio.State = "CLOSED";
                logger.Info("ioSession is null " + this.ioConnector.DefaultRemoteEndPoint);
                result = true;
            }
            return result;
        }

        public override bool Connect()
        {
            bool result = false;
            this.nio.State = "CONNECTING";
            if ((this.ioSession != null) && (this.ioSession.Connected))
            {
                logger.Warn("already connected, " + this.ioSession);
                result = true;
            }
            else
            {
                logger.Info("trying to connect " + this.ioConnector.DefaultRemoteEndPoint);
                
                while (!result)
                {
                    try
                    {
                        if ((this.nio.State.Equals("CLOSED")) || (this.nio.State.Equals("UNLOADED")))
                        {
                            logger.Warn("can not retry to connect, " + this.nio);
                            return false;
                        }

                        //190731 IP Check
                        if (this.ioConnector.DefaultRemoteEndPoint == null)
                        {
                            logger.Fatal("IP Format NG!! - AGV Number " + this.nio.Name + " is IP Format NG!!" + "(" + this.nio.RemoteIp + ":" + this.nio.Port + ")");
                        }
                        else
                        {
                            logger.Info(this.ioConnector.DefaultRemoteEndPoint);
                        }
                        //logger.Info(this.ioConnector.DefaultRemoteEndPoint);
                        //

                        //191203 Add Socket ConnectTimeoutInMillis
                        this.ioConnector.ConnectTimeoutInMillis = connectionTimeout;
                        //

                        IConnectFuture connectFuture = this.ioConnector.Connect();

                        logger.Info("this.ioConnector.ConnectStatus: " + connectFuture.Connected + " " + this.ioConnector.DefaultRemoteEndPoint);

                        connectFuture.Await();

                        if (connectFuture.Connected)
                        {
                            //OK Connect
                            if ((this.nio.State.Equals("CLOSED")) || (this.nio.State.Equals("UNLOADED")))
                            {
                                ICloseFuture closeFuture = this.ioSession.Close(true);
                                closeFuture.Await();
                                if (closeFuture.Closed)
                                {
                                    return false;
                                }
                            }
                            logger.Info("connected " + this.ioConnector.DefaultRemoteEndPoint);
                            logger.Info("onlineConnected, " + this.nio);
                            
                            this.workflowManager.Execute("ONLINECONNECTED", this.nio);
                            result = true;
                        }
                        else
                        {
                            //191101 if Connect NG, Sleep(5000)
                            Thread.Sleep((int)this.connectionTimeout);
                            //connectFuture.Cancel();
                            //

                            //NG Connect
                            //191008 Socket Connect NG시 Catch안되도록 변경
                            continue;
                        }
                        this.ioSession = connectFuture.Session;
                    }
                    //catch (RuntimeIoException e)
                    catch (Exception e)
                    {
                        logger.Warn("failed to connect{" + this.ioConnector.DefaultRemoteEndPoint + "}, trying to connect after " + this.connectionTimeout + ", " + this.nio, e);
                        Thread.Sleep((int)this.connectionTimeout);
                    }
                }
            }
            return result;
        }

        public bool IsOpen()
        {
            bool result = false;
            if ((this.ioSession != null) && (this.ioSession.Connected))
            {
                result = true;
            }
            return result;
        }

        public override void OnDisconnected()
        {
            logger.Info("disconnected, " + this.nio);
            this.workflowManager.Execute("DISCONNECTED", this.nio);
        }
    }
}
