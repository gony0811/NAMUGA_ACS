using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ACS.Core.Message.Model;
using ACS.Communication.Msb.Highway101;
using ACS.Communication.Msb.RabbitMQ.Message;
using ACS.Communication.Msb.Util;
using ACS.Utility;
using System.Text.Json;


namespace ACS.Communication.Msb.RabbitMQ
{
    public abstract class AbstractRabbitMQListener : AbstractRabbitMQ, IMsbControllable
    {
        public ChannelDestination Destination { get; set; }
        public IModel ListenerChannel { get; set; }
        public IMsbConverter MsbConverter { get; set; }
        public String CorrelationId { get; set; }

        public List<ChannelDestination> RoutingDestinations { get; set; }


        private IBasicProperties props;

        private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();

        public AbstractRabbitMQListener()
        {

        }

        public override void Init()
        {
            base.Init();
        }

        public bool Open()
        {
            return Connection.IsOpen;
        }

        public abstract void OnMessage(XmlDocument document, string paramString);

        public abstract void OnMessage(AbstractMessage paramAbstractMessage);

        /// <summary>
        /// JSON 메시지 수신 시 호출. 서브클래스에서 오버라이드하여 처리.
        /// </summary>
        public virtual void OnJsonMessage(string jsonMessage, string dest)
        {
            logger.Warn($"JSON message received but OnJsonMessage not overridden. dest={dest}");
        }

        /// <summary>
        /// 메시지 문자열이 JSON인지 판별 ('{' 로 시작).
        /// </summary>
        protected static bool IsJsonMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.TrimStart()[0] == '{';
        }

        public bool Start()
        {
            try
            {
                //Stop();
                CreateListener();

                logger.Info((new StringBuilder("succeeded in initializing, ")).Append(this).ToString());
                return true;
            }
            catch (Exception e)
            {
                logger.Fatal("public bool start()" + e.ToString());
            }
            finally
            {

            }

            return true;
        }

        public bool Stop()
        {
            try
            {
                if (Session.IsOpen)
                {
                    Session.Close();
                    Session.Dispose();
                }

                if (Connection.IsOpen)
                {
                    Connection.Close(new TimeSpan(1000));
                    Connection.Dispose();
                }
            }
            catch (Exception e)
            {

            }

            return true;
        }

        public void CreateListener()
        {
            if (this.Destination is ChannelDestination)
            {
                Session = CreateSession();

                switch(CastOption)
                {
                    case CASTOPTION_UNICAST:
                        {
                            Session.QueueDeclare(Destination.Name, Durable, Exclusive, AutoDelete, null);
                            /*
                             * At this point we're sure that the task_queue queue won't be lost even if RabbitMQ restarts. 
                             * Now we need to mark our messages as persistent - by setting IBasicProperties.SetPersistent to true.
                             */
                            var properties = Session.CreateBasicProperties();
                            properties.Persistent = true;
                            var consumer = new EventingBasicConsumer(Session);
                            consumer.Received += OnUnicast;
                            Session.BasicConsume(queue: Destination.Name,
                                     autoAck: false,
                                     consumer: consumer);
                        }
                        break;
                    case CASTOPTION_MULTICAST:
                        {
                            Session.ExchangeDeclare(exchange: Destination.Name, type: ExchangeType.Fanout);
                            QueueName = Session.QueueDeclare().QueueName;
                            Session.QueueBind(queue: QueueName, exchange: Destination.Name, routingKey: "");

                            var consumer = new EventingBasicConsumer(Session);
                            consumer.Received += OnMulticast;
                            Session.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);
                        }
                        break;
                    case CASTOPTION_RPC_SERVER:
                        {
                            Session.QueueDeclare(queue: Destination.Name, durable: Durable, exclusive: Exclusive, autoDelete: AutoDelete, arguments: null);
                            Session.BasicQos(0, 1, false);
                            var consumer = new EventingBasicConsumer(Session);
                            Session.BasicConsume(queue: Destination.Name, autoAck: false, consumer: consumer);

                            consumer.Received += OnRequest;
                        }
                        break;
                    case CASTOPTION_RPC_CLIENT:
                        {
                            QueueName = Session.QueueDeclare().QueueName;
                            var consumer = new EventingBasicConsumer(Session);

                            props = Session.CreateBasicProperties();
                            CorrelationId = Guid.NewGuid().ToString();
                            props.CorrelationId = CorrelationId;
                            props.ReplyTo = QueueName;

                            consumer.Received += OnReply;
                        }
                        break;
                }
            }
            else
            {
                throw new Exception();
            }
        }

        public virtual void OnReply(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            if (ea.BasicProperties.CorrelationId == CorrelationId)
            {
                respQueue.Add(response);
            }
        }

        public virtual void OnRequest(object sender, BasicDeliverEventArgs ea)
        {
            // sender는 EventingBasicConsumer이므로 Model 속성에서 IModel을 가져옴
            IModel session = ((EventingBasicConsumer)sender).Model;
            var body = ea.Body.ToArray();
            var props = ea.BasicProperties;
            var replyProps = session.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;

            var message = Encoding.UTF8.GetString(body);

            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + Destination.Name + "}, received message{" + message + "}");
                }

                string dest = Destination.Name;

                // JSON 메시지 감지: '{' 로 시작하면 JSON으로 처리
                if (IsJsonMessage(message))
                {
                    logger.Info($"RPC received JSON message from {dest}");
                    OnJsonMessage(message, dest);
                }
                else
                {
                    object obj = null;

                    if (this.MsbConverter != null)
                    {
                        obj = MessageConverterUtils.GetMessageBasedOnXmlString(body, MsbConverter);
                    }
                    else
                    {
                        obj = MessageConverterUtils.GetMessageBasedOnXmlString(body);
                    }

                    if (obj is ExtendDocument)
                    {
                        ExtendDocument document = (ExtendDocument)obj;

                        document.putExtendDataElement("PRIMARYMESSAGE", message);
                        MessageConverterUtils.SetOriginatedName(document, dest);

                        OnMessage(document, dest);
                    }
                    else if (obj is AbstractMessage)
                    {
                        AbstractMessage abstractMessage = (AbstractMessage)obj;
                        string transactionId = IdGeneratorUtils.RandomTransactionId();
                        abstractMessage.TransactionId = transactionId;

                        if (String.IsNullOrEmpty(abstractMessage.ConversationId))
                        {
                            abstractMessage.ConversationId = transactionId;
                        }

                        if (String.IsNullOrEmpty(abstractMessage.OriginatedName))
                        {
                            abstractMessage.OriginatedName = dest;
                        }
                        OnMessage(abstractMessage);
                    }
                    else if (obj is XmlDocument)
                    {
                        XmlDocument document = (XmlDocument)obj;
                        MessageConverterUtils.SetOriginatedName(document, this.QueueName);

                        logger.Info("received message : " + document.InnerXml);

                        OnMessage(document, dest);
                    }
                    else
                    {
                        logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"OnRequest error: {e.Message}", e);
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(message);
                session.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                  basicProperties: replyProps, body: responseBytes);
                session.BasicAck(deliveryTag: ea.DeliveryTag,
                  multiple: false);
            }
        }

        public virtual void OnMulticast(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                IModel session = sender as IModel;
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                object obj = null;
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + Destination + "}, received message{" + body + "}");
                }

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(body, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(body);
                }

                string dest = this.QueueName;

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, this.QueueName);

                    logger.Info("received message : " + document.InnerXml);

                    OnMessage(document, dest);
                }
                else if (obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;
                    string transactionId = IdGeneratorUtils.RandomTransactionId();
                    abstractMessage.TransactionId = transactionId;

                    if (String.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if (String.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = this.QueueName;
                    }
                    logger.Info("received message : " + abstractMessage.MessageName);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }

            }
            catch (Exception ex)
            {
                logger.Error("failed to handle transceiver message", ex);
            }
        }

        public virtual void OnUnicast(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                IModel session = sender as IModel;
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + Destination + "}, received message{" + body + "}");
                }

                string dest = this.QueueName;

                // JSON 메시지 감지: '{' 로 시작하면 JSON으로 처리
                if (IsJsonMessage(message))
                {
                    logger.Info("received JSON message : " + (message.Length > 200 ? message.Substring(0, 200) + "..." : message));
                    OnJsonMessage(message, dest);
                    return;
                }

                object obj = null;
                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(body, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(body);
                }

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, this.QueueName);

                    logger.Info("received message : " + document.InnerXml);

                    OnMessage(document, dest);
                }
                else if (obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;
                    string transactionId = IdGeneratorUtils.RandomTransactionId();
                    abstractMessage.TransactionId = transactionId;

                    if (String.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if (String.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = this.QueueName;
                    }
                    logger.Info("received message : " + abstractMessage.MessageName);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }
            }
            catch (Exception ex)
            {
                logger.Error("failed to handle transceiver message", ex);
            }
            finally
            {
                Session.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }

        public string GetMsbControllerName()
        {
            return "rabbitmq";
        }
    }
}
