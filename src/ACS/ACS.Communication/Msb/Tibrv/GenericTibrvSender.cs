using ACS.Communication.Msb;
using ACS.Communication.Msb.Highway101;
using ACS.Communication.Msb.Util;
using ACS.Communication.Msb.Tibrv;
using ACS.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public class GenericTibrvSender : AbstractTibrv, IMessageAgent, ISynchronousMessageAgent
    {
        //public IDestination defaultDestination;
        public ChannelDestination defaultDestination;
        public DestinationResolver destinationResolver;
        public long timeout = 10000L;

        public long Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }


        //public static //logger //logger
        //{
        //    get
        //    {
        //        return //logger;
        //    }
        //}

        //public IDestination DefaultDestination
        public ChannelDestination DefaultDestination
        {
            get
            {
                return this.defaultDestination;
            }
            set
            {
                this.defaultDestination = value;
            }
        }

        public DestinationResolver DestinationResolver
        {
            get
            {
                return this.destinationResolver;
            }
            set
            {
                this.destinationResolver = value;
            }
        }


        public void Send(XmlDocument document)
        {
            Send(document, false, "");
        }

        public void Send(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(document, this.defaultDestination, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(string message)
        {
            Send(message, false, "");
        }

        public void Send(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(message, this.defaultDestination, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(System.Collections.IDictionary messages)
        {
            Send(messages, this.defaultDestination);
        }

        public void Send(byte[] message)
        {
            Send(message, this.defaultDestination);
        }

        public void Send(ISerializable serializable)
        {
            Send(serializable, this.defaultDestination);
        }

        public void Send(object @object)
        {
            Send(@object, false, "");
        }

        public void Send(object @object, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(@object, this.defaultDestination, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public void Send(XmlDocument document, IDestination destination)
        //{
        //    Send(document, ((QueueDestination)destination).Name, false, "");
        //}

        //public void Send(XmlDocument document, IDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    Send(document, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        //public void Send(string message, IDestination destination)
        //{
        //    Send(message, ((QueueDestination)destination).Name, false, "");
        //}

        //public void Send(string message, IDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    Send(message, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        //public void Send(System.Collections.IDictionary messages, IDestination destination)
        //{
        //    Send(messages, destination, false, "");
        //}

        //public void Send(sbyte[] message, IDestination destination)
        //{
        //    Send(message.ToString(), destination);
        //}

        //public void Send(ISerializable serializable, IDestination destination)
        //{
        //    Send(serializable, destination, false, "");
        //}

        //public void Send(object @object, IDestination destination)
        //{
        //    Send(@object, ((QueueDestination)destination).Name, false, "");
        //}

        //public void Send(object @object, IDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        //{
        //    Send(@object, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        //}

        public void Send(XmlDocument document, ChannelDestination destination)
        {
            Send(document, ((QueueDestination)destination).Name, false, "");
        }

        public void Send(XmlDocument document, ChannelDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(document, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(string message, ChannelDestination destination)
        {
            Send(message, ((QueueDestination)destination).Name, false, "");
        }

        public void Send(string message, ChannelDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(message, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(System.Collections.IDictionary messages, ChannelDestination destination)
        {
            Send(messages, destination, false, "");
        }

        public void Send(sbyte[] message, ChannelDestination destination)
        {
            Send(message.ToString(), destination);
        }

        public void Send(ISerializable serializable, ChannelDestination destination)
        {
            Send(serializable, destination, false, "");
        }

        public void Send(object @object, ChannelDestination destination)
        {
            Send(@object, ((QueueDestination)destination).Name, false, "");
        }

        public void Send(object @object, ChannelDestination destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(@object, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }


        public void Send(XmlDocument document, string dest)
        {
            Send(document, dest, false, "");
        }

        public void Send(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                Message tibrvMsg = new Message();
                if (this.DataFormat == (eDataFormat)2)
                {
                    //System.IO.MemoryStream byteArrayOutputStream = new System.IO.MemoryStream();

                    //ObjectOutputStream objectOutputStream = new ObjectOutputStream(byteArrayOutputStream);
                    //objectOutputStream.writeObject(document);
                    //objectOutputStream.close();
                    //sbyte[] array = (sbyte[])(Array)byteArrayOutputStream.ToArray();

                    sbyte[] array = (sbyte[])(Array)Encoding.Default.GetBytes(document.OuterXml);
                    tibrvMsg.AddField(this.dataFieldName, array);
                }
                else
                {
                    tibrvMsg.UpdateField(this.dataFieldName, document.InnerXml);
                }
                tibrvMsg.SendSubject = dest;

                //bsw
                //tibrvMsg.SendSubject = "/ACS/SDV/ES/LISTENER";

                Transport.TibrvTransport.Send(tibrvMsg);
                //logger.Info("server message send" + System.Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

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

        public void Send(string message, string dest)
        {
            Send(message, dest, false, "");
        }

        public void Send(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
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
            finally
            {

            }
        }

        public void Send(System.Collections.IDictionary messages, string dest)
        {
            Send(messages, dest, false, "");
        }

        public void Send(byte[] message, string dest)
        {
            Send(message.ToString(), dest);
        }


        public void Send(ISerializable serializable, string dest)
        {
            Send(serializable, dest, false, "");
        }

        public void Send(object @object, string dest)
        {
            Send(@object, dest, false, "");
        }

        public void Send(object @object, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            if ((@object is Message))
            {
                try
                {
                    Message tibrvMsg = (Message)@object;
                    tibrvMsg.SendSubject = dest;
                    Transport.TibrvTransport.Send(tibrvMsg);
                    if (useCommunicationMessageNameForLogging)
                    {
                        //logger.fine(@object + " was sent to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                    }
                    else
                    {
                        //logger.fine(@object + " was sent to " + dest);
                    }
                }
                catch (RendezvousException e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + @object + "}", e);
                }
                catch (Exception e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + @object + "}", e);
                }
                finally
                {

                }
            }
            else if ((@object is ISerializable))
            {
                try
                {
                    Message tibrvMsg = new Message();
                    if (this.DataFormat == (eDataFormat)2)
                    {
                        //System.IO.MemoryStream byteArrayOutputStream = new System.IO.MemoryStream();
                        //ObjectOutputStream objectOutputStream = new ObjectOutputStream(byteArrayOutputStream);
                        //objectOutputStream.writeObject(@object);
                        //objectOutputStream.close();

                        //sbyte[] array = byteArrayOutputStream.toByteArray();

                        sbyte[] array = null;

                        IFormatter formatter = new BinaryFormatter();
                        using (MemoryStream stream = new MemoryStream())
                        {
                            formatter.Serialize(stream, @object);
                            array = (sbyte[])(Array)stream.ToArray();
                        }

                        tibrvMsg.AddField(this.dataFieldName, array);

                        tibrvMsg.SendSubject = dest;

                        Transport.TibrvTransport.Send(tibrvMsg);
                        if (useCommunicationMessageNameForLogging)
                        {
                            //logger.fine(@object.ToString() + " was sent to " + dest, communicationMessageName, useCommunicationMessageNameForLogging);
                        }
                        else
                        {
                            //logger.fine(@object.ToString() + " was sent to " + dest);
                        }
                    }
                    else
                    {
                        //logger.error("failed to send message, set useObject as true if you use this method");
                    }
                }
                catch (RendezvousException e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + @object.ToString() + "}", e);
                }
                catch (Exception e)
                {
                    //logger.error("failed to send message to dest{" + dest + "}, {" + @object.ToString() + "}", e);
                }
                finally
                {

                }
            }
            else
            {
                //logger.error("failed to send message to dest{" + dest + "}, {" + @object + "}, object should be an instance of " + typeof(TibrvMsg) + "|" + typeof(Serializable));
            }
        }

        public XmlDocument Request(XmlDocument document)
        {
            return Request(document, false, "");
        }

        public XmlDocument Request(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, ((QueueDestination)this.defaultDestination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument Request(XmlDocument document, string dest)
        {
            return Request(document, dest, false, "");
        }

        public XmlDocument Request(XmlDocument document, String dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, dest, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public XmlDocument Request(XmlDocument document, IDestination destination)
        public XmlDocument Request(XmlDocument document, ChannelDestination destination)
        {
            return Request(document, destination, false, "");
        }

        //public XmlDocument Request(XmlDocument document, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public XmlDocument Request(XmlDocument document, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(document, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument Request(XmlDocument document, long timeout)
        {
            return Request(document, timeout, false, "");
        }

        public XmlDocument Request(XmlDocument document, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(document, this.defaultDestination, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }


        //public XmlDocument Request(XmlDocument document, IDestination destination, long timeout)
        public XmlDocument Request(XmlDocument document, ChannelDestination destination, long timeout)
        {
            return Request(document, destination, this.timeout, false, "");
        }

        //public XmlDocument Request(XmlDocument document, IDestination destination, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public XmlDocument Request(XmlDocument document, ChannelDestination destination, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(document, ((QueueDestination)destination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument Request(XmlDocument document, string dest, long timeout)
        {
            return Request(document, dest, timeout, false, "");
        }

        public XmlDocument Request(XmlDocument document, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string replyMessage = Request(document.InnerXml, dest, timeout, useCommunicationMessageNameForLogging, communicationMessageName);

            if (string.ReferenceEquals(replyMessage, null))
            {
                //logger.Error("failed to request " + document.InnerXml);
                string messagename = document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText;

                if (messagename == "CONTROL-HEARTBEAT")
                {
                    logger.Error("failed to request :" + document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText + " : " + document.SelectSingleNode("/MESSAGE/DATA/APPLICATIONNAME").InnerText);
                }
                else
                {
                    logger.Error("failed to request " + document.InnerXml);
                }

                return null;
            }
            else
            {
                XmlDocument documentTemp = new XmlDocument();

                documentTemp.LoadXml(replyMessage);

                return documentTemp;
            }
            //return replyMessage != null ? .MakeDocument4Contents(replyMessage) : null;
        }


        public string Request(string message)
        {
            return Request(message, false, "");
        }

        public string Request(string message, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(message, ((QueueDestination)this.defaultDestination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, string dest)
        {
            return Request(message, dest, false, "");
        }

        public string Request(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, dest, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public string Request(string message, IDestination destination)
        public string Request(string message, ChannelDestination destination)
        {
            return Request(message, destination, false, "");
        }

        //public String Request(String message, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public String Request(String message, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(message, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public String Request(String message, long timeout)
        {
            return Request(message, timeout, false, "");
        }

        public String Request(String message, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(message, this.defaultDestination, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public String Request(String message, IDestination destination, long timeout)
        public String Request(String message, ChannelDestination destination, long timeout)
        {
            return Request(message, destination, timeout, false, "");
        }

        //public String Request(String message, IDestination destination, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public String Request(String message, ChannelDestination destination, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(message, ((QueueDestination)destination).Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public String Request(String message, String dest, long timeout)
        {
            return Request(message, dest, timeout, false, "");
        }

        public String Request(String message, String dest, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
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

        public object Request(object obj)
        {
            return Request(obj, false, "");
        }

        public object Request(object obj, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(obj, ((QueueDestination)this.defaultDestination).Name, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, String dest)
        {
            return Request(obj, dest, false, "");
        }

        public object Request(object obj, String dest, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(obj, dest, this.timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public object Request(object obj, IDestination destination)
        public object Request(object obj, ChannelDestination destination)
        {
            return Request(obj, destination, false, "");
        }

        //public object Request(object obj, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public object Request(object obj, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(obj, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, long timeout)
        {
            return Request(obj, timeout, false, "");
        }

        public object Request(object obj, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request((long)timeout, this.defaultDestination, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public object Request(object obj, IDestination destination, long timeout)
        public object Request(object obj, ChannelDestination destination, long timeout)
        {
            return Request(obj, destination, timeout, false, "");
        }

        //public object Request(object obj, IDestination destination, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public object Request(object obj, ChannelDestination destination, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            return Request(obj, ((QueueDestination)destination).Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, string dest, long timeout)
        {
            return Request(obj, dest, timeout, false, "");
        }

        public object Request(object obj, String dest, long timeout, bool useCommunicationMessageNameForLogging, String communicationMessageName)
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

                        sbyte[] array = null;

                        IFormatter formatter = new BinaryFormatter();
                        using (MemoryStream stream = new MemoryStream())
                        {
                            formatter.Serialize(stream, obj);
                            array = (sbyte[])(Array)stream.ToArray();
                        }

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

        public ISerializable Request(ISerializable serializable)
        {
            return Request(serializable, this.defaultDestination);
        }
        public ISerializable Request(ISerializable serializable, String dest)
        {
            object obj = Request(serializable, dest, this.timeout, false, "");
            if (obj is ISerializable)
            {
                return (ISerializable)obj;
            }
            return null;
        }

        //public ISerializable Request(ISerializable serializable, IDestination destination)
        public ISerializable Request(ISerializable serializable, ChannelDestination destination)
        {
            return Request(serializable, destination, this.timeout);
        }

        public ISerializable Request(ISerializable serializable, long timeout)
        {
            object obj = Request(serializable, this.defaultDestination, timeout, false, "");
            if (obj is ISerializable)
            {
                return (ISerializable)obj;
            }
            return null;
        }

        public ISerializable Request(ISerializable serializable, String dest, long timeout)
        {
            object obj = Request(serializable, dest, timeout, false, "");
            if (obj is ISerializable)
            {
                return (ISerializable)obj;
            }
            return null;
        }

        //public ISerializable Request(ISerializable serializable, IDestination destination, long timeout)
        public ISerializable Request(ISerializable serializable, ChannelDestination destination, long timeout)
        {
            object obj = Request(serializable, destination, timeout, false, "");
            if (obj is ISerializable)
            {
                return (ISerializable)obj;
            }
            return null;
        }


        public void Reply(XmlDocument secondary, XmlDocument primary)
        {
            Reply(secondary, primary, false, "");
        }

        public void Reply(XmlDocument secondary, XmlDocument primary, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            String dest = secondary.SelectSingleNode("/MESSAGE/ORIGINATED/ORIGINATEDNAME").InnerText;
            Reply(secondary, dest);
        }

        public void Reply(XmlDocument secondary, String dest)
        {
            Reply(secondary, dest, false, "");
        }

        public void Reply(XmlDocument secondary, String dest, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            Send(secondary, dest, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        //public void Reply(XmlDocument secondary, IDestination destination)
        public void Reply(XmlDocument secondary, ChannelDestination destination)
        {
            Reply(secondary, destination);
        }

        //public void Reply(XmlDocument secondary, IDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        public void Reply(XmlDocument secondary, ChannelDestination destination, bool useCommunicationMessageNameForLogging, String communicationMessageName)
        {
            Reply(secondary, ((QueueDestination)destination).Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }


        public void Reply(ISerializable serializable, String dest, String conversationId)
        {
            //logger.warn("not supported, please contact developer team");
        }

        //public void Reply(ISerializable serializable, IDestination destination, String conversationId)
        public void Reply(ISerializable serializable, ChannelDestination destination, String conversationId)
        {
            //logger.warn("not supported, please contact developer team");
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("tibrvSender{");
            sb.Append("destination=").Append(this.defaultDestination);
            sb.Append(", transport=").Append(this.transport);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", useDataField=").Append(this.useDataField);
            sb.Append(", dataFieldName=").Append(this.dataFieldName);
            sb.Append(", dataFormat=").Append(this.DataFormat).Append("{").Append(GetDataFormatTostring()).Append("}");
            sb.Append(", timeout=").Append(this.timeout);
            sb.Append("}");

            return sb.ToString();
        }
        static void AddObject(Message message, object data)
        {
            const string FIELD_NAME = "object";

            IFormatter formatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            formatter.Serialize(memoryStream, data);
            memoryStream.Close();
            message.AddField(FIELD_NAME, memoryStream.ToArray(), 0);
        }

    }


    [Serializable]
    class PersonalData
    {
        string firstName = "";
        string lastName = "";
        [NonSerialized] string ssn = "XXX-XX-XXXX";
        int age = 0;

        public PersonalData()
        {
        }

        public PersonalData(string firstName, string lastName, int age, string ssn)
        {
            this.firstName = firstName;
            this.lastName = lastName;
            this.age = age;
            this.ssn = ssn;
        }

        public override string ToString()
        {
            return this.lastName + ", " + this.firstName + " - " + this.age + " y.o. - SSN " + this.ssn;
        }
    }

}

