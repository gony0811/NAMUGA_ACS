using ACS.Communication.Msb.Convert.Mapping;
using ACS.Framework.Message.Model;
using ACS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Spring.Objects;
using System.Collections;
using System.Reflection;


namespace ACS.Communication.Msb.Convert.Implement
{

    public class MsbXmlConverterExImplement : AbstractMsbConverter
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(MsbXmlConverterExImplement));
        public override Object ConvertToHost(String messageName, AbstractMessage abstractMessage)
        {
            SendMessageMapping sendMessageMapping = (SendMessageMapping)this.SendMessageMappings[messageName];
            if (sendMessageMapping == null)
            {
                throw new ConvertException(messageName, "message{" + messageName + "} is not defined, please check message mappling file");
            }
            String sendMessageName = sendMessageMapping.HostMessageName;
            XmlDocument documentTemplate = (XmlDocument)this.SendMessageTemplates[sendMessageName];
            if (documentTemplate == null)
            {
                throw new ConvertException(sendMessageName, "message{" + sendMessageName + "} is not defined, please check send message file");
            }
            XmlDocument sendDocument = (XmlDocument)documentTemplate.Clone();
            ObjectWrapper beanWrapper = new ObjectWrapper(abstractMessage);
            FillOutToHost(sendDocument.DocumentElement, beanWrapper, messageName);

            return sendDocument;
        }

        public override AbstractMessage ConvertFromHost(Object message)
        {
            XmlDocument document = (XmlDocument)message;
            String receiveMessageName = XmlUtility.GetDataFromXml(document, this.XpathOfMessageName);
            ReceiveMessageMapping receiveMessageMapping = null;

            if (this.ReceiveMessageMappings.Contains(receiveMessageName))
            {
                receiveMessageMapping = (ReceiveMessageMapping)this.ReceiveMessageMappings[receiveMessageName];
            }

            if (receiveMessageMapping == null)
            {
                throw new ConvertException(receiveMessageName, "message{" + receiveMessageName + "} is not defined, please check message mappling file");
            }

            XmlDocument documentTemplate = (XmlDocument)this.ReceiveMessageTemplates[receiveMessageName];
            if (documentTemplate == null)
            {
                throw new ConvertException(receiveMessageName, "message{" + receiveMessageName + "} is not defined, please check receive message file");
            }
            XmlDocument receive = (XmlDocument)documentTemplate.Clone();

            String internalMessageName = receiveMessageMapping.Name;
            String internalClassName = receiveMessageMapping.ClassName;
            AbstractMessage messageObject = CreateReceiveMessage(internalClassName);
            if (messageObject != null)
            {
                messageObject.ReceivedMessageName = receiveMessageName;
                messageObject.MessageName = internalMessageName;
                messageObject.ReceivedMessage = message;

                ObjectWrapper beanWrapper = new ObjectWrapper(messageObject);
                FillOutFromHost(document.DocumentElement, receive, beanWrapper, internalMessageName);
            } 

            return messageObject;
        }

        protected void FillOutToHost(XmlNode nodes, ObjectWrapper beanWrapper, String messageName)
        {
            //XmlNode childElements = element;
            //if (childElements.ChildNodes.Count != 1)
            //{
            //    foreach (var childelement in childElements)
            //    {

            //        //XmlElement childElement = (XmlElement)childelement;
            //        XmlElement childElement = childelement as XmlElement;
            //        if (childElement == null) return;
            //        String type = childElement.GetAttribute("type");
            //        if ((!string.IsNullOrEmpty(type)) && (type.Equals("list")))
            //        {
            //            FillOutToHost(childElement, (IList)beanWrapper.GetPropertyValue(childElement.GetAttribute("name")), messageName);
            //            childElement.RemoveAttribute("type");
            //            childElement.RemoveAttribute("name");
            //        }
            //        else
            //        {
            //            FillOutToHost(childElement, beanWrapper, messageName);
            //        }
            //    }
            //}

            if(nodes.HasChildNodes)
            {
                foreach(XmlNode node in nodes)
                {
                    if(node.NodeType == XmlNodeType.Element)
                    {
                        XmlElement childElement = (XmlElement)node;

                        string type = childElement.GetAttribute("type");

                        if((!string.IsNullOrEmpty(type) && type.Equals("list")))
                        {
                            FillOutToHost(childElement, (IList)beanWrapper.GetPropertyValue(childElement.GetAttribute("name")), messageName);
                            childElement.RemoveAttribute("type");
                            childElement.RemoveAttribute("name");
                        }
                        else
                        {
                            FillOutToHost(childElement, beanWrapper, messageName);
                        }
                    }
                    else
                    {
                        FillOutToHost(node, beanWrapper, messageName);
                    }
                }
            }
            else
            {
                String value = nodes.InnerText;
                if (!string.IsNullOrEmpty(value))
                {
                    if (IsConstant(value))
                    {
                        nodes.InnerText = value;
                    }
                    else if (value.StartsWith(PLACE_HOLDER_PROPERTY)) //"$"
                    {
                        value = RemovePlaceHolder(value);

                        if (beanWrapper.GetPropertyInfo(value).CanRead)
                        {
                            nodes.InnerText = GetPropertyValue(beanWrapper, value);
                        }
                        else
                        {
                            nodes.InnerText = "";
                            logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + nodes.Name + "} can not be set, default is set, please check configuration file");
                        }
                    }
                    else if (value.StartsWith(PLACE_HOLDER_CAUSE)) //"@"
                    {
                        value = RemovePlaceHolder(value);

                        if (value.Contains("."))
                        {
                            String[] token = value.Split('.');

                            if (beanWrapper.GetPropertyInfo(token[0]).CanRead)
                            {
                                String propertyValue = GetPropertyValue(beanWrapper, token[0]);
                                if (string.IsNullOrEmpty(propertyValue))
                                {
                                    nodes.InnerText = this.MessageSourceAccessor.GetMessage(value.ToUpper(), "");
                                }
                                else
                                {
                                    nodes.InnerText = this.MessageSourceAccessor.GetMessage(value.ToUpper() + "." + propertyValue, "");
                                }
                            }
                            else
                            {
                                nodes.InnerText = "";
                                logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + nodes.Name + "} can not be set, default is set, please check configuration file");
                            }
                        }
                        else if (beanWrapper.GetPropertyInfo(value).CanRead)
                        {
                            nodes.InnerText = GetPropertyValue(beanWrapper, value);
                        }
                        else
                        {
                            nodes.InnerText = "";
                            logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + nodes.Name + "} can not be set, default is set, please check configuration file");
                        }
                    }
                    else if (value.StartsWith(PLACE_HOLDER_METHOD)) //"#"
                    {
                        value = RemovePlaceHolder(value);
                        String argument = "";

                        int keyStart = value.IndexOf(PLACE_HOLDER_METHOD_ARGUMENT_PREFIX) + 1; //")"
                        if (keyStart != -1)
                        {
                            int keyEnd = value.IndexOf(PLACE_HOLDER_METHOD_ARGUMENT_SUFFIX) + 1;
                            if (keyEnd != -1)
                            {
                                argument = value.Substring(keyStart, keyEnd - keyStart);
                            }
                        }
                        if (value.StartsWith(METHOD_CHANGEVALUE))  //"changeValue"
                        {
                            String delargument = BlankRemove(argument);
                            String[] arguments = delargument.Split(',');

                            if (arguments.Length > 0)
                            {
                                String propertyValue = "";

                                if (beanWrapper.GetPropertyInfo(arguments[0]).CanRead)
                                {
                                    propertyValue = GetPropertyValue(beanWrapper, arguments[0]);
                                    try
                                    {
                                        String changedValue = this.MsbValueConverter != null ? this.MsbValueConverter.ChangeValue(nodes.Name, arguments[0], propertyValue, beanWrapper.WrappedInstance, messageName) : propertyValue;
                                        nodes.InnerText = changedValue;
                                    }
                                    catch (Exception e)
                                    {
                                        nodes.InnerText = "";
                                        logger.Warn("failed to changeValue, so {" + nodes.Name + "} can not be set, default is set, please check configuration file", e);
                                    }
                                }
                                else
                                {
                                    nodes.InnerText = "";
                                    logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + nodes.Name + "} can not be set, default is set, please check configuration file");
                                }
                            }
                            else
                            {
                                nodes.InnerText = "";
                                logger.Warn("argument{" + argument + "} is not set properly, please check configuration file");
                            }
                        }
                        else if (value.StartsWith(METHOD_COMPOSEVALUE)) //"composeValue"
                        {
                            try
                            {
                                String changedValue = this.MsbValueConverter != null ? this.MsbValueConverter.ComposeValue(nodes.Name, beanWrapper.WrappedInstance, messageName) : "";
                                nodes.InnerText = changedValue;
                            }
                            catch (Exception e)
                            {
                                nodes.InnerText = "";
                                logger.Warn("failed to composeValue, so {" + nodes.Name + "} can not be set, default is set, please check configuration file", e);
                            }
                        }
                        else
                        {
                            nodes.InnerText = "";
                            logger.Warn("argument{" + argument + "} is not set properly, it should start with {" + METHOD_CHANGEVALUE + "|" + METHOD_COMPOSEVALUE + "}, please check configuration file");
                        }
                    }
                    else
                    {
                        nodes.InnerText = "";
                        logger.Warn("{" + value + "} should start with " + PLACE_HOLDER_PROPERTY + "|" + PLACE_HOLDER_METHOD + "|" + PLACE_HOLDER_CAUSE);
                    }
                }
            }
        }

        protected void FillOutToHost(XmlElement element, IList variables, String messageName)
        {
            XmlNodeList childElements = element.ChildNodes;
            if (childElements.Count > 0)
            {
                XmlElement childTemplate = (XmlElement)((XmlElement)element.ChildNodes[0].Clone());
                element.RemoveChild(childTemplate);
                for (int index = 0; index < variables.Count; index++)
                {
                    Object variable = variables[index];
                    ObjectWrapper beanWrapper = new ObjectWrapper(variable);

                    XmlElement addedElement = (XmlElement)childTemplate.Clone();
                    XmlNodeList subChildElements = addedElement.ChildNodes;
                    foreach (var subchild in subChildElements)
                    {
                        XmlElement subChildElement = (XmlElement)subchild;

                        String value = subChildElement.InnerText;
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (IsConstant(value))
                            {
                                subChildElement.InnerText = value;
                            }
                            else if (value.StartsWith(PLACE_HOLDER_PROPERTY))  //"$"
                            {
                                value = RemovePlaceHolder(value);
                                System.Reflection.PropertyInfo propertyInfo = beanWrapper.WrappedType.GetProperty(value);

                                if (beanWrapper.GetPropertyInfo(value).CanRead)
                                {
                                    subChildElement.InnerText = GetPropertyValue(beanWrapper, value);
                                }
                                else
                                {
                                    subChildElement.InnerText = "";
                                    logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + subChildElement.Name + "} can not be set, default is set, please check configuration file");
                                }
                            }
                            else if (value.StartsWith(PLACE_HOLDER_METHOD)) //"#"
                            {
                                value = RemovePlaceHolder(value);
                                String argument = "";

                                int keyStart = value.IndexOf(PLACE_HOLDER_METHOD_ARGUMENT_PREFIX);  //"("
                                if (keyStart != -1)
                                {
                                    int keyEnd = value.IndexOf(PLACE_HOLDER_METHOD_ARGUMENT_SUFFIX, keyStart + PLACE_HOLDER_METHOD_ARGUMENT_PREFIX.Length);
                                    if (keyEnd != -1)
                                    {
                                        argument = value.Substring(keyStart + PLACE_HOLDER_METHOD_ARGUMENT_PREFIX.Length, keyEnd);
                                    }
                                }
                                if (value.StartsWith(METHOD_CHANGEVALUE)) //"changeValue"
                                {
                                    String delargument = BlankRemove(argument);
                                    String[] arguments = delargument.Split(',');
                                    if (arguments.Length > 0)
                                    {
                                        String propertyValue = "";

                                        if (beanWrapper.GetPropertyInfo(arguments[0]).CanRead)
                                        {
                                            propertyValue = GetPropertyValue(beanWrapper, arguments[0]);
                                            try
                                            {
                                                String changedValue = this.MsbValueConverter != null ? this.MsbValueConverter.ChangeValue(subChildElement.Name, arguments[0], propertyValue, beanWrapper.WrappedInstance, messageName) : propertyValue;
                                                subChildElement.InnerText = changedValue;
                                            }
                                            catch (Exception e)
                                            {
                                                subChildElement.InnerText = "";
                                                logger.Warn("failed to changeValue, so {" + subChildElement.Name + "} can not be set, default is set, please check configuration file", e);
                                            }
                                        }
                                        else
                                        {
                                            subChildElement.InnerText = "";
                                            logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + subChildElement.Name + "} can not be set, default is set, please check configuration file");
                                        }
                                    }
                                    else
                                    {
                                        subChildElement.InnerText = "";
                                        logger.Warn("argument{" + argument + "} is not set properly, please check configuration file");
                                    }
                                }
                                else if (value.StartsWith(METHOD_COMPOSEVALUE)) //"changeValue"
                                {
                                    try
                                    {
                                        String changedValue = this.MsbValueConverter != null ? this.MsbValueConverter.ComposeValue(subChildElement.Name, beanWrapper.WrappedInstance, messageName) : "";
                                        subChildElement.InnerText = changedValue;
                                    }
                                    catch (Exception e)
                                    {
                                        subChildElement.InnerText = "";
                                        logger.Warn("failed to composeValue, so {" + subChildElement.Name + "} can not be set, default is set, please check configuration file", e);
                                    }
                                }
                                else
                                {
                                    subChildElement.InnerText = "";
                                    logger.Warn("argument{" + argument + "} is not set properly, it should start with {" + METHOD_CHANGEVALUE + "|" + METHOD_COMPOSEVALUE + "}, please check configuration file");
                                }
                            }
                            else
                            {
                                subChildElement.InnerText = "";
                                logger.Warn("{" + value + "} should start with " + PLACE_HOLDER_PROPERTY + "|" + PLACE_HOLDER_METHOD);
                            }
                        }
                    }
                    element.AppendChild(addedElement);
                }
            }
        }

        protected void FillOutFromHost(XmlElement hostListedElement, XmlDocument internalDocument, IList listVariable, String messageName)
        {
            XmlNodeList elements = hostListedElement.ChildNodes;
            if (elements.Count > 0)
            {
                foreach (var elem in elements)
                {
                    XmlElement childElement = (XmlElement)elem;

                    XmlElement internalElement = (XmlElement)internalDocument.SelectSingleNode("//" + childElement.Name);

                    String className = internalElement.GetAttribute("class");

                    Object obj = CreateObject(className);
                    if (obj == null)
                    {
                        logger.Error("To create object is failed. can not convert host message");
                        return;
                    }
                    ObjectWrapper vairableWrapper = new ObjectWrapper(obj);
                    foreach (var childelem in childElement.ChildNodes)
                    {
                        XmlElement grandChildElement = (XmlElement)childelem;

                        FillOutFromHost(grandChildElement, internalDocument, vairableWrapper, messageName);
                    }
                    listVariable.Add(obj);
                }
            }
        }

        protected void FillOutFromHost(XmlElement hostDocumentElement, XmlDocument internalDocument, ObjectWrapper beanWrapper, String messageName)
        {
            XmlNodeList elements = hostDocumentElement.ChildNodes;
            if (elements.Count > 0)
            {
                foreach (var element in elements)
                {
                    XmlElement childElement = (XmlElement)element;

                    XmlElement internalElement = (XmlElement)internalDocument.SelectSingleNode("//" + childElement.Name);
                    if (internalElement != null)
                    {
                        String type = internalElement.GetAttribute("type");
                        if ((!string.IsNullOrEmpty(type)) && (type.Equals("list")))
                        {
                            FillOutFromHost(childElement, internalDocument, (IList)beanWrapper.GetPropertyValue(internalElement.GetAttribute("name")), messageName);
                        }
                        else
                        {
                            FillOutFromHost(childElement, internalDocument, beanWrapper, messageName);
                        }
                    }
                    else if (logger.IsDebugEnabled)
                    {
                        logger.Debug("{" + childElement.Name + ") is constant or empty");
                    }
                }
            }
            else
            {
                String hostValue = hostDocumentElement.InnerText;

                XmlElement internalElement = (XmlElement)internalDocument.SelectSingleNode("//" + hostDocumentElement.Name);
                if (internalElement != null)
                {
                    String value = internalElement.InnerText;
                    if (!IsConstant(value))
                    {
                        value = RemovePlaceHolder(value);

                        if (beanWrapper.GetPropertyInfo(value).CanWrite)
                        {
                            beanWrapper.SetPropertyValue(value, hostValue);
                        }
                        else
                        {
                            logger.Warn(beanWrapper.WrappedType.Name + " does not have a property{" + value + "}, so {" + value + "} can not be set, please check configuration file");
                        }
                    }
                    else if (logger.IsDebugEnabled)
                    {
                        logger.Debug("{" + value + ") is constant or empty");
                    }
                }
            }
        }

        public string BlankRemove(string inputString)
        {
            //우선 앞/뒤 공백을 제거한다
            string value = inputString.Trim();
            //공백을 제거한 결과 문자열 저장을 위한 StringBuilder 객체 생성
            System.Text.StringBuilder resultString = new StringBuilder();
            //입력받은 문자열을 공백문자를 기준으로 String배열로 변환
            string[] arrayString = value.Split(' ');
            //String 배열을 돌면서 공백이 아닌 문자만 추출하여 StringBuilder 객체에 추가
            foreach (string s in arrayString)
            {
                if (s != string.Empty) resultString.Append(s);
            }
            
            return resultString.ToString();
        }
    }
}
