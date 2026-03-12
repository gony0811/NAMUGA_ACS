using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ACS.Communication.Msb.RabbitMQ.Message;
using ACS.Communication.Msb.Util;

namespace ACS.Communication.Msb.RabbitMQ
{
    public class GenericRabbitMQSender : AbstractRabbitMQ, IMessageAgent, ISynchronousMessageAgent
    {      
        private EventingBasicConsumer consumer;
        private BlockingCollection<string> respQueue = new BlockingCollection<string>();
        private IBasicProperties props;
        public ChannelDestination DefaultDestination { get; set; }

        public override void Init()
        {
            string destinationName = DefaultDestination.Name.Replace(".", "/");

            if (!destinationName.StartsWith("/"))
            {
                destinationName = "/" + destinationName;
            }

            DefaultDestination.Name = destinationName;

            try
            {
                this.Session = CreateSession();

                if (this.Session == null)
                {
                    throw new ApplicationException(
                        $"[ACS] RabbitMQ 세션을 생성할 수 없습니다. RabbitMQ 서버가 실행 중인지 확인하세요.");
                }

                switch (CastOption)
                {
                    case CASTOPTION_UNICAST:
                        {
                            Session.QueueDeclare(queue: DefaultDestination.Name, durable: Durable, exclusive: Exclusive, autoDelete: AutoDelete, arguments: null);
                            /*
                             * At this point we're sure that the task_queue queue won't be lost even if RabbitMQ restarts. 
                             * Now we need to mark our messages as persistent - by setting IBasicProperties.SetPersistent to true.
                            */
                            var properties = Session.CreateBasicProperties();
                            properties.Persistent = true;
                        }
                        break;
                    case CASTOPTION_MULTICAST:
                        {
                            Session.ExchangeDeclare(exchange: DefaultDestination.Name, type: ExchangeType.Fanout);
                        }
                        break;
                    case CASTOPTION_RPC_CLIENT:
                        {
                            QueueName = Session.QueueDeclare().QueueName;
                            consumer = new EventingBasicConsumer(Session);

                            props = Session.CreateBasicProperties();
                            var correlationId = Guid.NewGuid().ToString();
                            props.CorrelationId = correlationId;
                            props.ReplyTo = QueueName;

                            consumer.Received += (model, ea) =>
                            {
                                var body = ea.Body.ToArray();
                                var response = Encoding.UTF8.GetString(body);
                                if (ea.BasicProperties.CorrelationId == correlationId)
                                {
                                    respQueue.Add(response);
                                }
                            };
                        }
                        break;              
                }

            }
            catch (Exception e)
            {
                // MsbStartException;
                throw e;
            }
        }

        public void Send(XmlDocument document)
        {
            Send(document, false, "");
        }

        public void Send(string message)
        {
            Send(message, false, "");
        }

        public void Send(IDictionary messages)
        {
            //Not Supported
        }

        public void Send(byte[] message)
        {
            Send(message, this.DefaultDestination.Name);
        }

        public void Send(ISerializable paramSerializable)
        {
            Send(paramSerializable, this.DefaultDestination.Name);
        }

        public void Send(object paramObject)
        {
            Send(paramObject, false, "");
        }

        public void Send(XmlDocument document, string destination)
        {
            Send(document, destination, false, "");
        }

        public void Send(string message, string dest)
        {
            Send(message, dest, false, "");
        }

        public void Send(IDictionary paramMap, string paramstring)
        {
            throw new NotImplementedException();
        }

        public void Send(byte[] paramArrayOfByte, string paramstring)
        {
            throw new NotImplementedException();
        }

        public void Send(ISerializable paramSerializable, string paramstring)
        {
            throw new NotImplementedException();
        }

        public void Send(object paramObject, string paramstring)
        {
            throw new NotImplementedException();
        }

        public void Send(string paramstring1, bool paramBoolean, string paramstring2)
        {
            throw new NotImplementedException();
        }

        public void Send(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                switch(CastOption)
                {
                    case CASTOPTION_UNICAST:
                        {
                            var body = Encoding.UTF8.GetBytes(message);
                            Session.BasicPublish(exchange: "", routingKey: dest, basicProperties: null, body: body);                            
                        }
                        break;
                    case CASTOPTION_MULTICAST:
                        {
                            var body = Encoding.UTF8.GetBytes(message);
                            Session.BasicPublish(exchange: dest, routingKey: "", basicProperties: null, body: body);
                        }
                        break;
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
            catch(Exception e)
            {
                logger.Error("failed to send message{" + message + "}", e);
            }
            finally
            {

            }
        }

        public void Send(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMeesageName)
        {
            Send(document, this.DefaultDestination.Name, useCommunicationMessageNameForLogging, communicationMeesageName);
        }

        public void Send(XmlDocument document, string destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                switch (CastOption)
                {
                    case CASTOPTION_UNICAST:
                        {
                            var body = Encoding.UTF8.GetBytes(document.InnerXml);
                            Session.BasicPublish(exchange: "", routingKey: destination, basicProperties: null, body: body);
                        }
                        break;
                    case CASTOPTION_MULTICAST:
                        {
                            var body = Encoding.UTF8.GetBytes(document.InnerXml);
                            Session.BasicPublish(exchange: destination, routingKey: "", basicProperties: null, body: body);
                        }
                        break;
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
            catch (Exception e)
            {
                logger.Error("failed to send message{" + document.InnerXml + "}", e);
            }
            finally
            {

            }
        }

        public void Send(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            Send(obj, this.DefaultDestination.Name, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public void Send(object obj, string destination, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                if (!(obj is string)) return;

                switch (CastOption)
                {
                    case CASTOPTION_UNICAST:
                        {
                            var body = Encoding.UTF8.GetBytes(obj as string);
                            Session.BasicPublish(exchange: "", routingKey: destination, basicProperties: null, body: body);
                        }
                        break;
                    case CASTOPTION_MULTICAST:
                        {
                            var body = Encoding.UTF8.GetBytes(obj as string);
                            Session.BasicPublish(exchange: destination, routingKey: "", basicProperties: null, body: body);
                        }
                        break;
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
            catch (Exception e)
            {
                logger.Error("failed to send message{" + obj + "}", e);
            }
            finally
            {

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

        public XmlDocument Request(XmlDocument document, long timeout)
        {
            return Request(document, timeout, false, "");
        }

        public XmlDocument Request(XmlDocument document, string dest, long timeout)
        {
            return Request(document, dest, timeout, false, "");
        }

        public string Request(string message)
        {
            return Request(message, false, "");
        }

        public string Request(string message, string dest)
        {
            return Request(message, dest, false, "");
        }

        public string Request(string message, long timeout)
        {
            return Request(message, timeout, false, "");
        }

        public string Request(string message, string dest, long timeout)
        {
            return Request(message, dest, timeout, false, "");
        }

        public object Request(object obj)
        {
            return Request(obj, false, "");
        }

        public object Request(object obj, string dest)
        {
            return Request(obj, dest, false, "");
        }

        public object Request(object obj, long timeout)
        {
            return Request(obj, timeout, false, "");
        }

        public object Request(object obj, string dest, long timeout)
        {
            return Request(obj, dest, timeout, false, "");
        }

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

        public XmlDocument Request(XmlDocument document, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, this.DefaultDestination.Name, this.DefaultTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument Request(XmlDocument document, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, dest, this.DefaultTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument Request(XmlDocument document, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(document, this.DefaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public XmlDocument Request(XmlDocument document, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string replyMessage = Request(document.InnerXml, dest, timeout, useCommunicationMessageNameForLogging, communicationMessageName);

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

        public string Request(string message, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, this.DefaultDestination.Name, this.DefaultTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, dest, this.DefaultTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(message, this.DefaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public string Request(string message, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string replyMessage = string.Empty;

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


                var body = Encoding.UTF8.GetBytes(message);

                Session.BasicPublish(exchange: "", routingKey: dest, basicProperties: props, body: body);
                Session.BasicConsume(consumer: consumer, queue: QueueName, autoAck: true);


                replyMessage = respQueue.Take();

                if (replyMessage == null)
                {
                    logger.Error("failed to get reply message, timeout{" + this.DefaultTTL + "} occurred");
                    return null;
                }

                return replyMessage;
            }
            catch(Exception e)
            {
                return replyMessage;
            }
            finally
            {

            }
        }

        public object Request(object obj, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return this.Request(obj, this.DefaultDestination.Name, this.DefaultTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, string dest, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return this.Request(obj, dest, this.DefaultTTL, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            return Request(obj, this.DefaultDestination.Name, timeout, useCommunicationMessageNameForLogging, communicationMessageName);
        }

        public object Request(object obj, string dest, long timeout, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            string replyMessage = string.Empty;

            if (obj is string)
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

                    var body = Encoding.UTF8.GetBytes(obj as string);

                    Session.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: props, body: body);
                    Session.BasicConsume(consumer: consumer, queue: QueueName, autoAck: true);

                    replyMessage = respQueue.Take();

                    if (replyMessage == null)
                    {
                        logger.Error("failed to get reply message, timeout{" + this.DefaultTTL + "} occurred");
                        return null;
                    }

                    return replyMessage;
                }
                catch (Exception e)
                {
                    logger.Error("failed to request " + obj, e);
                    return null;
                }
            }
            return null;
        }


        public void Reply(XmlDocument secondary, XmlDocument primary)
        {
            Reply(secondary, primary, false, "");
        }

        public void Reply(XmlDocument secondary, string dest) 
        {
        }

        public void Reply(ISerializable serializable, string dest, string conversationId)
        {
            logger.Warn("not supported, please contact developer team");
        }

        public void Reply(XmlDocument secondary, XmlDocument primary, bool useCommunicationMessageNameForLogging, string communicationMessageName)
        {
            try
            {
                object requestMessage = ((ExtendDocument)primary).ExtendDataElement("PRIMARYMESSAGE");
                string replyMessage = string.Empty;

            }
            catch (Exception e)
            {
                // logger
            }
        }

        public void Reply(XmlDocument paramDocument, string paramstring1, bool paramBoolean, string paramstring2)
        {
            throw new NotImplementedException();
        }
    }
}
