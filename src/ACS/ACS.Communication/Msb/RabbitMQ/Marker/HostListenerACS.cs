using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ACS.Communication.Msb.RabbitMQ.Message;
using ACS.Communication.Msb.Util;
using ACS.Framework.Message.Model;
using ACS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.RabbitMQ.Marker
{
    public class HostListenerACS : GenericWorkflowRabbitMQListener
    {
        public override void OnUnicast(object sender, BasicDeliverEventArgs ea)
        {
            IModel session = sender as IModel;
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var originatedName = ea.BasicProperties.AppId;

            try
            {
                if (logger.Log.IsDebugEnabled)
                {
                    logger.Debug("destination{" + originatedName + "}, received message{" + message + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(message, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(message);
                }

                string dest = String.IsNullOrEmpty(ea.BasicProperties.AppId) ? "" : ea.BasicProperties.AppId;

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, originatedName);

                    logger.Info("server message received" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

                    OnMessage(document, dest);
                }
                else if (obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;
                    string transactionId = IdGeneratorUtils.RandomTransactionId();
                    abstractMessage.TransactionId = transactionId;

                    if (string.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = originatedName;
                    }

                    logger.Info(abstractMessage.ReceivedMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }
            }
            catch(Exception e)
            {
                logger.Error(e.Message, e);
            }
            finally
            {

            }
        }

        public override void OnMulticast(object sender, BasicDeliverEventArgs ea)
        {
            IModel session = sender as IModel;
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var originatedName = ea.BasicProperties.AppId;

            try
            {
                if (logger.Log.IsDebugEnabled)
                {
                    logger.Debug("destination{" + originatedName + "}, received message{" + message + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(message, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(message);
                }

                string dest = String.IsNullOrEmpty(ea.BasicProperties.AppId) ? "" : ea.BasicProperties.AppId;

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;

                    MessageConverterUtils.SetOriginatedName(document, message);

                    //logger.receive(document);

                    OnMessage(document, dest);
                }
                else if (obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;

                    string transactionId = IdGeneratorUtils.RandomTransactionId();
                    abstractMessage.TransactionId = transactionId;

                    if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = originatedName;
                    }

                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
            }
            finally
            {

            }
        }

        public override void OnRequest(object sender, BasicDeliverEventArgs ea)
        {
            IModel session = sender as IModel;
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var originatedName = ea.BasicProperties.AppId;

            try
            {
                if (logger.Log.IsDebugEnabled)
                {
                    logger.Debug("destination{" + originatedName + "}, received message{" + message + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(message, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(message);
                }

                string dest = String.IsNullOrEmpty(ea.BasicProperties.AppId) ? "" : ea.BasicProperties.AppId;

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

                    if (string.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = originatedName;
                    }

                    logger.Info(abstractMessage.ReceivedMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
            }
            finally
            {

            }
        }
    }
}
