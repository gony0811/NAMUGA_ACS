using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.Diagnostics;
using Spring.Context.Support;
using ACS.Communication.Msb.Convert.Mapping;
using ACS.Communication.Msb.Convert;
using ACS.Framework.Message.Model;
using ACS.Utility;
using Spring.Util;
using Spring.Objects;
using System.Reflection;
using log4net;
using System.Configuration;
using NHibernate.Cfg;
using ACS.Communication.Msb.Convert.Implement;

namespace ACS.Communication.Msb.Convert.Impl
{
    /// <summary>
    /// 190905 SDD ADS 메시지 수신용
    /// </summary>
    public abstract class AbstractMsbConverterQcs : AbstractMsbConverter
    {
        public override void Init()
        {
            //String time = TimeUtils.getTimeToMilliPrettyFormat();

            //System.out.println("[" + time + "] " + getClass().getName() + " will be initialized");
            //if (this.logManager != null)
            //{
            //    logger.setLogManager(this.logManager);
            //}
            //else
            //{
            //    System.out.println("[" + time + "] logManger is not used, because it is not wired at " + getClass().getName());
            //}
            Load();
        }

        public override void Load()
        {
            LoadMessageMappings();
            LoadReceiveMessages();
            LoadSendMessages();
        }

        private void LoadSendMessages()
        {
            if (string.IsNullOrEmpty(this.SendMessageTemplateFilePath))
            {
                logger.Warn("receiveMessageTemplate should be defined first. check template file path{" + this.ReceiveMessageTemplateFilePath + "}");
            }
            else
            {
                XmlDocument sendMessageTemplate = new XmlDocument();
                SendMessageTemplateFilePath = SendMessageTemplateFilePath.Replace("@{site}", ConfigurationManager.AppSettings[ACS.Framework.Application.Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                string path = SystemUtility.GetFullPathName(SendMessageTemplateFilePath);
                sendMessageTemplate.Load(path);

                if (sendMessageTemplate == null)
                {
                    logger.Fatal("receiveMessageTemplate should be created first, please check error message");
                    return;
                }

                Dictionary<string, XmlDocument> sendMessageTemplates = new Dictionary<string, XmlDocument>();

                XmlNodeList messageNodeList = sendMessageTemplate.GetElementsByTagName("Message");

                foreach (XmlNode sendMessageNode in messageNodeList)
                {
                    XmlDocument messageTemplate = new XmlDocument();
                    messageTemplate.AppendChild(messageTemplate.ImportNode(sendMessageNode, true));
                    string messageName = XmlUtility.GetDataFromXml(messageTemplate, this.XpathOfMessageName);

                    if (sendMessageTemplates.ContainsKey(messageName))
                    {
                        sendMessageTemplates[messageName] = messageTemplate;
                    }
                    else
                    {
                        sendMessageTemplates.Add(messageName, messageTemplate);
                        //logger.info("sendMessageTemplate{" + messageName + "} is registered");
                    }
                }

                this.SendMessageTemplates = sendMessageTemplates;
            }
        }
        //public abstract object ConvertToHost(string paramstring, AbstractMessage paramAbstractMessage);

        //public abstract AbstractMessage ConvertFromHost(object paramObject);
    }
}
