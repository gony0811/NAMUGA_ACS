using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using ACS.Core.Host;
using ACS.Core.Base;
using ACS.Communication;
using ACS.Core.Message.Model;
using ACS.Core.Message;
using ACS.Communication.Msb;
using System.IO;

namespace ACS.Manager.Host
{
    public class HostMessageManagerImplement : AbstractManager, IHostMessageManager
    {
        public IMessageAgent HostAgent { get; set; }
        public IMsbConverter MsbConverter { get; set; }
        public IDictionary HostAgents { get; set; }
        public IMessageAgent DefaultHostAgent { get { return HostAgent; } }
        public ISynchronousMessageAgent DefaultSynchronousHostAgent { get; set; }
        public bool UseMultiHostAgent { get; set; }


        public object Convert(string messageName, AbstractMessage abstractMessage)
        {
            try
            {
                return this.MsbConverter != null ? this.MsbConverter.ConvertToHost(messageName, abstractMessage) : null;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }
            return null;
        }
        public bool UseSend(string name)
        {
            return this.MsbConverter != null ? this.MsbConverter.UseSend(name) : false;
        }

        public bool UseReceive(string name)
        {
            return this.MsbConverter != null ? this.MsbConverter.UseReceive(name) : false;
        }

        public string GetSendHostMessageName(string name)
        {
            return this.MsbConverter != null ? this.MsbConverter.GetSendHostMessageName(name) : "";
        }

        public string GetReceiveHostMessageName(string name)
        {
            return this.MsbConverter != null ? this.MsbConverter.GetReceiveHostMessageName(name) : "";
        }


        public void SendMessageToHost(AbstractMessage message, String communicationMessageName)
        {
            Object obj = message.SendingMessage;
            
            if (!(obj is FileInfo))
            {
                if ((obj is XmlDocument))
                {
                    if ((message is BaseMessage))
                    {
                        //logger.InfoFormat(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), (BaseMessage)message, communicationMessageName);
                    }
                    else
                    {
                        //logger.InfoFormat(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), message, communicationMessageName);
                    }

                    SelectHostAgent(message, communicationMessageName).Send((XmlDocument)obj, true, communicationMessageName);
                }
                else if ((obj is String))
                {
                    if ((message is BaseMessage))
                    {
                        logger.Info((String)obj, ((BaseMessage)message).ToString(), communicationMessageName);
                    }
                    else
                    {
                        logger.Info((String)obj, message.ToString(), communicationMessageName);
                    }
                    SelectHostAgent(message, communicationMessageName).Send((String)obj, true, communicationMessageName);
                }
                else
                {
                    if ((message is BaseMessage))
                    {
                        logger.Info(obj.ToString(), ((BaseMessage)message).ToString(), communicationMessageName);
                    }
                    else
                    {
                        logger.Info(obj.ToString(), message.ToString(), communicationMessageName);
                    }
                    SelectHostAgent(message, communicationMessageName).Send(obj, true, communicationMessageName);
                }
            }
            else
            {
                try
                {
                    FileInfo file = (FileInfo)obj;
                    SelectHostAgent(message, communicationMessageName).Send(file);

                    file.Delete();
                }
                catch (IOException e)
                {
                    logger.Error("failed to get file, please check it out", e);
                }
            }
        }

        public void SendMessageToHost(AbstractMessage message, String dest, String communicationMessageName)
        {
            Object obj = message.SendingMessage;
            if (!(obj is FileInfo))
            {
                if ((obj is XmlDocument))
                {
                    if ((message is BaseMessage))
                    {
                        ///logger.InfoFormat(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), (BaseMessage)message, communicationMessageName);
                    }
                    else
                    {
                        //logger.InfoFormat(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), message, communicationMessageName);
                    }
                    SelectHostAgent(message, dest, communicationMessageName).Send((XmlDocument)obj, dest, true, communicationMessageName);
                }
                else if ((obj is String))
                {
                    if ((message is BaseMessage))
                    {
                        logger.Info((String)obj, ((BaseMessage)message).ToString(), communicationMessageName);
                    }
                    else
                    {
                        logger.Info((String)obj, message.ToString(), communicationMessageName);
                    }
                    SelectHostAgent(message, dest, communicationMessageName).Send((String)obj, dest, true, communicationMessageName);
                }
                else
                {
                    if ((message is BaseMessage))
                    {
                        logger.Info(obj.ToString(), ((BaseMessage)message).ToString(), communicationMessageName);
                    }
                    else
                    {
                        logger.Info(obj.ToString(), message.ToString(), communicationMessageName);
                    }
                    SelectHostAgent(message, dest, communicationMessageName).Send(obj, dest, true, communicationMessageName);
                }
            }
            else
            {
                try
                {
                    FileInfo file = (FileInfo)obj;
                    SelectHostAgent(message, dest, communicationMessageName).Send(file, dest);
                    file.Delete();
                }
                catch (IOException e)
                {
                    logger.Error("failed to get file, please check it out", e);
                }
            }
        }


        public Object RequestMessageToHost(AbstractMessage message, String communicationMessageName)
        {
            Object obj = message.SendingMessage;
            Object replyObject = null;
            if (!(obj is FileInfo))
            {
                if ((obj is XmlDocument))
                {
                    if ((message is BaseMessage))
                    {
                        //logger.well(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), (BaseMessage)message, communicationMessageName);
                    }
                    else
                    {
                        //logger.well(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), message, communicationMessageName);
                    }
                    replyObject = SelectSynchronousHostAgent(message, communicationMessageName).Request((XmlDocument)obj, true, communicationMessageName);
                }
                else
                {
                    if ((message is BaseMessage))
                    {
                        logger.Info(obj.ToString(), ((BaseMessage)message).ToString(), communicationMessageName);
                    }
                    else
                    {
                        logger.Info(obj.ToString(), message.ToString(), communicationMessageName);
                    }
                    replyObject = SelectSynchronousHostAgent(message, communicationMessageName).Request(obj, true, communicationMessageName);
                }
            }
            else
            {
                try
                {
                    FileInfo file = (FileInfo)obj;
                    replyObject = SelectSynchronousHostAgent(message, communicationMessageName).Request(file);

                    file.Delete();
                }
                catch (IOException e)
                {
                    logger.Error("failed to get file, please check it out", e);
                }
            }
            return replyObject;
        }


        public Object RequestMessageToHost(AbstractMessage message, String dest, String communicationMessageName)
        {
            Object obj = message.SendingMessage;
            Object replyObject = null;
            if (!(obj is FileInfo))
            {
                if ((obj is XmlDocument))
                {
                    if ((message is BaseMessage))
                    {
                        //logger.well(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), (BaseMessage)message, communicationMessageName);
                    }
                    else
                    {
                        //logger.well(XmlUtils.toStringWithoutDeclaration((XmlDocument)obj), message, communicationMessageName);
                    }
                    replyObject = SelectSynchronousHostAgent(message, dest, communicationMessageName).Request((XmlDocument)obj, dest, true, communicationMessageName);
                }
                else
                {
                    if ((message is BaseMessage))
                    {
                        logger.Info(obj.ToString(), ((BaseMessage)message).ToString(), communicationMessageName);
                    }
                    else
                    {
                        logger.Info(obj.ToString(), message.ToString(), communicationMessageName);
                    }
                    replyObject = SelectSynchronousHostAgent(message, dest, communicationMessageName).Request(obj, dest, true, communicationMessageName);
                }
            }
            else
            {
                try
                {
                    FileInfo file = (FileInfo)obj;
                    replyObject = SelectSynchronousHostAgent(message, dest, communicationMessageName).Request(file, dest);

                    file.Delete();
                }
                catch (IOException e)
                {
                    logger.Error("failed to get file, please check it out", e);
                }
            }
            return replyObject;
        }


        protected IMessageAgent SelectHostAgent(AbstractMessage message, string communicationMessageName)
        {
            return this.DefaultHostAgent;
        }

        protected IMessageAgent SelectHostAgent(AbstractMessage message, string dest, string communicationMessageName)
        {
            return this.DefaultHostAgent;
        }

        protected ISynchronousMessageAgent SelectSynchronousHostAgent(AbstractMessage message, string communicationMessageName)
        {
            return this.DefaultSynchronousHostAgent;
        }

        protected ISynchronousMessageAgent SelectSynchronousHostAgent(AbstractMessage message, string dest, string communicationMessageName)
        {
            return this.DefaultSynchronousHostAgent;
        }
    }
}
