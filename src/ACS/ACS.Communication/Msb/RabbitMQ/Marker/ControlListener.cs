using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Utility;
using System.Xml;
using RabbitMQ.Client;
using ACS.Communication.Msb.Util;
using RabbitMQ.Client.Events;
using ACS.Core.Message.Model;
using ACS.Communication.Msb.RabbitMQ.Message;

namespace ACS.Communication.Msb.RabbitMQ.Marker
{
    public class ControlListener : GenericWorkflowRabbitMQListener
    {
        public override void OnUnicast(object sender, BasicDeliverEventArgs ea)
        {
            IModel session = sender as IModel;
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + session.ChannelNumber.ToString() + "}, received message{" + message + "}");
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

                string dest = Destination.Name;

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, Destination.Name);
                    logger.Info("server message received" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));
                    //logger.Info("received message : " + document.InnerXml);

                    //logger.well(XmlUtils.toStringPrettyFormatWithoutDeclaration(document), transactionId, messageName, carrierName, transportCommandId, currentMachineName, currentUnitName, messageName);

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
                        abstractMessage.OriginatedName = Destination.Name;
                    }
                    //logger.receive(abstractMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }
            }
            catch(Exception e)
            {
                logger.Error("failed to handle transceiver message", e);
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

            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" +ea.BasicProperties.AppId + "}, received message{" + message + "}");
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

                string dest = String.IsNullOrEmpty(Destination.Name) ? "" : Destination.Name;

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, Destination.Name);

                    logger.Info("received message : " + document.InnerXml);

                    //logger.well(XmlUtils.toStringPrettyFormatWithoutDeclaration(document), transactionId, messageName, carrierName, transportCommandId, currentMachineName, currentUnitName, messageName);

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
                        abstractMessage.OriginatedName = Destination.Name;
                    }
                    //logger.receive(abstractMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + message);
                }

            }
            catch (Exception e)
            {
                logger.Error("failed to handle transceiver message", e);
            }
        }

        public override void OnRequest(object sender, BasicDeliverEventArgs ea)
        {
            IModel session = sender as IModel;

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + ea.BasicProperties.AppId + "}, received message{" + message + "}");
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
                        abstractMessage.OriginatedName = ea.BasicProperties.AppId;
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
