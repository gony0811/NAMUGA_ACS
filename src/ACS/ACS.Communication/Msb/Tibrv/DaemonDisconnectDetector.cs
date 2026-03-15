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
    public class DaemonDisconnectDetector : AbstractTibrvListener
    {
        private DisconnectDetector disconnectDetector;

        public DisconnectDetector DisconnectDetector
        {
            get
            {
                return this.disconnectDetector;
            }
            set
            {
                this.disconnectDetector = value;
            }
        }


        public override void Init()
        {
            try
            {
                this.destination = new QueueDestination("_RV.WARN.SYSTEM.RVD.DISCONNECTED");
                base.Init();
            }
            catch (RendezvousException e)
            {
                //throw new MsbStartException();
            }
            finally
            {

            }
        }

        /// <summary>
        /// @deprecated
        /// </summary>
        public override void onMessage(XmlDocument document, string dest)
        {
            //logger.info(XmlUtils.toStringPrettyFormat(document) + ", dest{" + dest + "}");
        }

        public override void onMessage(string transactionId, string messageName, XmlDocument document)
        {
            //logger.info(XmlUtils.toStringPrettyFormat(document) + ", transactionId{" + transactionId + "}, messageName{" + messageName + "}");
        }

        public override void onMessage(AbstractMessage abstractMessage)
        {
            //logger.info(abstractMessage);
        }

        //public override void onMsg(Listener tibrvListener, Message tibrvMsg)
        public override void onMsg(object tibrvListener, MessageReceivedEventArgs tibrvMsg)
        {
            //try
            //{
            //    string clazz = (string)tibrvMsg.GetField("ADV_CLASS").Value;
            //    string source = (string)tibrvMsg.GetField("ADV_SOURCE").Value;
            //    string name = (string)tibrvMsg.GetField("ADV_NAME").Value;
            //    if (name.Equals("RVD.DISCONNECTED"))
            //    {
            //        //logger.fine("daemon was disconnected, " + Transport);
            //        if (this.disconnectDetector != null)
            //        {
            //            this.disconnectDetector.disconnected(Transport);
            //        }
            //    }
            //}
            //catch (RendezvousException e)
            //{
            //    //logger.warn("failed to get data from tibrvMsg, please check tibrvMsg.field", e);
            //}
            //finally
            //{

            //}
        }

        public override void onAwakeMessage(string conversationId, string messageName, XmlDocument document)
        {
            //logger.info(XmlUtils.toStringPrettyFormat(document) + ", conversationId{" + conversationId + "}, messageName{" + messageName + "}");
        }

        public override void onAwakeMessage(AbstractMessage abstractMessage)
        {
            //logger.info(abstractMessage);
        }
    }

}
