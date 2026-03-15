using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.Diagnostics;
using ACS.Communication.Msb.Convert.Mapping;
using ACS.Communication.Msb.Convert;
using ACS.Core.Message.Model;
using ACS.Utility;
using System.Reflection;
using ACS.Core.Logging;
using System.Configuration;

namespace ACS.Communication.Msb.Convert.Implement
{
    public abstract class AbstractMsbConverter : IMsbConverter
    {
        protected static Logger logger = Logger.GetLogger(typeof(AbstractMsbConverter));

        protected static String PLACE_HOLDER_PROPERTY = "$";
        protected static String PLACE_HOLDER_METHOD = "#";
        protected static String PLACE_HOLDER_CAUSE = "@";
        protected static String PLACE_HOLDER_START_PREFIX_PROPERTY = "${";
        protected static String PLACE_HOLDER_START_PREFIX_METHOD = "#{";
        protected static String PLACE_HOLDER_START_PREFIX_CAUSE = "@{";
        protected static String PLACE_HOLDER_END_SUFFIX = "}";
        protected static String PLACE_HOLDER_METHOD_ARGUMENT_PREFIX = "(";
        protected static char PLACE_HOLDER_METHOD_ARGUMENT_PREFIX_CHAR = '(';
        protected static String PLACE_HOLDER_METHOD_ARGUMENT_SUFFIX = ")";
        protected static char PLACE_HOLDER_METHOD_ARGUMENT_SUFFIX_CHAR = ')';
        protected static String METHOD_CHANGEVALUE = "ChangeValue";
        protected static String METHOD_COMPOSEVALUE = "ComposeValue";


        public String MessagesMappingFilePath { get; set; }
        public String ReceiveMessageTemplateFilePath { get; set; }
        public String SendMessageTemplateFilePath { get; set; }
        public IDictionary SendMessageMappings { get; set; }
        public IDictionary ReceiveMessageMappings { get; set; }
        public IDictionary ReceiveMessageTemplates { get; set; }
        public IDictionary SendMessageTemplates { get; set; }
        public IMsbValueConverter MsbValueConverter { get; set; }
        public String XpathOfMessageName { get; set; }
        public IDictionary<string, string> MessageSource { get; set; }

        public virtual void Init()
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

        public virtual void Load()
        {
            LoadMessageMappings();
            LoadReceiveMessages();
            LoadSendMessages();
        }

        public virtual void LoadSendMessages()
        {
            if (string.IsNullOrEmpty(this.SendMessageTemplateFilePath))
            {
                logger.Warn("receiveMessageTemplate should be defined first. check template file path{" + this.ReceiveMessageTemplateFilePath + "}");
            }
            else
            {
                XmlDocument sendMessageTemplate = new XmlDocument();
                SendMessageTemplateFilePath = SendMessageTemplateFilePath.Replace("@{site}", ConfigurationManager.AppSettings[ACS.Core.Application.Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                string path = SystemUtility.GetFullPathName(SendMessageTemplateFilePath);
                sendMessageTemplate.Load(path);

                if (sendMessageTemplate == null)
                {
                    logger.Fatal("receiveMessageTemplate should be created first, please check error message");
                    return;
                }

                Dictionary<string, XmlDocument> sendMessageTemplates = new Dictionary<string, XmlDocument>();

                XmlNodeList messageNodeList = sendMessageTemplate.GetElementsByTagName("Msg");
                

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

        public virtual void LoadReceiveMessages()
        {
           
            if (string.IsNullOrEmpty(this.ReceiveMessageTemplateFilePath))
            {
                //logger.warn("receiveMessageTemplate should be defined first. check template file path{" + this.receiveMessageTemplateFilePath + "}");
            }
            else
            {
                XmlDocument receiveMessageTemplate = new XmlDocument();
                ReceiveMessageTemplateFilePath = ReceiveMessageTemplateFilePath.Replace("@{site}", ConfigurationManager.AppSettings[ACS.Core.Application.Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                string path = SystemUtility.GetFullPathName(ReceiveMessageTemplateFilePath);
                
                receiveMessageTemplate.Load(path);

                if (receiveMessageTemplate == null)
                {
                    //logger.fatal("receiveMessageTemplate should be created first, please check error message");
                    return;
                }

                Dictionary<string, object> receiveMessageTemplates = new Dictionary<string, object>();

                XmlNodeList messageNodeList = receiveMessageTemplate.GetElementsByTagName("Msg");
                foreach (XmlNode receiveMessageNode in messageNodeList)
                {
                    XmlDocument messageTemplate = new XmlDocument();                  
                    messageTemplate.AppendChild(messageTemplate.ImportNode(receiveMessageNode, true));

                    string messageName = XmlUtility.GetDataFromXml(messageTemplate, this.XpathOfMessageName);

                    if (receiveMessageTemplates.ContainsKey(messageName))
                    {
                        receiveMessageTemplates[messageName] = messageTemplate;
                    }
                    else
                    {
                        receiveMessageTemplates.Add(messageName, messageTemplate);
                        //logger.info("receiveMessageTemplate{" + messageName + "} is registered");
                    }
                }

                this.ReceiveMessageTemplates = receiveMessageTemplates;
            }
        }

        public virtual void LoadMessageMappings()
        {
            if (string.IsNullOrEmpty(MessagesMappingFilePath))
            {
                //logger.warn("messageMapping configuration should be defined first. check mapping file path{" + this.messagesMappingFilePath + "}");
            }
            else
            {
                string path = SystemUtility.GetFullPathName(MessagesMappingFilePath);
                path = path.Replace("@{site}", ConfigurationManager.AppSettings[ACS.Core.Application.Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                XmlDocument messageNamesDocument = new XmlDocument();
                messageNamesDocument.Load(path);
                if (messageNamesDocument == null)
                {
                    //logger.fatal("messageNamesMapping should be created first, please check error messages");
                    return;
                }
                Dictionary<string, object> receiveMessageMappings = new Dictionary<string, object>();
                Dictionary<string, object> sendMessageMappings = new Dictionary<string, object>();

                XmlNode receiveMessages = messageNamesDocument.SelectSingleNode("//receive");

                foreach (XmlNode node in receiveMessages)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;

                    XmlElement receiveMessageElement = (XmlElement)node;

                    String name = receiveMessageElement.GetAttribute("name");
                    String className = receiveMessageElement.GetAttribute("class");
                    String hostMessageName = receiveMessageElement.GetAttribute("host-message-name");

                    if (string.IsNullOrEmpty(hostMessageName))
                    {
                        //logger.info("{" + name + "} can not be mapped, it will be skipped");
                    }
                    else
                    {
                        bool use = false;
                        if(!bool.TryParse(receiveMessageElement.GetAttribute("use"), out use))
                        {
                            use = false;
                        }

                        ReceiveMessageMapping receiveMessageMapping = new ReceiveMessageMapping(name, className, hostMessageName, use);
                        if (receiveMessageMappings.ContainsKey(name))
                        {
                            //키가 중복된 경우 덮어 씌움
                            receiveMessageMappings[name] = receiveMessageMapping;
                        }
                        else
                        {
                            receiveMessageMappings.Add(name, receiveMessageMapping);
                            //logger.info("{" + name + "} is mapped to {" + hostMessageName + "}");
                        }
                    }
                }

                XmlNode sendMessages = messageNamesDocument.SelectSingleNode("//send");

                foreach (XmlNode node in sendMessages)
                {
                    if (node.NodeType != XmlNodeType.Element) continue;

                    XmlElement sendMessageElement = (XmlElement)node;

                    String name = sendMessageElement.GetAttribute("name");
                    String methodName = sendMessageElement.GetAttribute("method");
                    String className = sendMessageElement.GetAttribute("class");
                    String hostMessageName = sendMessageElement.GetAttribute("host-message-name");
                    if (string.IsNullOrEmpty(hostMessageName))
                    {
                        //logger.info("{" + name + "} can not be mapped, it will be skipped");
                    }
                    else
                    {
                        bool use = false;
                        if (!bool.TryParse(sendMessageElement.GetAttribute("use"), out use))
                        {
                            use = false;
                        }

                        SendMessageMapping sendMessageMapping = new SendMessageMapping(name, className, methodName, hostMessageName, use);

                        if (sendMessageMappings.ContainsKey(name))
                        {
                            //키가 중복된 경우 덮어 씌움
                            sendMessageMappings[name] = sendMessageMapping;
                        }
                        else
                        {
                            sendMessageMappings.Add(name, sendMessageMapping);
                            //logger.info("{" + name + "} is mapped to {" + hostMessageName + "}");
                        }
                    }
                }

                this.ReceiveMessageMappings = receiveMessageMappings;
                this.SendMessageMappings = sendMessageMappings;
            }
        }

        public IMsbValueConverter GetMsbValueConverter()
        {
            throw new NotImplementedException();
        }

        public string GetReceiveHostMessageName(string name)
        {
            String result = "";
            ReceiveMessageMapping receiveMessageMapping = null;

            if (this.ReceiveMessageMappings.Contains(name))
            {
                receiveMessageMapping = (ReceiveMessageMapping)this.ReceiveMessageMappings[name];
            }

            if (receiveMessageMapping != null)
            {
                result = receiveMessageMapping.HostMessageName;
            }
            return result;
        }

        public XmlDocument GetReceiveMessageTemplate(string name)
        {

            XmlDocument documentTemplate = null;

            if (this.ReceiveMessageTemplates.Contains(name))
                documentTemplate = (XmlDocument)this.ReceiveMessageTemplates[name];

            if (documentTemplate == null)
            {
                throw new ConvertException(name, name + " is not defined, please check send message file");
            }
            return (XmlDocument)documentTemplate.Clone();
        }

        public string GetSendHostMessageName(string name)
        {
            String result = "";
            SendMessageMapping sendMessageMapping = null;

            if (this.SendMessageMappings.Contains(name))
            {
                sendMessageMapping = (SendMessageMapping)this.SendMessageMappings[name];
            }

            if (sendMessageMapping != null)
            {
                result = sendMessageMapping.HostMessageName;
            }
            return result;
        }

        public XmlDocument GetSendMessageTemplate(string name)
        {

            XmlDocument documentTemplate = null;

            if(this.SendMessageTemplates.Contains(name))
                documentTemplate = (XmlDocument)this.SendMessageTemplates[name];

            if (documentTemplate == null)
            {
                throw new ConvertException(name, name + " is not defined, please check send message file");
            }
            return (XmlDocument)documentTemplate.Clone();
        }

        public void Reload()
        {
            Load();
        }

        public bool UseReceive(string name)
        {
            bool result = false;
            ReceiveMessageMapping receiveMessageMapping = null;

            if (this.ReceiveMessageMappings.Contains(name))
            {
                receiveMessageMapping = (ReceiveMessageMapping)this.ReceiveMessageMappings[name];
            }
            
            if (receiveMessageMapping != null)
            {
                result = receiveMessageMapping.Use;
            }
            return result;
        }

        public bool UseSend(string name)
        {
            bool result = false;
            SendMessageMapping sendMessageMapping = null;

            if (this.SendMessageMappings.Contains(name))
            {
                sendMessageMapping = (SendMessageMapping)this.SendMessageMappings[name];
            }

            if (sendMessageMapping != null)
            {
                result = sendMessageMapping.Use;
            }
            return result;
        }


        protected AbstractMessage CreateReceiveMessage(string className)
        {
            AbstractMessage message = null;
            try
            {
                Type type = Type.ReflectionOnlyGetType(className, true, true);
                message = (AbstractMessage)Activator.CreateInstance(type);
                message.OriginatedType = "H";
            }
            catch (Exception e)
            {
                logger.Error("", e);
            }

            return message;
        }

        protected object CreateObject(string className)
        {
            object obj = null;
            try
            {
                Type type = Type.ReflectionOnlyGetType(className, true, true);
                obj = Activator.CreateInstance(type);
            }
            catch (TypeLoadException e)
            {
                logger.Error("create object is failed. className(" + className + ")", e);
            }
            catch (TypeInitializationException e)
            {
                logger.Error("create object is failed. className(" + className + ")", e);
            }
            return obj;
        }

        protected bool IsConstant(string value)
        {
            bool result = true;
            if (!string.IsNullOrEmpty(value))
            {
                if ((value.StartsWith(PLACE_HOLDER_PROPERTY)) || (value.StartsWith(PLACE_HOLDER_CAUSE)) || (value.StartsWith(PLACE_HOLDER_METHOD)))
                {
                    result = false;
                }
            }

            return result;
        }

        protected string RemovePlaceHolder(string value)
        {
            try
            {
                return value.Substring(2, value.Length - (PLACE_HOLDER_END_SUFFIX.Length + 2));
            }
            catch (Exception e)
            {
                logger.Warn("failed to remove placeHolder from{" + value + "}", e);
            }
            return value;
        }

        protected string GetPropertyValue(ObjectWrapper objectWrapper, string value)
        {
            String result = "";
            try
            {
                Object propertyValue = objectWrapper.GetPropertyValue(value);
                result = propertyValue != null ? propertyValue.ToString() : "";
            }
            catch (Exception e)
            {
                logger.Warn("failed to set propertyValue{" + value + "}. so, whiteString will be set, " + e);
            }
            return result;
        }


        public abstract object ConvertToHost(string paramstring, AbstractMessage paramAbstractMessage);

        public abstract AbstractMessage ConvertFromHost(object paramObject);
    }
}
