using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ACS.Utility;

namespace ACS.Communication.Msb.RabbitMQ
{
    class GenericWorkflowRabbitMQListenerQcs : GenericWorkflowRabbitMQListener
    {
        public override void OnMessage(XmlDocument document, string dest)
        {
            string messageName = XmlUtility.GetDataFromXml(document, xpathOfMessageName);
            string commandName = messageName;

            Console.WriteLine(messageName);

            if (string.IsNullOrEmpty(messageName))
            {
                commandName = XmlUtility.GetDataFromXml(document, "/Message/Header/MessageName");    //"MOVECMD"
            }

            string transactionId = XmlUtility.GetDataFromXml(document, "/Message/Header/TransactionID");

            if (string.IsNullOrEmpty(commandName))
            {
                logger.Fatal("public void OnMessage(XmlDocument document) : " + "commandName is Null or Empty");
            }

            ExecuteWorkflow(transactionId, commandName, document);          //commandName is "MOVECMD"
        }
    }
}
