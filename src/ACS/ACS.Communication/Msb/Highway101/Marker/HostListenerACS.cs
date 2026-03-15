using com.miracom.transceiverx;
using com.miracom.transceiverx.session;
using ACS.Communication.Msb.Highway101.Message;
using ACS.Communication.Msb.Util;
using ACS.Communication.Util;
using ACS.Core.Message.Model;
using System;
using System.Text;
using System.Xml;
using ACS.Utility;

namespace ACS.Communication.Msb.Highway101.Marker
{
    public class HostListenerACS : GenericWorkflowHighway101Listener
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

                
                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;
                    MessageConverterUtils.SetOriginatedName(document, paramMessage.getChannel());

                    logger.Info("host message received"+ Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

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

                    logger.Info("host message received" + Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

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
                logger.Error("failed to handle highway101 message", e);
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

        //public HostListenerACS()
        //{

        //}
    }
}
