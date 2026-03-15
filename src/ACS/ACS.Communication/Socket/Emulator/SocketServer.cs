using ACS.Core.Logging;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Logging;
using Mina.Transport.Socket;
using ACS.Communication;
using ACS.Communication.Socket.Checker;
using ACS.Communication.Socket.Model;
using ACS.Core.Workflow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Emulator
{
    /// <summary>
    /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
    /// </summary>
    public class SocketServer : AbstractSocketService, IControllable
    {
        public Logger logger = Logger.GetLogger(typeof(SocketServer));
        private IoAcceptor ioAcceptor;
        private Nio nio;
        private IWorkflowManager workflowManager;
        private DuplicateChecker duplicateChecker;

        public virtual DuplicateChecker DuplicateChecker
        {
            get { return this.duplicateChecker; }
            set { this.duplicateChecker = value; }
        }

        public virtual IoAcceptor IoAcceptor
        {
            get { return this.ioAcceptor; }
            set { this.ioAcceptor = value; }
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
            //IO Acceptor 선언
            IoAcceptor nioSocketAcceptor = new AsyncSocketAcceptor();

            //IPEndPoint address = new IPEndPoint(IPAddress.Parse(this.nio.RemoteIp), this.nio.Port);
            //ioAcceptor.DefaultRemoteEndPoint = address;

            //이벤트 핸들러 연결
            ServerIoHandler serverIoHandler = new ServerIoHandler();
            serverIoHandler.WorkflowManager = this.workflowManager;
            serverIoHandler.DuplicateChecker = this.duplicateChecker;
            serverIoHandler.AbstractSocketService = this;
            serverIoHandler.Nio = this.nio;
            nioSocketAcceptor.Handler = serverIoHandler;

            //Encoder, Decoder 연결
            ProtocolCodecFilter codecFilter = new ProtocolCodecFilter(new CommonPacketEmulatorFactory());
            LoggingFilter loggingFilter = new LoggingFilter();
            DefaultIoFilterChainBuilder filterChainBuilder = new DefaultIoFilterChainBuilder();

            nioSocketAcceptor.FilterChainBuilder = filterChainBuilder;
            nioSocketAcceptor.FilterChain.AddLast("codecFilter", codecFilter);
            nioSocketAcceptor.FilterChain.AddLast("loggingFilter", loggingFilter);

            IoAcceptor = nioSocketAcceptor;

            return 0;
        }

        public bool Open()
        {
            IoServiceStatistics ioServiceStatistics = this.ioAcceptor.Statistics;
            logger.Info(ioServiceStatistics.ToString());

            return this.ioAcceptor.Active;
        }

        public bool Start()
        {
            bool result = false;
            result = Connect();

            return result;
        }

        public bool Stop()
        {
            bool result = true;
            this.ioAcceptor.Unbind();

            return result;
        }

        public override bool Connect()
        {
            bool result = false;
            try
            {
                //IPEndPoint address = new IPEndPoint(IPAddress.Parse(this.nio.RemoteIp), this.nio.Port);
                //ioAcceptor.DefaultRemoteEndPoint = address;

                this.ioAcceptor.Bind(new IPEndPoint(IPAddress.Parse(this.nio.RemoteIp), this.nio.Port));
                logger.Info("listening on " + this.ioAcceptor.LocalEndPoint);
                result = true;
            }
            catch (IOException e)
            {
                logger.Error("failed to start, " + this.ioAcceptor, e);
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

            this.WorkflowManager.Execute("DISCONNECTED", this);

        }
    }

    #region V2 Backup
    //public class SocketServer : AbstractSocketService, IControllable
    //{
    //    public ILog logger = log4net.LogManager.GetLogger(typeof(SocketServer)); 
    //    private IoAcceptor ioAcceptor;
    //    public IWorkflowManager WorkflowManager { get; set; }

    //    public virtual IoAcceptor IoAcceptor
    //    {
    //        get { return this.ioAcceptor; }
    //        set { this.ioAcceptor = value; }
    //    }


    //    public bool Open()
    //    {
    //        IoServiceStatistics ioServiceStatistics = this.ioAcceptor.Statistics;
    //        logger.Info(ioServiceStatistics.ToString());

    //        return this.ioAcceptor.Active;
    //    }

    //    public bool Start()
    //    {
    //        bool result = false;
    //        result = Connect();

    //        return result;
    //    }

    //    public bool Stop()
    //    {
    //        bool result = true;
    //        this.ioAcceptor.Unbind();

    //        return result;
    //    }

    //    public override bool Connect()
    //    {
    //        bool result = false;
    //        try
    //        {
    //            this.ioAcceptor.Bind();
    //            logger.Info("listening on " + this.ioAcceptor.DefaultLocalEndPoint);
    //            result = true;
    //        }
    //        catch (IOException e)
    //        {
    //            logger.Error("failed to start, " + this.ioAcceptor, e);
    //        }
    //        return result;
    //    }

    //    public override void OnDisconnected()
    //    {

    //        this.WorkflowManager.Execute("DISCONNECTED", this);

    //    }
    //}
    #endregion
}
