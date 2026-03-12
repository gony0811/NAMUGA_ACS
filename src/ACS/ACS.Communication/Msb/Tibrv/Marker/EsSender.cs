using ACS.Communication.Msb.Highway101;
using ACS.Communication.Msb.Util;
using ACS.Utility;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv.Marker
{
    public class EsSender : GenericTibrvSender
    {
        public void send(XmlDocument document)
        {
            send(document, false, "");
        }
        public void send(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            send(document, this.DefaultDestination, useCommunicationMessageNameForLogging, communicationMessageName);
        }
        public void send(string message)
        {
            send(message, false, "");
        }

        public void send(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            send(message, this.DefaultDestination, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void send(IDictionary messages)
        {
            send(messages, this.defaultDestination);
        }

        public void send(sbyte[] message)
        {
            send(message, this.DefaultDestination.ToString());
        }

        public void send(ISerializable serializable)
        {
            send(serializable, this.DefaultDestination);
        }

        public void send(object obj)
        {
            send(obj, false, "");
        }

        public void send(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            send(obj, this.defaultDestination, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public void send(XmlDocument document, IDestination destination)
            public void send(XmlDocument document, ChannelDestination destination)
        {
            send(document, ((QueueDestination)destination).Name, false, "");
        }

        //public void send(XmlDocument document, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
            public void send(XmlDocument document, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            send(document, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public void send(string message, IDestination destination)
                    public void send(string message, ChannelDestination destination)
        {
            send(message, ((QueueDestination)destination).Name, false, "");
        }

        //public void send(string message, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
            public void send(string message, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            send(message, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public void send(IDictionary messages, IDestination destination)
            public void send(IDictionary messages, ChannelDestination destination)
        {
            send(messages, destination, false, "");
        }

        //public void send(sbyte[] message, IDestination destination)
        //{
        //    send(new String(message), destination);
        //}

        //public void send(ISerializable serializable, IDestination destination)
            public void send(ISerializable serializable, ChannelDestination destination)
        {
            send(serializable, destination, false, "");
        }

        //public void send(object obj, IDestination destination)
            public void send(object obj, ChannelDestination destination)
        {
            send(obj, ((QueueDestination)destination).Name, false, "");
        }

        //public void send(object obj, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
            public void send(object obj, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            send(obj, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void send(XmlDocument document, string dest)
        {
            send(document, dest, false, "");
        }

        public void send(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                Message tibrvMsg = new Message();
                if (this.DataFormat == (eDataFormat)2)
                {
                    //ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
                    //ObjectOutputStream objectOutputStream = new ObjectOutputStream(byteArrayOutputStream);
                    //objectOutputStream.writeObject(document);
                    //objectOutputStream.close();

                    sbyte[] array = (sbyte[])(Array)Encoding.Default.GetBytes(document.OuterXml);
                    tibrvMsg.AddField(this.dataFieldName, array);
                }
                else
                {
                    tibrvMsg.UpdateField(this.dataFieldName, document.InnerXml);
                }
                tibrvMsg.SendSubject = dest;

                //logger.Info("server message send" + System.Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

                Transport.TibrvTransport.Send(tibrvMsg);
                if (useCommunicationMessageNameForLogging)
                {
                    //logger.fine(XmlUtils.toStringPrettyFormat(document) + " was sent to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                }
                else
                {
                    //logger.fine(XmlUtils.toStringPrettyFormat(document) + " was sent to " + dest);
                }
            }
            catch (RendezvousException e)
            {
                logger.Error("failed to send message to dest{" + dest + "}, {" + XmlUtility.GetLogStringFromXml(document.DocumentElement) + "}", e);
            }
            catch (Exception e)
            {
                logger.Error("failed to send message to dest{" + dest + "}, {" + XmlUtility.GetLogStringFromXml(document.DocumentElement) + "}", e);
            }
            finally
            {

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
                Message tibrvMsg = new Message();
                tibrvMsg.UpdateField(this.dataFieldName, message);
                tibrvMsg.SendSubject = dest;

                Transport.TibrvTransport.Send(tibrvMsg);
                if (useCommunicationMessageNameForLogging)
                {
                    //logger.fine(message + " was sent to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                }
                else
                {
                    //logger.fine(message + " was sent to " + dest);
                }
            }
            catch (RendezvousException e)
            {
                //logger.error("failed to send message to dest{" + dest + "}, {" + message + "}", e);
            }
            catch (Exception e)
            {
                //logger.error("failed to send message to dest{" + dest + "}, {" + message + "}", e);
            }
            finally
            {

            }
        }

        public void send(IDictionary messages, string dest)
        {
            send(messages, dest, false, "");
        }

        //public void send(sbyte[] message, String dest)
        //{
        //    send(new String(message), dest);
        //}

        public void send(ISerializable serializable, string dest)
        {
            send(serializable, dest, false, "");
        }

        public void send(object obj, string dest)
        {
            send(obj, dest, false, "");
        }

        public void send(object obj, String dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if (obj is Message)
            {
                try
                {
                    Message tibrvMsg = (Message)obj;
                    tibrvMsg.SendSubject = dest;
                    Transport.TibrvTransport.Send(tibrvMsg);
                    if (useCommunicationMessageNameForLogging)
                    {
                        //logger.fine(object + " was sent to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                    }
                    else
                    {
                        //logger.fine(object + " was sent to " + dest);
                    }
                }
                catch (RendezvousException e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + obj + "}", e);
                }
                catch (Exception e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + obj + "}", e);
                }
                finally
                {

                }
            }
            else if (obj is ISerializable)
            {
                try
                {
                    Message tibrvMsg = new Message();

                    if (this.DataFormat == (eDataFormat)2)
                    {
                        //ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
                        //ObjectOutputStream objectOutputStream = new ObjectOutputStream(byteArrayOutputStream);
                        //objectOutputStream.writeObject(object);
                        //objectOutputStream.close();

                        sbyte[] array = (sbyte[])(Array)Encoding.Default.GetBytes((string)obj);
                        tibrvMsg.AddField(this.dataFieldName, array);

                        tibrvMsg.SendSubject = dest;

                        Transport.TibrvTransport.Send(tibrvMsg);

                        if (useCommunicationMessageNameForLogging)
                        {
                            //logger.fine(object.toString() + " was sent to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                        }
                        else
                        {
                            //logger.fine(object.toString() + " was sent to " + dest);
                        }
                    }
                    else
                    {
                        //logger.error("failed to send message, set useObject as true if you use this method");
                    }
                }
                catch (RendezvousException e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + object.toString() + "}", e);
                }
                catch (Exception e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + object.toString() + "}", e);
                }
                finally
                {

                }
            }
        }

        public XmlDocument request(XmlDocument document)
        {
            return request(document, false, "");
        }

        public XmlDocument request(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, ((QueueDestination)this.defaultDestination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument request(XmlDocument document, string dest)
        {
            return request(document, dest, false, "");
        }

        public XmlDocument request(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, dest, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public XmlDocument request(XmlDocument document, IDestination destination)
            public XmlDocument request(XmlDocument document, ChannelDestination destination)
        {
            return request(document, destination, false, "");
        }

        //public XmlDocument request(XmlDocument document, IDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
            public XmlDocument request(XmlDocument document, ChannelDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument request(XmlDocument document, long timeout)
        {
            return request(document, timeout, false, "");
        }

        public XmlDocument request(XmlDocument document, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, this.DefaultDestination, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public XmlDocument request(XmlDocument document, IDestination destination, long timeout)
            public XmlDocument request(XmlDocument document, ChannelDestination destination, long timeout)
        {
            return request(document, destination, this.timeout, false, "");
        }

        //public XmlDocument request(XmlDocument document, IDestination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
                    public XmlDocument request(XmlDocument document, ChannelDestination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(document, ((QueueDestination)destination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument request(XmlDocument document, string dest, long timeout)
        {
            return request(document, dest, timeout, false, "");
        }

        public XmlDocument request(XmlDocument document, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            String replyMessage = request(document.InnerText, dest, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
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
        }

        public string request(String message)
        {
            return request(message, false, "");
        }

        public string request(String message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, ((QueueDestination)this.defaultDestination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string request(string message, string dest)
        {
            return request(message, dest, false, "");
        }
        public string request(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, dest, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public string request(string message, IDestination destination)
            public string request(string message, ChannelDestination destination)
        {
            return request(message, destination, false, "");
        }

        //public string request(string message, IDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
            public string request(string message, ChannelDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string request(string message, long timeout)
        {
            return request(message, timeout, false, "");
        }

        public string request(string message, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, this.defaultDestination, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public string request(string message, IDestination destination, long timeout)
            public string request(string message, ChannelDestination destination, long timeout)
        {
            return request(message, destination, timeout, false, "");
        }

        //public string request(string message, IDestination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
            public string request(string message, ChannelDestination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(message, ((QueueDestination)destination).Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string request(string message, string dest, long timeout)
        {
            return request(message, dest, timeout, false, "");
        }

        public string request(string message, string dest, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            try
            {
                if (useCommunicationMessageNameForLogging)
                {
                    //logger.fine(message + " will be requested to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                }
                else
                {
                    //logger.fine(message + " will be requested to " + dest);
                }
                Message tibrvMsg = new Message();
                tibrvMsg.UpdateField(this.dataFieldName, message);
                tibrvMsg.SendSubject = dest;
                Message replyTibrvMsg = Transport.TibrvTransport.SendRequest(tibrvMsg, timeout);
                if (replyTibrvMsg == null)
                {
                    //logger.error("failed to get reply message, timeout{" + this.timeout + "} occurred");
                    return null;
                }
                return MessageConverterUtilsEx.GetMessageAsString(replyTibrvMsg, this.dataFieldName);
            }
            catch (RendezvousException e)
            {
                //logger.error("failed to request to dest{" + dest + "}, " + message, e);
                return null;
            }
            catch (Exception e)
            {
                //logger.error("failed to request to dest{" + dest + "}, " + message, e);
            }
            finally
            {

            }
            return null;
        }

        public object request(object obj)
        {
            return request(obj, false, "");
        }

        public object request(object obj, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return request(obj, ((QueueDestination)this.defaultDestination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object request(object obj, string dest)
        {
            return request(obj, dest, false, "");
        }

        public object request(object obj, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(obj, dest, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public object request(object obj, IDestination destination)
            public object request(object obj, ChannelDestination destination)
        {
            return request(obj, destination, false, "");
        }

        //public object request(object obj, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
            public object request(object obj, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return request(obj, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object request(object obj, long timeout)
        {
            return request(obj, timeout, false, "");
        }

        public object request(object obj, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request((long)timeout, this.defaultDestination, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public object request(object obj, IDestination destination, long timeout)
            public object request(object obj, ChannelDestination destination, long timeout)
        {
            return request(obj, destination, timeout, false, "");
        }

        //public object request(object obj, IDestination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
            public object request(object obj, ChannelDestination destination, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return request(obj, ((QueueDestination)destination).Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object request(object obj, string dest, long timeout)
        {
            return request(obj, dest, timeout, false, "");
        }

        public object request(object obj, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if (obj is Message)
            {
                try
                {
                    if (useCommunicationMessageNameForLogging)
                    {
                        //logger.fine(object + " will be requested to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                    }
                    else
                    {
                        //logger.fine(object + " will be requested to " + dest);
                    }
                    Message tibrvMsg = (Message)obj;
                    tibrvMsg.SendSubject = dest;
                    Message replyTibrvMsg = Transport.TibrvTransport.SendRequest(tibrvMsg, timeout);
                    if (replyTibrvMsg == null)
                    {
                        //logger.error("failed to get reply message, timeout{" + this.timeout + "} occurred");
                        return null;
                    }
                    return replyTibrvMsg;
                }
                catch (RendezvousException e)
                {
                    //logger.error("failed to request to dest{" + dest + "}, " + object, e);
                    return null;
                }
                catch (Exception e)
                {
                    //logger.error("failed to request to dest{" + dest + "}, " + object, e);
                    return null;
                }
                finally
                {

                }
            }
            if (obj is ISerializable)
            {
                try
                {
                    if (useCommunicationMessageNameForLogging)
                    {
                        //logger.fine(object + " will be requested to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                    }
                    else
                    {
                        //logger.fine(object + " will be requested to " + dest);
                    }
                    Message tibrvMsg = new Message();
                    if (this.DataFormat == (eDataFormat)2)
                    {
                        //ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
                        //ObjectOutputStream objectOutputStream = new ObjectOutputStream(byteArrayOutputStream);
                        //objectOutputStream.writeObject(object);
                        //objectOutputStream.close();

                        //byte[] array = byteArrayOutputStream.toByteArray();
                        sbyte[] array = (sbyte[])(Array)Encoding.Default.GetBytes(obj.ToString());
                        tibrvMsg.AddField(this.dataFieldName, array);

                        tibrvMsg.SendSubject = dest;

                        Message replyTibrvMsg = Transport.TibrvTransport.SendRequest(tibrvMsg, timeout);
                        if (replyTibrvMsg == null)
                        {
                            //logger.error("failed to get reply message, timeout{" + this.timeout + "} occurred");
                            return null;
                        }
                        return MessageConverterUtilsEx.GetMessageBasedOnObject(replyTibrvMsg, this.dataFieldName);
                    }
                    //logger.error("failed to send message, set useObject as true if you use this method");
                    return null;
                }
                catch (RendezvousException e)
                {
                    //logger.error("failed to request to dest{" + dest + "}, " + object, e);
                    return null;
                }
                catch (Exception e)
                {
                    //logger.error("failed to request to dest{" + dest + "}, " + object, e);
                    return null;
                }
                finally
                {

                }
            }
            //logger.error("failed to request{" + object + "}, object should be an instance of " + TibrvMsg.class + "|" + Serializable.class);
            return null;
        }

        public ISerializable request(ISerializable serializable)
        {
            return request(serializable, this.defaultDestination);
        }

        public ISerializable request(ISerializable serializable, String dest)
        {
            object obj = request(serializable, dest, this.timeout, false, "");
            if ((obj is ISerializable))
            {
                return (ISerializable)obj;
            }
            return null;
        }

        //public ISerializable request(ISerializable serializable, IDestination destination)
            public ISerializable request(ISerializable serializable, ChannelDestination destination)
        {
            return request(serializable, destination, this.timeout);
        }

        public ISerializable request(ISerializable serializable, long timeout)
        {
            object obj = request(serializable, this.defaultDestination, timeout, false, "");
            if ((obj is ISerializable))
            {
                return (ISerializable)obj;
            }
            return null;
        }

        public ISerializable request(ISerializable serializable, string dest, long timeout)
        {
            object obj = request(serializable, dest, timeout, false, "");
            if ((obj is ISerializable))
            {
                return (ISerializable)obj;
            }
            return null;
        }


        //public ISerializable request(ISerializable serializable, IDestination destination, long timeout)
                    public ISerializable request(ISerializable serializable, ChannelDestination destination, long timeout)
        {
            object obj = request(serializable, destination, timeout, false, "");
            if ((obj is ISerializable))
            {
                return (ISerializable)obj;
            }
            return null;
        }

        public void reply(XmlDocument secondary, XmlDocument primary)
        {
            reply(secondary, primary, false, "");
        }

        public void reply(XmlDocument secondary, XmlDocument primary, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string dest = XmlUtility.GetDataFromXml(secondary, "/MESSAGE/ORIGINATED/ORIGINATEDNAME");
            reply(secondary, dest);
        }


        public void reply(XmlDocument secondary, string dest)
        {
            reply(secondary, dest, false, "");
        }
        public void reply(XmlDocument secondary, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            send(secondary, dest, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public void reply(XmlDocument secondary, IDestination destination)
            public void reply(XmlDocument secondary, ChannelDestination destination)
        {
            reply(secondary, destination);
        }

        //public void reply(XmlDocument secondary, IDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
            public void reply(XmlDocument secondary, ChannelDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            reply(secondary, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void reply(ISerializable serializable, string dest, string conversationId)
        {
            //logger.warn("not supported, please contact developer team");
        }

        //public void reply(ISerializable serializable, IDestination destination, string conversationId)
            public void reply(ISerializable serializable, ChannelDestination destination, string conversationId)
        {
            //logger.warn("not supported, please contact developer team");
        }
    }
}

