using com.miracom.transceiverx;
using ACS.Communication.Msb.Highway101.Message;
using ACS.Communication.Msb.Util;
using ACS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.Highway101.Marker
{
    public class EsSender : GenericHighway101Sender
    {
        public void send(XmlDocument document)
        {
            send(document, false, "");
        }

        public void send(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            send(document, this.DefaultDestination.Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void send(string message)
        {
            send(message, false, "");
        }

        public void send(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            send(message, this.DefaultDestination.Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void send(IDictionary messages)
        {
            logger.Warn("not supported, please contact developer team");
        }

        public void send(sbyte[] message)
        {
            send(message, this.DefaultDestination.Name);
        }


        public void send(ISerializable serializable)
        {
            send(serializable, this.DefaultDestination.Name);
        }

        public void send(object obj)
        {
            send(obj, false, "");
        }

        public void send(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string dest = this.DefaultDestination.Name;
            send(obj, dest, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void send(XmlDocument document, string destination)
        {
            send(document, destination, false, "");
        }

        //public void send(XmlDocument document, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    send(document, destination, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        //public void send(string message, Destination destination)
        //{
        //    send(message, destination, false, "");
        //}

        //public void send(string message, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    send(message, destination, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        //public void send(IDictionary messages, Destination destination)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //}

        //public virtual void send(sbyte[] message, Destination destination)
        //{
        //    send(stringHelper.Newstring(message), destination);
        //}


        //public void send(ISerializable serializable, Destination destination)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //}

        //public void send(object obj, Destination destination)
        //{
        //    send(obj, destination, false, "");
        //}

        //public void send(object obj, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    send(obj, destination, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        public void send(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                com.miracom.transceiverx.message.Message message = this.senderSession.createMessage();
                message.setChannel(dest);
                message.setTTL(this.messageTTL);
                message.setData(document.OuterXml);

                logger.Info("sever message send" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

                if (this.castOption.Equals("UNICAST"))
                {
                    message.setDeliveryMode((short)1);
                    this.senderSession.sendUnicast(message);
                }
                else if (this.castOption.Equals("MULTICAST"))
                {
                    message.setDeliveryMode((short)2);
                    this.senderSession.sendMulticast(message);
                }
                else if (this.castOption.Equals("GUNICAST"))
                {
                    message.setDeliveryMode((short)5);
                    this.senderSession.sendGuaranteedUnicast(dest, message);
                }
                else
                {
                    message.setDeliveryMode((short)6);
                    this.senderSession.sendGuaranteedMulticast(dest, message);
                }

                if (useCommunicationMessageNameForLogging)
                {
                    //logger.well(XmlUtils.tostringPrettyFormat(document) + " was sent to " + dest, communicationMessageName);
                }
                else
                {
                    //logger.well(XmlUtils.tostringPrettyFormat(document) + " was sent to " + dest);
                }
            }
            catch (TrxException e)
            {
                logger.Error("failed to send message{" + document.OuterXml + "}", e);
            }
            catch (Exception e)
            {
                logger.Error("failed to send message{" + document.OuterXml + "}", e);
            }
        }

        public void send(string message, string dest)
        {
            send(message, dest, false, "");
        }

        public void send(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                com.miracom.transceiverx.message.Message sendMessage = this.senderSession.createMessage();
                sendMessage.putChannel(dest);
                sendMessage.putData(message);

                if (this.CastOption.Equals("UNICAST"))
                {
                    sendMessage.putDeliveryMode((short)1);
                    this.senderSession.sendUnicast(sendMessage);
                }
                else if (this.CastOption.Equals("MULTICAST"))
                {
                    sendMessage.putDeliveryMode((short)2);
                    this.senderSession.sendMulticast(sendMessage);
                }
                else if (this.CastOption.Equals("GUNICAST"))
                {
                    sendMessage.putDeliveryMode((short)5);
                    this.senderSession.sendGuaranteedUnicast(dest, sendMessage);
                }
                else
                {
                    sendMessage.putDeliveryMode((short)6);
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
            finally
            {

            }
        }

        public void send(IDictionary messages, string dest)
        {
            logger.Warn("not supported, please contact developer team");
        }

        public void send(sbyte[] message, string dest)
        {
            send(stringHelper.Newstring(message), dest);
        }

        public void send(ISerializable serializable, string dest)
        {
            logger.Warn("not supported, please contact developer team");
        }

        public virtual void send(object obj, string dest)
        {
            send(obj, dest, false, "");
        }

        public void send(object obj, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if ((obj is com.miracom.transceiverx.message.Message))
            {
                try
                {
                    ((com.miracom.transceiverx.message.Message)obj).putChannel(dest);
                    ((com.miracom.transceiverx.message.Message)obj).putTTL(this.messageTTL);
                    if (this.castOption.Equals("UNICAST"))
                    {
                        ((com.miracom.transceiverx.message.Message)obj).putDeliveryMode((short)1);
                        this.senderSession.sendUnicast((com.miracom.transceiverx.message.Message)obj);
                    }
                    else if (this.castOption.Equals("MULTICAST"))
                    {
                        ((com.miracom.transceiverx.message.Message)obj).putDeliveryMode((short)2);
                        this.senderSession.sendMulticast((com.miracom.transceiverx.message.Message)obj);
                    }
                    else if (this.castOption.Equals("GUNICAST"))
                    {
                        ((com.miracom.transceiverx.message.Message)obj).putDeliveryMode((short)5);
                        this.senderSession.sendGuaranteedUnicast(dest, (com.miracom.transceiverx.message.Message)obj);
                    }
                    else
                    {
                        ((com.miracom.transceiverx.message.Message)obj).putDeliveryMode((short)6);
                        this.senderSession.sendGuaranteedMulticast(dest, (com.miracom.transceiverx.message.Message)obj);
                    }
                }
                catch (TrxException e)
                {
                    logger.Error("failed to send message{" + obj + "}", e);
                }
                catch (Exception e)
                {
                    logger.Error("failed to send message{" + obj + "}", e);
                }
            }
            else
            {
                logger.Error("failed to send message{" + obj + "}, object should be an instance of " + typeof(com.miracom.transceiverx.message.Message));
            }
        }

        public XmlDocument request(XmlDocument document)
        {
            return request(document, false, "");
        }

        public XmlDocument request(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, this.DefaultDestination.Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument request(XmlDocument document, string dest)
        {
            return request(document, dest, false, "");
        }

        public XmlDocument request(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, dest, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public XmlDocument request(XmlDocument document, Destination destination)
        //{
        //    return request(document, destination, false, "");
        //}

        //public XmlDocument request(XmlDocument document, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(document, ((ChannelDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        public XmlDocument request(XmlDocument document, long timeout)
        {
            return request(document, timeout, false, "");
        }

        public XmlDocument request(XmlDocument document, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, this.DefaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public XmlDocument request(XmlDocument document, Destination destination, long timeout)
        //{
        //    return request(document, destination, this.messageTTL, false, "");
        //}

        //public XmlDocument request(XmlDocument document, Destination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(document, ((ChannelDestination)destination).Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        public XmlDocument request(XmlDocument document, string dest, long timeout)
        {
            return request(document, dest, timeout, false, "");
        }

        public XmlDocument request(XmlDocument document, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string replyMessage = request(document.OuterXml, dest, timeout, useCommunicationMessageNameForLogging, communicationMessageName);

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

            //return !string.ReferenceEquals(replyMessage, null) ? XmlUtils.MakeDocument4Contents(replyMessage) : null;
        }

        public string request(string message)
        {
            return request(message, false, "");
        }

        public string request(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, this.DefaultDestination.Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public virtual string request(string message, string dest)
        {
            return request(message, dest, false, "");
        }

        public string request(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, dest, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }
        //public string request(string message, Destination destination)
        //{
        //    return request(message, destination, false, "");
        //}
        //public virtual string request(string message, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(message, ((ChannelDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        //}
        public string request(string message, long timeout)
        {
            return request(message, timeout, false, "");
        }

        public string request(string message, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, this.DefaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public string request(string message, Destination destination, long timeout)
        //{
        //    return request(message, destination, timeout, false, "");
        //}

        //public string request(string message, Destination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(message, ((ChannelDestination)destination).Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        public string request(string message, string dest, long timeout)
        {
            return request(message, dest, timeout, false, "");
        }

        public string request(string message, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
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
                msg.putChannel(dest);
                msg.putData(message);
                msg.putTTL(timeout);
                msg.putDeliveryMode((short)3);

                com.miracom.transceiverx.message.Message replyMessage = this.senderSession.sendRequest(msg);
                if (replyMessage == null)
                {
                    logger.Error("failed to get reply message, timeout{" + this.messageTTL + "} occurred");
                    return null;
                }
                return MessageConverterUtils.GetMessageAsString(replyMessage);
            }
            catch (TrxException e)
            {
                logger.Error("failed to request " + message, e);
            }
            finally
            {

            }

            return null;
        }
        //public object request(object obj)
        //{
        //    return request(obj, false, "");
        //}

        //public object request(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(obj, ((QueueDestination)this.DefaultDestination).Name, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        //}
        public object request(object obj, string dest)
        {
            return request(obj, dest, false, "");
        }

        public object request(object obj, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(obj, dest, this.messageTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public object request(object obj, Destination destination)
        //{
        //    return request(obj, destination, false, "");
        //}
        //public object request(object @object, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(@object, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        //}
        public object request(object obj, long timeout)
        {
            return request(obj, timeout, false, "");
        }
        public object request(object obj, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(System.Convert.ToInt64(timeout), this.DefaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public object request(object obj, Destination destination, long timeout)
        //{
        //    return request(obj, destination, timeout, false, "");
        //}

        //public object request(object obj, Destination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    return request(obj, ((QueueDestination)destination).Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        public object request(object obj, string dest, long timeout)
        {
            return request(obj, dest, timeout, false, "");
        }

        public object request(object obj, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if ((obj is com.miracom.transceiverx.message.Message))
            {
                try
                {
                    if (useCommunicationMessageNameForLogging)
                    {
                        //logger.well(@object + " will be requested to " + dest, communicationMessageName);
                    }
                    else
                    {
                        //logger.well(@object + " will be requested to " + dest);
                    }
                    com.miracom.transceiverx.message.Message replyMessage = this.senderSession.sendRequest((com.miracom.transceiverx.message.Message)obj);
                    if (replyMessage == null)
                    {
                        logger.Error("failed to get reply message, timeout{" + this.messageTTL + "} occurred");
                        return null;
                    }
                    return MessageConverterUtils.GetMessageAsString(replyMessage);
                }
                catch (TrxException e)
                {
                    logger.Error("failed to request " + obj, e);
                    return null;
                }
                finally
                {

                }
            }
            return null;
        }
        public ISerializable request(ISerializable serializable)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        public ISerializable request(ISerializable serializable, string dest)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        //public ISerializable request(ISerializable serializable, Destination destination)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //    return null;
        //}

        public ISerializable request(ISerializable serializable, long timeout)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        public ISerializable request(ISerializable serializable, string dest, long timeout)
        {
            logger.Warn("not supported, please contact developer team");
            return null;
        }

        //public ISerializable request(ISerializable serializable, Destination destination, long timeout)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //    return null;
        //}

        public void reply(XmlDocument secondary, XmlDocument primary)
        {
            reply(secondary, primary, false, "");
        }

        public void reply(XmlDocument secondary, XmlDocument primary, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                com.miracom.transceiverx.message.Message requestMessage = (com.miracom.transceiverx.message.Message)((ExtendDocument)primary).ExtendDataElement("PRIMARYMESSAGE");
                com.miracom.transceiverx.message.Message replyMessage = requestMessage.createReply();
                replyMessage.putDeliveryMode((short)4);
                replyMessage.putData(secondary.OuterXml);
                logger.Info(secondary.OuterXml);
                this.senderSession.sendReply(requestMessage, replyMessage);
            }
            catch (TrxException e)
            {
                logger.Error("failed to process reply message", e);
            }
            finally
            {

            }
        }
        public void reply(XmlDocument secondary, string dest)
        {
            logger.Warn("not supported, please contact developer team");
        }

        public void reply(XmlDocument secondary, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            logger.Warn("not supported, please contact developer team");
        }

        //public void reply(XmlDocument secondary, Destination destination)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //}

        //public void reply(XmlDocument secondary, Destination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //}

        public void reply(ISerializable serializable, string dest, string conversationId)
        {
            logger.Warn("not supported, please contact developer team");
        }

        //public void reply(ISerializable serializable, Destination destination, string conversationId)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //}

        //public void send(XmlDocument document, Destination destination)
        //{
        //    //logger.Warn("not supported, please contact developer team");
        //}
    }
}
