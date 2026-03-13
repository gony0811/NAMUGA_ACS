using ACS.Framework.Logging;
using Mina.Core.Service;
using ACS.Communication;
using ACS.Workflow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket
{
    public class SocketServer : AbstractSocketService, IControllable
    {
        public Logger logger = Logger.GetLogger(typeof(SocketServer));
        private IoAcceptor ioAcceptor;
        public IWorkflowManager WorkflowManager { get; set; }

        public virtual IoAcceptor IoAcceptor
        {
            get { return this.ioAcceptor; }
            set { this.ioAcceptor = value; }
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
                this.ioAcceptor.Bind();
                logger.Info("listening on " + this.ioAcceptor.DefaultLocalEndPoint);
                result = true;
            }
            catch (IOException e)
            {
                logger.Error("failed to start, " + this.ioAcceptor, e);
            }
            return result;
        }

        public override void OnDisconnected()
        {
            this.WorkflowManager.Execute("DISCONNECTED", this);
        }
    }
}
