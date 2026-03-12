using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using ACS.Communication.Msb.Highway101;
using ACS.Communication.Msb.Highway101.Message;
using com.miracom.transceiverx;
using com.miracom.transceiverx.message;
using com.miracom.transceiverx.session;
using ACS.Communication.Msb;
using ACS.Utility;


namespace ACS.Communication.Msb.Highway101
{
    public class GenericHighway101SenderACS : AbstractHighway101, IMessageAgent, ISynchronousMessageAgent
    {
        public Session senderSession;

        public ChannelDestination defaultDestination;
        public long messageTTL = 60000L;

        public Session SenderSession
        {
            get { return senderSession; }
            set { senderSession = value; }
        }

        public ChannelDestination DefaultDestination
        {
            get { return defaultDestination; }
            set { defaultDestination = value; }
        }

        public long MessageTTL
        {
            get { return messageTTL; }
            set { messageTTL = value; }
        }

        public override void Init()
        {
            string destinationName = defaultDestination.Name.Replace(".", "/");

            if (!destinationName.StartsWith("/"))
            {
                destinationName = "/" + destinationName;
            }

            ChannelDestination destination = new ChannelDestination(destinationName);

            try
            {
                this.senderSession = CreateSession();
                if (this.DeliveryMode.Equals("PULL"))
                {
                    CreatePullingThread(this.senderSession, this);
                }
            }
            catch (TrxException e)
            {
                // MsbStartException;
                throw e;
            }
        }

        public void Send(XmlDocument document)
        {
            Send(document, false, "");
        }

        public void Send(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMeesageName)
        {
            Send(document, this.defaultDestination.Name, useCommunicationMessageNameForLogging, communicationMeesageName);
        }

        public void Send(string message)
        {
            Send(message, false, "");
        }

        public void Send(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(message, this.defaultDestination.Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(IDictionary messages)
        {
            // Not Supported
        }

        public void Send(byte[] message)
        {
            Send(message, this.defaultDestination.Name);
        }

        public void Send(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(obj, this.defaultDestination.Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(XmlDocument document, string destination)
        {
            Send(document, destination, false, "");
        }

        public void Send(IDictionary messages, string destination)
        {
            //Not Supported
        }

        public void Send(object obj, string destination)
        {
            Send(obj, destination, false, "");
        }

        public void Send(XmlDocument document, string destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                com.miracom.transceiverx.message.Message message = this.senderSession.createMessage();
                message.setChannel(destination);
                message.setTTL(this.messageTTL);
                message.setData(document.InnerXml);

                //190611 Modification of Host Report Form
                //logger.Info("host message send" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));
                logger.Info("host message send" + Environment.NewLine + document.InnerXml);
                if (this.CastOption.Equals("UNICAST"))
                {
                    message.setDeliveryMode(1);
                    this.senderSession.sendUnicast(message);
                    //logger.Info("H101 SEND COMPLETE &&&& " );
                    //logger.Info(document.InnerXml);
                    
                }
                else if (this.CastOption.Equals("MULTICAST"))
                {
                    message.setDeliveryMode(2);
                    this.senderSession.sendMulticast(message);
                }
                else if (this.CastOption.Equals("GUNICAST"))
                {
                    message.setDeliveryMode(5);
                    this.senderSession.sendGuaranteedUnicast(destination, message);
                }
                else
                {
                    message.setDeliveryMode(6);
                    this.senderSession.sendGuaranteedMulticast(destination, message);
                }

                string messageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");
                string transactionId = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/TRANSACTIONID");
                string carrierName = "";
                string transportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/COMMANDID");
                string currentMachineName = "";
                string currentUnitName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");
                if (useCommunicationMessageNameForLogging)
                {
                    // log
                }
                else
                {
                    // short log
                }
            }
            catch (TrxException e)
            {
                logger.Error("H101 send communiaction error!", e);
            }
            catch (Exception e)
            {
                logger.Error("H101 send communiaction error!", e);
            }
        }

        public void Send(string message, string dest)
        {
            Send(message, dest, false, "");
        }

        public void Send(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                com.miracom.transceiverx.message.Message sendMessage = this.senderSession.createMessage();
                sendMessage.setChannel(dest);
                sendMessage.setData(message);
                if (this.CastOption.Equals("UNICAST"))
                {
                    sendMessage.setDeliveryMode((short)1);
                    this.senderSession.sendUnicast(sendMessage);
                    //logger.Info("H101 SEND COMPLETE $$$$ ");
                    //logger.Info(message);
                    logger.Info("host message send : " + message);
                }
                else if (this.CastOption.Equals("MULTICAST"))
                {
                    sendMessage.setDeliveryMode((short)2);
                    this.senderSession.sendMulticast(sendMessage);
                }
                else if (this.CastOption.Equals("GUNICAST"))
                {
                    sendMessage.setDeliveryMode((short)5);
                    this.senderSession.sendGuaranteedUnicast(dest, sendMessage);
                }
                else
                {
                    sendMessage.setDeliveryMode((short)6);
                    this.senderSession.sendGuaranteedMulticast(dest, sendMessage);
                }
                if (useCommunicationMessageNameForLogging)
                {
                    //logger.well(message + " was sent to " + dest, communicationMessageName);
                }
                else
                {
                    //logger.well(message + " was sent to " + dest);
                }
            }
            catch (TrxException e)
            {
                logger.Error("failed to send message{" + message + "}", e);
            }
            catch (Exception e)
            {
                logger.Error("failed to send message{" + message + "}", e);
            }
        }

        //public void Send(IDictionary messages, string dest)
        //{
        //    // not supported
        //}

        public void Send(byte[] message, string dest)
        {
            Send(message.ToString(), dest);
        }

        //public void Send(object obj, string dest)
        //{
        //    Send(obj, dest, false, "");
        //}

        public void Send(object obj, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if (obj is com.miracom.transceiverx.message.Message)
            {
                try
                {
                    ((com.miracom.transceiverx.message.Message)obj).setChannel(dest);
                    ((com.miracom.transceiverx.message.Message)obj).setTTL(this.messageTTL);
                    if (this.CastOption.Equals("UNICAST"))
                    {
                        ((com.miracom.transceiverx.message.Message)obj).setDeliveryMode(1);
                        this.senderSession.sendUnicast((com.miracom.transceiverx.message.Message)obj);
                    }
                    else if (this.CastOption.Equals("MULTICAST"))
                    {
                        ((com.miracom.transceiverx.message.Message)obj).setDeliveryMode(2);
                        this.senderSession.sendMulticast((com.miracom.transceiverx.message.Message)obj);
                    }
                    else if (this.CastOption.Equals("GUNICAST"))
                    {
                        ((com.miracom.transceiverx.message.Message)obj).setDeliveryMode(5);
                        this.senderSession.sendGuaranteedUnicast(dest, (com.miracom.transceiverx.message.Message)obj);
                    }
                    else
                    {
                        ((com.miracom.transceiverx.message.Message)obj).setDeliveryMode(6);
                        this.senderSession.sendGuaranteedMulticast(dest, (com.miracom.transceiverx.message.Message)obj);
                    }
                }
                catch (TrxException e)
                {
                    // error log
                }
                catch (Exception e)
                {
                    // error log
                }
            }
            else
            {
                // error log
            }
        }

        public XmlDocument Request(XmlDocument document)
        {
            return Request(document, false, "");
        }

        public XmlDocument Request(XmlDocument document, string dest)
        {
            return Request(document, dest, false, "");
        }

        public XmlDocument Request(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, dest, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public XmlDocument Request(XmlDocument document)
        //{
        //    return Request(document, false, "");
        //}

        public XmlDocument Request(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, this.defaultDestination.Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }


        public XmlDocument Request(XmlDocument document, long timeout)
        {
            return Request(document, timeout, false, "");
        }

        public XmlDocument Request(XmlDocument document, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, this.defaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }


        public XmlDocument Request(XmlDocument document, string dest, long timeout)
        {
            return Request(document, dest, timeout, false, "");
        }

        public XmlDocument Request(XmlDocument document, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string replyMessage = Request(document.ToString(), dest, timeout, useCommunicationMessageNameForLogging, communicationMessageName);

            if (string.ReferenceEquals(replyMessage, null))
            {
                return null;
            }
            else
            {
                XmlDocument documentTemp = new XmlDocument();

                documentTemp.LoadXml(replyMessage);

                return documentTemp;
            }

            //return replyMessage != null ? XmlUtils.MakeDocument4Contents(replyMessage) : null;
        }

        public string Request(string message)
        {
            return Request(message, false, "");
        }

        public string Request(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, this.defaultDestination.Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, string dest)
        {
            return Request(message, dest, false, "");
        }

        public string Request(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, dest, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, long timeout)
        {
            return Request(message, timeout, false, "");
        }
        public string Request(string message, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, this.defaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, string dest, long timeout)
        {
            return Request(message, dest, timeout, false, "");
        }

        public string Request(string message, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                if (useCommunicationMessageNameForLogging)
                {
                    //logger.well(message + " will be requested to " + dest, communicationMessageName);
                }
                else
                {
                    //logger.well(message + " will be requested to " + dest);
                }
                com.miracom.transceiverx.message.Message msg = this.senderSession.createMessage();
                msg.setChannel(dest);
                msg.setData(message);
                msg.setTTL(timeout);
                msg.setDeliveryMode((short)3);
                com.miracom.transceiverx.message.Message replyMessage = this.senderSession.sendRequest(msg);
                if (replyMessage == null)
                {
                    throw new TrxException();
                }
                return replyMessage.getStreamMessage().toString();
            }
            catch (TrxException e)
            {
                logger.Error("failed to request " + message, e);
            }
            return null;
        }

        public object Request(object obj)
        {
            return Request(obj, false, "");
        }

        public object Request(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return this.Request(obj, this.defaultDestination.Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, string dest)
        {
            return Request(obj, dest, false, "");
        }

        public object Request(object obj, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return this.Request(obj, dest, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, long timeout)
        {
            return Request(obj, timeout, false, "");
        }

        public object Request(object obj, string dest, long timeout)
        {
            return Request(obj, dest, timeout, false, "");
        }

        public object Request(object obj, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(obj, this.defaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if (obj is com.miracom.transceiverx.message.Message)
            {		      
                try
                {               
                    if (useCommunicationMessageNameForLogging) 
                    {
                        //logger.well(object + " will be requested to " + dest, communicationMessageName);              
                    } 
                    else 
                    {
                        //logger.well(object + " will be requested to " + dest);
                    }
                
                    com.miracom.transceiverx.message.Message replyMessage = this.senderSession.sendRequest((com.miracom.transceiverx.message.Message)obj);
                
                    if (replyMessage == null)
                    {                
                        logger.Error("failed to get reply message, timeout{" + this.messageTTL + "} occurred");
                        return null;
                    }

                    return ACS.Communication.Msb.Util.MessageConverterUtils.GetMessageAsString(replyMessage);
                }
                catch (TrxException e)
                {
                    logger.Error("failed to request " + obj, e);
                    return null;
                }
            }
            return null;
        }

        //public object Request(object obj, string dest, long timeout)
        //{
        //  return Request(obj, dest, timeout, false, "");
        //}

        public ISerializable Request(ISerializable serializable)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        public ISerializable Request(ISerializable serializable, string dest)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        public ISerializable Request(ISerializable serializable, long timeout)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        public ISerializable Request(ISerializable serializable, string dest, long timeout)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        public void Reply(XmlDocument secondary, XmlDocument primary)
        {
            Reply(secondary, primary, false, "");
        }

        public void Reply(XmlDocument secondary, XmlDocument primary, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                com.miracom.transceiverx.message.Message requestMessage = (com.miracom.transceiverx.message.Message)((ExtendDocument)primary).ExtendDataElement("PRIMARYMESSAGE");
                com.miracom.transceiverx.message.Message replyMessage = requestMessage.createReply();
                replyMessage.setDeliveryMode(4);
                replyMessage.setData(secondary.InnerXml);
                this.senderSession.sendReply(requestMessage, replyMessage);
            }
            catch(TrxException e)
            {
                // logger
            }
        }

        public void Reply(XmlDocument secondary, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            logger.Warn("not supported, please contact developer team");
        }
        public void Reply(ISerializable serializable, string dest, string conversationId)
        {
            logger.Warn("not supported, please contact developer team");
        }

        public void Reply(XmlDocument secondary, string dest) { }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("highway101Sender{");
            sb.Append("destination=").Append(this.defaultDestination);
            sb.Append(", connectUrl=").Append(this.ConnectUrl);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", sessionId=").Append(this.SessionId);
            sb.Append(", castOption=").Append(this.CastOption);
            sb.Append(", stationMode=").Append(this.StationMode);
            sb.Append(", deliveryMode=").Append(this.DeliveryMode);
            sb.Append(", pullTimeout=").Append(this.PullTimeout);
            sb.Append(", autoRecoveryOption=").Append(this.AutoRecoveryOption);
            sb.Append(", defaultTTL=").Append(this.DefaultTTL);
            sb.Append(", dataFormat=").Append(this.DataFormat).Append("{").Append(GetDataFormatTostring()).Append("}");
            sb.Append("}");

            return sb.ToString();
        }

        public GenericHighway101SenderACS()
        {
            //Load Configuration
        }


        public void Send(ISerializable paramSerializable)
        {
            Send(paramSerializable, this.defaultDestination.Name);
        }

        public void Send(object paramObject)
        {
            Send(paramObject, false, "");
        }

        public void Send(ISerializable paramSerializable, string destination)
        {
            Send(paramSerializable, destination, false, "");
        }
    }
}
