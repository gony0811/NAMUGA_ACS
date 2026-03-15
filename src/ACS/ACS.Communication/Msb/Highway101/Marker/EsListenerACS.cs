using com.miracom.transceiverx.session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.miracom.transceiverx.message;
using com.miracom.transceiverx;
using ACS.Communication.Msb.Util;
using System.Xml;
using ACS.Communication.Util;
using ACS.Utility;
using ACS.Core.Message.Model;
using ACS.Communication.Msb.Highway101.Message;

namespace ACS.Communication.Msb.Highway101.Marker
{
    public class EsListenerACS : GenericWorkflowHighway101Listener
    {
        public override void onUnicast(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
        {
            try
            {
                if(logger.IsDebugEnabled)
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

                if(obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

                    logger.Info("server message received" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

                   OnMessage(document, dest);
                }
                else if(obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;
                    string transactionId = IdGeneratorUtils.RandomTransactionId();
                    abstractMessage.TransactionId = transactionId;

                    if(string.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = transactionId;
                    }

                    if(string.IsNullOrEmpty(abstractMessage.OriginatedName))
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
            //    object obj = null;
            //    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);
            //    string dest = string.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

            //    if (obj is XmlDocument)
            //    {
            //        XmlDocument document = (XmlDocument)obj;
            //        //MessageConverterUtils.setOriginatedName(document, paramMessage.Channel);
                    
            //        string transactionId = IdGeneratorUtils.randomTransactionId();
            //        //string messageName = XmlUtils.getXmlData(document, "//MESSAGENAME");
            //        //string currentUnitName = XmlUtils.getXmlData(document, "//VEHICLEID");
            //        //string machineName = XmlUtils.getXmlData(document, "//TAIL").Trim();

            //        //logger.well(XmlUtils.tostringPrettyFormatWithoutDeclaration(document), transactionId, messageName, "", "", machineName, currentUnitName, messageName);

            //        OnMessage(document, dest);
            //    }
            //    else if (obj is AbstractMessage)
            //    {
            //        AbstractMessage abstractMessage = (AbstractMessage)obj;
            //        string transactionId = IdGeneratorUtils.randomTransactionId();
            //        abstractMessage.TransactionId = transactionId;

            //        if (string.IsNullOrEmpty(abstractMessage.ConversationId))
            //        {
            //            abstractMessage.ConversationId = transactionId;
            //        }

            //        if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
            //        {
            //            abstractMessage.OriginatedName = paramMessage.getChannel();
            //        }
            //        //logger.receive(abstractMessage);
            //        OnMessage(abstractMessage);
            //    }
            //    else
            //    {
            //        //logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage);
            //    }

            //}
            //catch (TrxException e)
            //{
            //    //logger.Error("error");
            //}
            //catch (Exception e)
            //{
            //    //logger.Error("failed to handle transceiver message", e);
            //}
            //finally
            //{

            //}
        }

        public override void onMulticast(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
        {

            try
            {
                if(logger.IsDebugEnabled)
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

                if(obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;

                    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

                    //logger.receive(document);

                    OnMessage(document, dest);
                }
                else if(obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;

                    string transactionId = IdGeneratorUtils.RandomTransactionId();
                    abstractMessage.TransactionId = transactionId;

                    if(string.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = paramMessage.getChannel();
                    }

                    OnMessage(abstractMessage);
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage.ToString());
                }
            }
            catch(TrxException e)
            {
                logger.Error(e.Message, e);
            }
            catch (Exception e)
            {

                logger.Error(e.Message, e);
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

            //try
            //{
            //    object obj = null;

            //    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);

            //    string dest = string.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

            //    if (obj is XmlDocument)
            //    {
            //        XmlDocument document = (XmlDocument)obj;
            //        //MessageConverterUtils.setOriginatedName(document, paramMessage.Channel);
            //        //logger.receive(document);
            //        OnMessage(document, dest);
            //    }
            //    else if (obj is AbstractMessage)
            //    {
            //        AbstractMessage abstractMessage = (AbstractMessage)obj;
            //        string transactionId = IdGeneratorUtils.randomTransactionId();
            //        abstractMessage.TransactionId = transactionId;

            //        if (string.IsNullOrEmpty(abstractMessage.ConversationId))
            //        {
            //            abstractMessage.ConversationId = transactionId;
            //        }

            //        if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
            //        {
            //            abstractMessage.OriginatedName = paramMessage.getChannel();
            //        }

            //        //logger.receive(abstractMessage);
            //        OnMessage(abstractMessage);
            //    }
            //    else
            //    {
            //        //logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage);
            //    }
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
            //    //document.putExtendDataElement("PRIMARYMESSAGE", paramMessage);
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


            //try
            //{
            //    object obj = null;
            //    obj = MessageConverterUtils.GetMessageBasedOnXmlString(paramMessage);

            //    string dest = string.IsNullOrEmpty(paramSession.getReplyChannel()) ? "" : paramSession.getReplyChannel();

            //    if (obj is ExtendDocument)
            //    {
            //        ExtendDocument document = (ExtendDocument)obj;
            //        document.putExtendDataElement("PRIMARYMESSAGE", paramMessage);
            //        //MessageConverterUtils.setOriginatedName(document, dest);

            //        //logger.receive(document);
            //        OnMessage(document, dest);
            //    }
            //    else if (obj is AbstractMessage)
            //    {
            //        AbstractMessage abstractMessage = (AbstractMessage)obj;
            //        string transactionId = IdGeneratorUtils.randomTransactionId();
            //        abstractMessage.TransactionId = transactionId;

            //        if (string.IsNullOrEmpty(abstractMessage.ConversationId))
            //        {
            //            abstractMessage.ConversationId = transactionId;
            //        }

            //        if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
            //        {
            //            abstractMessage.OriginatedName = paramMessage.getChannel();
            //        }
            //        //logger.receive(abstractMessage);
            //        OnMessage(abstractMessage);
            //    }
            //    else
            //    {
            //        //logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + paramMessage);
            //    }
            //}
            //catch (TrxException e)
            //{
            //    //logger.Error("error");
            //}
            //catch (Exception e)
            //{
            //    //logger.Error("failed to handle highway101 message", e);
            //}
        }
    }

}
