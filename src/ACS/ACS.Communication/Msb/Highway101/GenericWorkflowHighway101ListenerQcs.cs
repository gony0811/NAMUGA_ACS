using ACS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.Highway101
{
    public class GenericWorkflowHighway101ListenerQcs : GenericWorkflowHighway101Listener
    {
        /// <summary>
        /// 190905 SDD ADS 메시지 수신용
        /// </summary>
        /// <param name="document"></param>
        /// <param name="dest"></param>
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
