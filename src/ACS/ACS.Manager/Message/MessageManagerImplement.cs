using ACS.Framework.Base;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Manager.Message
{
    public class MessageManagerImplement : AbstractManager, IMessageManager
    {
        protected XmlDocument templateDocument;
        protected String messageTemplatePath;
        public XmlDocument TemplateDocument
        {
            get { return TemplateDocument; }
            set { TemplateDocument = value; }
        }
        public string MessageTemplatePath
        {
            get { return messageTemplatePath; }
            set {messageTemplatePath=value;}
        }

        public virtual void Init()
        {
            base.Init();
            if (this.messageTemplatePath == null)
            {
                //logger.warn("message template should be defined first, default message format will be used");
                CreateDefaultMessageFormat();
            }
            else
            {
                templateDocument = new XmlDocument();
                this.templateDocument.Load(this.messageTemplatePath); //= XmlUtils.makeDocument(this.messageTemplatePath);
                if (this.templateDocument == null)
                {
                    //logger.fatal("template document should be created first, please check error messages");
                    return;
                }
            }
        }

        protected void CreateDefaultMessageFormat()
        {

            XmlDocument document = new XmlDocument();
            XmlElement message = document.CreateElement("MESSAGE");
            document.AppendChild(message);

            XmlElement header = document.CreateElement("HEADER");
            message.AppendChild(header);

            XmlNode messagename = document.CreateNode(XmlNodeType.Element, "MESSAGENAME", "");
            XmlNode transactionid = document.CreateNode(XmlNodeType.Element, "TRANSACTIONID", "");
            XmlNode conversationid = document.CreateNode(XmlNodeType.Element, "CONVERSATIONID", "");
            XmlNode time = document.CreateNode(XmlNodeType.Element, "TIME", "");
            XmlNode sender = document.CreateNode(XmlNodeType.Element, "SENDER", "");
            header.AppendChild(messagename);
            header.AppendChild(transactionid);
            header.AppendChild(conversationid);
            header.AppendChild(time);
            header.AppendChild(sender);

            XmlElement originated = document.CreateElement("ORIGINATED");
            message.AppendChild(originated);

            XmlNode originatedtype = document.CreateNode(XmlNodeType.Element, "ORIGINATEDTYPE", "");
            XmlNode originatedname = document.CreateNode(XmlNodeType.Element, "ORIGINATEDNAME", "");
            XmlNode machinename = document.CreateNode(XmlNodeType.Element, "MACHINENAME", "");
            XmlNode connectionid = document.CreateNode(XmlNodeType.Element, "CONNECTIONID", "");
            XmlNode username = document.CreateNode(XmlNodeType.Element, "USERNAME", "");

            originated.AppendChild(originatedtype);
            originated.AppendChild(originatedname);
            originated.AppendChild(machinename);
            originated.AppendChild(connectionid);
            originated.AppendChild(username);

            XmlElement data = document.CreateElement("DATA");
            message.AppendChild(data);
            XmlElement tail = document.CreateElement("TAIL");
            message.AppendChild(tail);

            this.templateDocument = document;
        }

        public ControlMessage CreateControlMessage(XmlDocument document)
        {
            ControlMessage controlMessage = new ControlMessage();
            controlMessage.ReceivedMessage = document;

            controlMessage.MessageName = document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText;

            controlMessage.ApplicationName = document.SelectSingleNode("/MESSAGE/DATA/APPLICATIONNAME").InnerText;
            controlMessage.ApplicationType = document.SelectSingleNode("/MESSAGE/DATA/APPLICATIONTYPE").InnerText;
            controlMessage.SecsName = document.SelectSingleNode("/MESSAGE/DATA/SECSNAME").InnerText;

            SetHeaderInfoToMessage(document, controlMessage);
            SetOriginatedInfoToMessage(document, controlMessage);

            return controlMessage;
        }

        public ControlMessage CreateControlMessage(String messageName, String applicationName)
        {
            ControlMessage controlMessage = new ControlMessage();

            controlMessage.MessageName = messageName;
            controlMessage.ApplicationName = applicationName;

            return controlMessage;
        }

        public XmlDocument CreateDocument(ControlMessage controlMessage)
        {
            XmlDocument document = CreateDocument((AbstractMessage)controlMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode applicationName = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
            XmlNode applicationType = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");
            XmlNode SecsName = document.CreateNode(XmlNodeType.Element, "SECSNAME", "");

            applicationName.InnerText = controlMessage.ApplicationName;
            applicationType.InnerText = controlMessage.ApplicationType;
            SecsName.InnerText = controlMessage.SecsName;
            data.AppendChild(applicationName);
            data.AppendChild(applicationType);
            data.AppendChild(SecsName);

            return document;
        }

        public XmlDocument CreateDocument(AbstractMessage abstractMessage)
        {
            XmlDocument document = (XmlDocument)this.templateDocument.Clone();

            SetHeaderInfoToDocument(abstractMessage, document);
            SetOriginatedInfoToDocument(abstractMessage, document);

            return document;
        }


        public void SetHeaderInfoToDocument(AbstractMessage abstractMessage, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["HEADER"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "MESSAGENAME")) node.InnerText = abstractMessage.MessageName;
                if (string.Equals(node.Name, "TRANSACTIONID")) node.InnerText = abstractMessage.TransactionId;
                if (string.Equals(node.Name, "CONVERSATIONID")) node.InnerText = abstractMessage.ConversationId;
                if (string.Equals(node.Name, "TIME")) node.InnerText = abstractMessage.Time;              
            }
        }

        public void SetOriginatedInfoToDocument(AbstractMessage abstractMessage, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) node.InnerText = abstractMessage.OriginatedType;
                if (string.Equals(node.Name, "ORIGINATEDNAME")) node.InnerText = abstractMessage.OriginatedName;
                if (string.Equals(node.Name, "CONNECTIONID")) node.InnerText = abstractMessage.ConnectionId;
                if (string.Equals(node.Name, "MACHINENAME")) node.InnerText = abstractMessage.CurrentMachineName;
                if (string.Equals(node.Name, "USERNAME")) node.InnerText = abstractMessage.UserName;

            }
        }

        public void SetHeaderInfoToMessage(XmlDocument document, AbstractMessage abstractMessage)
        {
            String messageName = "";
            String transactionId = "";
            String conversationId = "";
            String time = "";

            XmlElement header = document.DocumentElement["HEADER"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "MESSAGENAME")) messageName = node.InnerText;
                if (string.Equals(node.Name, "TRANSACTIONID")) transactionId = node.InnerText;
                if (string.Equals(node.Name, "CONVERSATIONID")) conversationId = node.InnerText;
                if (string.Equals(node.Name, "TIME")) time = node.InnerText;
            }

            abstractMessage.MessageName = messageName;
            abstractMessage.TransactionId = transactionId;
            abstractMessage.ConversationId = conversationId;
            abstractMessage.Time = time;

        }

        public void SetOriginatedInfoToMessage(XmlDocument document, AbstractMessage abstractMessage)
        {
            String originatedType = "";
            String originatedName = "";
            String connectionId = "";
            String machineName = "";
            String userName = "";

            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) originatedType = node.InnerText;
                if (string.Equals(node.Name, "ORIGINATEDNAME")) originatedName = node.InnerText;
                if (string.Equals(node.Name, "CONNECTIONID")) connectionId = node.InnerText;
                if (string.Equals(node.Name, "MACHINENAME")) machineName = node.InnerText;
                if (string.Equals(node.Name, "USERNAME")) userName = node.InnerText;
            }

            abstractMessage.OriginatedType = originatedType;
            abstractMessage.OriginatedName = originatedName;
            abstractMessage.ConnectionId = connectionId;
            abstractMessage.CurrentMachineName = machineName;
            abstractMessage.OriginatedMachineName = abstractMessage.CurrentMachineName;
            abstractMessage.UserName = userName;
        }
    }
}
