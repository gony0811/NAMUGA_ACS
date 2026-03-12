using ACS.Communication.Msb;
using ACS.Communication.Msb.Convert.Implement;
using ACS.Communication.Msb.Convert.Mapping;
using ACS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.Convert.Impl
{
    public class MsbXmlConvertrerSSMImplement : MsbXmlConverterExImplement
    {      
        public IDictionary SendHttpMessageTemplates { get; set; }
        public String SendHttpMessageTemplateFilePath { get; set; }
        public String XpathOfHttpMessageName { get; set; }
        public void Init()
        {
            base.Init();
            Load();
        }
        private void Load()
        {
            LoadHttpSendMessages();
        }
        private void LoadHttpSendMessages()
        {
            if (string.IsNullOrEmpty(this.SendHttpMessageTemplateFilePath))
            {
                logger.Warn("receiveMessageTemplate should be defined first. check template file path{" + this.ReceiveMessageTemplateFilePath + "}");
            }
            else
            {
                XmlDocument sendHttpMessageTemplate = new XmlDocument();
                SendHttpMessageTemplateFilePath = SendHttpMessageTemplateFilePath.Replace("@{site}", ConfigurationManager.AppSettings[ACS.Framework.Application.Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                string path = SystemUtility.GetFullPathName(SendHttpMessageTemplateFilePath);
                sendHttpMessageTemplate.Load(path);

                if (sendHttpMessageTemplate == null)
                {
                    logger.Fatal("receiveMessageTemplate should be created first, please check error message");
                    return;
                }

                Dictionary<string, XmlDocument> sendHttpMessageTemplates = new Dictionary<string, XmlDocument>();

                XmlNodeList messageNodeList = sendHttpMessageTemplate.GetElementsByTagName("Msg");


                foreach (XmlNode sendMessageNode in messageNodeList)
                {
                    XmlDocument messageTemplate = new XmlDocument();
                    messageTemplate.AppendChild(messageTemplate.ImportNode(sendMessageNode, true));
                    string messageName = XmlUtility.GetDataFromXml(messageTemplate, this.XpathOfHttpMessageName);
                    
                    if (sendHttpMessageTemplates.ContainsKey(messageName))
                    {
                        sendHttpMessageTemplates[messageName] = messageTemplate;
                    }
                    else
                    {
                        sendHttpMessageTemplates.Add(messageName, messageTemplate);
                        //logger.info("sendMessageTemplate{" + messageName + "} is registered");
                    }
                }

                this.SendHttpMessageTemplates = sendHttpMessageTemplates;
            }
        }


        public Object ConvertToHttpMessage(String messageName)
        {
            // 이걸 어떻게..? 
            XmlDocument documentTemplate = (XmlDocument)this.SendHttpMessageTemplates[messageName];
            if (documentTemplate == null)
            {
                throw new ConvertException(messageName, "message{" + messageName + "} is not defined, please check receive message file");
            }
            XmlDocument sendmessage = (XmlDocument)documentTemplate.Clone();

            return sendmessage;
        }

        public Object ConvertToTSMessage(String messageName)
        {
            ReceiveMessageMapping receiveMessageMapping = null;
            if (this.ReceiveMessageMappings.Contains(messageName))
            {
                receiveMessageMapping = (ReceiveMessageMapping)this.ReceiveMessageMappings[messageName];
            }
            if (receiveMessageMapping == null)
            {
                throw new ConvertException(messageName, "message{" + messageName + "} is not defined, please check message mappling file");
            }

            // 이걸 어떻게..? 
            XmlDocument documentTemplate = (XmlDocument)this.ReceiveMessageTemplates["TRSJOBREQ"];
            if (documentTemplate == null)
            {
                throw new ConvertException(messageName, "message{" + messageName + "} is not defined, please check receive message file");
            }
            XmlDocument receive = (XmlDocument)documentTemplate.Clone();

            return receive;
        }


    }
}
