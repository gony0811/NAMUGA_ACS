using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ACS.Framework.Application;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Control;
using ACS.Communication.Msb.Highway101.Message;
using com.miracom.transceiverx;
using com.miracom.transceiverx.message;

namespace ACS.Communication.Msb.Highway101
{
    public class ApplicationControlAgentHighway101Listener : AbstractHighway101Listener
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
                    com.miracom.transceiverx.message.Message requestMessage = (com.miracom.transceiverx.message.Message)((ExtendDocument)document).ExtendDataElement("PRIMARYMESSAGE");
                    com.miracom.transceiverx.message.Message replyMessage = null;

                    try
                    {
                        replyMessage = requestMessage.createReply();
                        replyMessage.setData(message);
                        this.ListenerSession.sendReply(requestMessage, replyMessage);
                    }
                    catch (TrxException e)
                    {
                        //log
                    }
                }
            }  
        }

        public override void OnMessage(AbstractMessage abstractMessage)
        {

        }
    }
}
