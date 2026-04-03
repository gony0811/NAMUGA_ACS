using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ACS.Communication.Msb.RabbitMQ.Message;
using ACS.Communication.Msb.Util;
using ACS.Core.Message.Model;
using ACS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.RabbitMQ.Marker
{
    public class ESListener : GenericWorkflowRabbitMQListener
    {
        public override void OnUnicast(object sender, BasicDeliverEventArgs ea)
        {
            IModel session = sender as IModel;
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var originatedName = ea.BasicProperties.AppId;

            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + originatedName + "}, received message{" + message + "}");
                }

                // JSON 메시지 감지: '{' 로 시작하면 JSON으로 처리
                string trimmed = message.TrimStart();
                if (trimmed.StartsWith("{"))
                {
                    OnJsonMessage(message);
                    return;
                }

                // 기존 XML 메시지 처리
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

                    if (String.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if (String.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = originatedName;
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
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + originatedName + "}, received message{" + message + "}");
                }

                // JSON 메시지 감지
                string trimmed = message.TrimStart();
                if (trimmed.StartsWith("{"))
                {
                    OnJsonMessage(message);
                    return;
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
                        abstractMessage.OriginatedName = originatedName;
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
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + originatedName + "}, received message{" + message + "}");
                }

                // JSON 메시지 감지
                string trimmed = message.TrimStart();
                if (trimmed.StartsWith("{"))
                {
                    OnJsonMessage(message);
                    return;
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

                    if (String.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if (String.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = originatedName;
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
                logger.Error(e.Message, e);
            }
            finally
            {

            }
        }

        /// <summary>
        /// JSON 형식 메시지 처리. header.messageName으로 워크플로우를 라우팅하고
        /// JSON 원본 문자열을 Arguments로 전달한다.
        /// </summary>
        private void OnJsonMessage(string json)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(json);
                string messageName = jsonDoc.RootElement
                    .GetProperty("header").GetProperty("messageName").GetString();
                string transactionId = jsonDoc.RootElement
                    .GetProperty("header").GetProperty("transactionId").GetString();

                logger.Info($"JSON message received: messageName={messageName}, transactionId={transactionId}");

                ExecuteWorkflow(transactionId, messageName, new object[] { json });
            }
            catch (Exception e)
            {
                logger.Error("JSON 메시지 처리 오류: " + json, e);
            }
        }
    }
}
