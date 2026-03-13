using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ACS.Communication.Msb.Highway101.Message;
using ACS.Framework.Application;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Control;

namespace ACS.Communication.Msb.RabbitMQ
{
    public class ApplicationControlAgentRabbitMQListener : AbstractRabbitMQListener
    {
        public IApplicationControlManager ApplicationControlManager { get; set; }
        public IMessageManagerEx MessageManager { get; set; }

        public override void OnMessage(XmlDocument document, string dest)
        {
            ControlMessageEx controlMessage = MessageManager.CreateControlMessage(document);

            this.ApplicationControlManager.Control(controlMessage);

            if (controlMessage.MessageName.Equals("CONTROL-HEARTBEAT"))
            {
                string message = document.InnerXml;

                if ((document is ExtendDocument))
                {

                }
            }
        }

        public override void OnMessage(AbstractMessage abstractMessage)
        {

        }
    }
}
