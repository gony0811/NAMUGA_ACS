using ACS.Core.Message.Model;
using ACS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.Tibrv
{
    public class SimpleTibrvListener : AbstractTibrvListener
    {
        /// <summary>
        /// @deprecated
        /// </summary>
        public override void onMessage(XmlDocument document, string dest)
        {
            //logger.info(XmlUtility.GetLogStringFromXml(document.DocumentElement) + ", " + dest);
        }

        public override void onMessage(string transactionId, string messageName, XmlDocument document)
        {
        }

        public override void onMessage(AbstractMessage abstractMessage)
        {
            //logger.info(abstractMessage);
        }

        public override void onAwakeMessage(string conversationId, string messageName, XmlDocument document)
        {
            //logger.info(XmlUtility.GetLogStringFromXml(document.DocumentElement) + ", conversationId{" + conversationId + "}, messageName{" + messageName + "}");
        }

        public override void onAwakeMessage(AbstractMessage abstractMessage)
        {
            //logger.info(abstractMessage);
        }
    }

}
