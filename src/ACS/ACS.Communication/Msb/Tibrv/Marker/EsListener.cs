using ACS.Communication.Msb.Util;
using ACS.Core.Message.Model;
using ACS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv.Marker
{
    public class EsListener : GenericWorkflowTibrvCmListener
    {
        private IList reconcileMessageReplyNameList;
        private const string reconcileAckName_ACK = "ACK";
        private const string reconcileAckName_EAC = "EAC";
        private const string reconcileAckName_ONLACK = "ONLACK";
        private const string reconcileAckName_COMMACK = "COMMACK";
        private const string reconcile_AckValue_Success = "0";
        private const string reconcile_AckValue_ONLACK_AlreayOnline = "2";

        public IList ReconcileMessageReplyNameList
        {
            get
            {
                return this.reconcileMessageReplyNameList;
            }
            set
            {
                this.reconcileMessageReplyNameList = value;
            }
        }

        int kk = 0;

        public override void onMsg(object tibrvListener, MessageReceivedEventArgs tibrvMsg)
        {
            Debug.WriteLine("EsListener:" + kk++);

            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("destination{" + ((QueueDestination)this.destination).Name + "}, received message{" + tibrvMsg.ToString() + "}");
                }

                object obj = null;

                if (this.MsbConverter != null)
                {
                    if (this.UseDataField)
                    {
                        if (this.DataFormat == (eDataFormat)0)
                        {
                            obj = MessageConverterUtilsEx.GetMessageBasedOnXmlString(tibrvMsg.Message, this.DataFieldName, this.MsbConverter);
                        }
                        else if (this.DataFormat == (eDataFormat)1)
                        {
                            obj = MessageConverterUtilsEx.GetMessageBasedOnPlainString(tibrvMsg.Message, this.DataFieldName, this.MsbConverter);
                        }
                        else
                        {
                            logger.Error("can not convert, messageDataFormat should be [XML_STRING|PLAIN_STRING] if you use msbConverter");
                        }
                    }
                    else
                    {
                        obj = MessageConverterUtilsEx.GetMessage(tibrvMsg.Message, this.MsbConverter);
                    }
                }
                else if (this.UseDataField)
                {
                    if (this.DataFormat == (eDataFormat)0)
                    {
                        obj = MessageConverterUtilsEx.GetMessageBasedOnXmlString(tibrvMsg.Message, this.DataFieldName);
                    }
                    else if (this.DataFormat == (eDataFormat)1)
                    {
                        obj = MessageConverterUtilsEx.GetMessageBasedOnPlainString(tibrvMsg.Message, this.DataFieldName);
                    }
                    else if (this.DataFormat == (eDataFormat)2)
                    {
                        obj = MessageConverterUtilsEx.GetMessageBasedOnObject(tibrvMsg.Message, this.DataFieldName);
                    }
                    else
                    {
                        logger.Error("can not convert, messageDataFormat should be [XML_STRING|PLAIN_STRING|OBJECT]");
                    }
                }
                else
                {
                    obj = MessageConverterUtilsEx.GetMessage(tibrvMsg.Message);
                }

                string dest = String.IsNullOrEmpty(tibrvMsg.Message.ReplySubject) ? "" : tibrvMsg.Message.ReplySubject;
                bool awakeMessage = false;

                if (obj is XmlDocument)
                {
                    XmlDocument document = (XmlDocument)obj;

                    XmlElement header = document.DocumentElement["HEADER"];
                    if (header == null)
                    {
                        logger.Error("document format is not valid, HEADER should exist");
                        return;
                    }
                    string messageName = header["MESSAGENAME"] != null ? header["MESSAGENAME"].InnerText : "";

                    XmlElement elementTransactionId = header["TRANSACTIONID"];

                    if (elementTransactionId == null)
                    {
                        logger.Error("document format is not valid, TRANSACTIONID should exist");
                        return;
                    }

                    string transactionId = Guid.NewGuid().ToString();
                    elementTransactionId.InnerText = transactionId;

                    XmlElement elementConversationId = header["CONVERSATIONID"];
                    if (elementConversationId == null)
                    {
                        logger.Error("document format is not valid, CONVERSATIONID should exist");
                        return;
                    }
                    string conversationId = elementConversationId.InnerText;

                    if (string.IsNullOrEmpty(conversationId))
                    {
                        conversationId = Guid.NewGuid().ToString();
                        elementConversationId.InnerText = conversationId;
                    }
                    XmlElement elementOriginated = document.DocumentElement["ORIGINATED"];

                    XmlElement elementMachineName = elementOriginated["MACHINENAME"];
                    string machineName = elementMachineName.InnerText;

                    if (elementOriginated == null)
                    {
                        logger.Error("document format is not valid, ORIGINATED should exist");
                        return;
                    }
                    elementOriginated["ORIGINATEDNAME"].InnerText = dest;

                    logger.Info("server message received" + System.Environment.NewLine + XmlUtility.GetLogStringFromXml(document.DocumentElement));

                    //logger.receive(document, transactionId, messageName);
                    //for (IEnumerator iterator = this.reconcileMessageReplyNameList.GetEnumerator(); iterator.MoveNext(); )
                    //{
                    //    string reconcileReplyMessageName = (string)iterator.Current;
                    //    if (messageName.Contains(reconcileReplyMessageName))
                    //    {
                    //        XmlNode dataElement = document.SelectSingleNode("/MESSAGE/DATA");
                    //        if ((dataElement != null) && (dataElement.FirstChild != null))
                    //        {
                    //            string acknowledgeName = ((XmlElement)dataElement.FirstChild).Name;
                    //            string ackValue = ((XmlElement)dataElement.FirstChild).Value;
                    //            if (acknowledgeName.Equals("ONLACK"))
                    //            {
                    //                if ((!ackValue.Equals("0")) && (!ackValue.Equals("2")))
                    //                {
                    //                    //logger.error("Reconcile Reply nak received, " + acknowledgeName + "[" + ackValue + "]", transactionId, messageName, "", "", machineName, "", messageName);
                    //                    break;
                    //                }
                    //            }
                    //            else if ((acknowledgeName.Contains("ACK")) || (acknowledgeName.Equals("EAC")))
                    //            {
                    //                if (!ackValue.Equals("0"))
                    //                {
                    //                    //logger.error("Reconcile Reply nak received, " + acknowledgeName + "[" + ackValue + "]", transactionId, messageName, "", "", machineName, "", messageName);
                    //                    break;
                    //                }
                    //            }
                    //            else if (((XmlElement)((XmlElement)dataElement.FirstChild).FirstChild).Name.Equals("COMMACK"))
                    //            {
                    //                acknowledgeName = ((XmlElement)((XmlElement)dataElement.FirstChild).FirstChild).Name;
                    //                ackValue = ((XmlElement)((XmlElement)dataElement.FirstChild).FirstChild).Value;
                    //                if (!ackValue.Equals("0"))
                    //                {
                    //                    //logger.error("Reconcile Reply nak received, " + acknowledgeName + "[" + ackValue + "]", transactionId, messageName, "", "", machineName, "", messageName);
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    if (!awakeMessage)
                    {
                        //onMessage(transactionId, messageName, document);
                        onMessage(document, transactionId);
                    }
                    else
                    {
                        onAwakeMessage(conversationId, messageName, document);
                    }
                }
                else if (obj is AbstractMessage)
                {
                    AbstractMessage abstractMessage = (AbstractMessage)obj;

                    String transactionId = Guid.NewGuid().ToString();
                    abstractMessage.TransactionId = transactionId;

                    if (string.IsNullOrEmpty(abstractMessage.ConversationId))
                    {
                        abstractMessage.ConversationId = Guid.NewGuid().ToString();
                        awakeMessage = true;
                    }

                    if (string.IsNullOrEmpty(abstractMessage.OriginatedName))
                    {
                        abstractMessage.OriginatedName = dest;
                    }

                    //logger.receive(abstractMessage);
                    if (!awakeMessage)
                    {
                        onMessage(abstractMessage);
                    }
                    else
                    {
                        onAwakeMessage(abstractMessage);
                    }
                }
                else
                {
                    logger.Error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + obj);
                }
            }
            catch (Exception e)
            {
                logger.Error("failed to handle tibrv message", e);
            }
            finally
            {

            }
        }
    }
}
