using com.miracom.transceiverx;
using com.miracom.transceiverx.session;
using ACS.Communication.Msb.Highway101.Message;
using ACS.Communication.Msb.Util;
using ACS.Communication.Util;
using ACS.Framework.Message.Model;
using ACS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Communication.Msb.Highway101.Marker
{
    public class DaemonListener : GenericWorkflowHighway101Listener
    {
        public override void onUnicast(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + paramMessage.getChannel() + "}, received message{" + paramMessage.ToString() + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);
                }

                string dest = String.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

                    string messageName = XmlUtility.GetDataFromXml(document, "/Msg/Command");
                    string transactionId = XmlUtility.GetDataFromXml(document, "//TransactionID");
                    string carrierName = XmlUtility.GetDataFromXml(document, "//CarrierID");
                    string transportCommandId = XmlUtility.GetDataFromXml(document, "//JobID");
                    string currentMachineName = XmlUtility.GetDataFromXml(document, "//SourceLoc");
                    string currentUnitName = XmlUtility.GetDataFromXml(document, "//SourcePort");

                    logger.Info("server message received" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));
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
                        abstractMessage.OriginatedName = paramMessage.getChannel();
                    }
                    //logger.receive(abstractMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage);
                }
            }
            catch (TrxException e)
            {
                logger.Error("error");
            }
            catch (Exception e)
            {
                logger.Error("failed to handle transceiver message", e);
            }


            //try
            //{
            //    string dest = String.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

            //    object obj = null;
            //    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);

            //    XmlDocument document = (XmlDocument)obj;
            //    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

            //    //logger.receive(document);
            //    OnMessage(document, dest);
            //}
            //catch (TrxException e)
            //{

            //}
            //catch (Exception e)
            //{

            //}
            //finally
            //{

            //}
            
        }

        public override void onMulticast(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
        {
            try
            {

                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + paramMessage.getChannel() + "}, received message{" + paramMessage.ToString() + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);
                }

                string dest = String.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

                    string messageName = XmlUtility.GetDataFromXml(document, "/Msg/Command");
                    string transactionId = XmlUtility.GetDataFromXml(document, "//TransactionID");
                    string carrierName = XmlUtility.GetDataFromXml(document, "//CarrierID");
                    string transportCommandId = XmlUtility.GetDataFromXml(document, "//JobID");
                    string currentMachineName = XmlUtility.GetDataFromXml(document, "//SourceLoc");
                    string currentUnitName = XmlUtility.GetDataFromXml(document, "//SourcePort");

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
                        abstractMessage.OriginatedName = paramMessage.getChannel();
                    }
                    //logger.receive(abstractMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage);
                }
            }
            catch (TrxException e)
            {
                logger.Error("error");
            }
            catch (Exception e)
            {
                logger.Error("failed to handle transceiver message", e);
            }


            //try
            //{
            //    string dest = String.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

            //    object obj = null;
            //    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);

            //    XmlDocument document = (XmlDocument)obj;
            //    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

            //    //logger.receive(document);
            //    OnMessage(document, dest);
            //}
            //catch (TrxException e)
            //{

            //}
            //catch (Exception e)
            //{

            //}
            //finally
            //{

            //}
            
        }

        public override void onRequest(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + paramMessage.getChannel() + "}, received message{" + paramMessage.ToString() + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage, MsbConverter);
                }
                else
                {
                    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);
                }

                string dest = String.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

                if (obj is ExtendDocument)
                {
                    ExtendDocument document = (ExtendDocument)obj;

                    document.putExtendDataElement("PRIMARYMESSAGE", paramMessage);
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
                        abstractMessage.OriginatedName = paramMessage.getChannel();
                    }

                    logger.Info(abstractMessage.ReceivedMessage);
                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage.ToString());
                }
            }
            catch (TrxException e)
            {
                logger.Error(e.Message, e);
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
            }
            finally
            {

            }


            //try
            //{
            //    string dest = String.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

            //    object obj = null;
            //    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);

            //    XmlDocument document = (XmlDocument)obj;
            //    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

            //    //logger.receive(document);
            //    OnMessage(document, dest);
            //}
            //catch (TrxException e)
            //{

            //}
            //catch (Exception e)
            //{

            //}
            //finally
            //{

            //}
        }
    }
}
