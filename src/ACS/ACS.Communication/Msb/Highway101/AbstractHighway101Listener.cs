using com.miracom.transceiverx;
using com.miracom.transceiverx.message;
using com.miracom.transceiverx.session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Xml.Linq;
using ACS.Communication.Msb.Convert;
using ACS.Communication.Msb;
using ACS.Communication.Msb.Util;
using ACS.Communication.Util;
using ACS.Communication.Msb.Highway101.Message;
using ACS.Framework.Message.Model;
using ACS.Utility;
/// <summary>
/// Listener
/// </summary>
namespace ACS.Communication.Msb.Highway101
{
    public abstract class AbstractHighway101Listener : AbstractHighway101, IMsbControllable, MessageConsumer
    {
        public Session ListenerSession { get; set; }
        public ChannelDestination Destination { get; set; }
        public IMsbConverter MsbConverter { get; set; }
        
        public AbstractHighway101Listener()
        {

        }

        public override void Init()
        {
            base.Init();
        }

        public bool Open()
        {
            return ListenerSession.isStarted();
        }

        public abstract void OnMessage(XmlDocument document, string paramString);

        public abstract void OnMessage(AbstractMessage paramAbstractMessage);

        public bool Start()
        {
            try
            {
                //Stop();
                CreateListener();

                logger.Info((new StringBuilder("succeeded in initializing, ")).Append(this).ToString());
                return true;
            }
            catch (TrxException e)
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
                if((this.PullingThread != null) || (this.PullingThread.IsAlive()))
                {
                    this.PullingThread.Terminate = true;
                }

                this.ListenerSession.disconnect();
            }
            catch(TrxException e)
            {

            }

            this.ListenerSession.destroy();

            return true;
        }

        private Task StopAsync()
        {
            return Task.Factory.StartNew(StopProcess);
        }

        public void StopProcess()
        {
            try
            {
                PullingThread.Terminate = true;

                if (PullingThread.WorkerThread != null && PullingThread.WorkerThread.IsAlive)
                    PullingThread.WorkerThread.Join();

                ListenerSession.disconnect();

                ListenerSession.destroy();
            }
            catch (TrxException e)
            {
                //LogHelper.Instance.Catch("public void StopProcess()" + e.ToString());
                //logger.Error("", e);
                logger.Error("public void StopProcess()" + e.ToString());
            }
            finally
            {

            }
            
            //return true;
        }

        public void CreateListener()
        {
            if (this.Destination is ChannelDestination)
            {
                ListenerSession = CreateSession();
                ListenerSession.addMessageConsumer(this);

                if (CastOption.Equals("UNICAST"))
                    ListenerSession.tuneUnicast(Destination.Name);
                else if (CastOption.Equals("MULTICAST"))
                    ListenerSession.tuneMulticast(Destination.Name);
                else if (CastOption.Equals("GUNICAST"))
                    ListenerSession.tuneGuaranteedUnicast(Destination.Name);
                else
                    ListenerSession.tuneGuaranteedMulticast(Destination.Name);

                logger.Info((new StringBuilder("succeeded in creating listener, Destination{")).Append(((ChannelDestination)Destination).Name).Append("}").ToString());

                if (DeliveryMode.Equals("PULL"))
                    CreatePullingThread(ListenerSession, this);
            }
            else
            {
                throw new Exception();
            }
        }

        public virtual void onUnicast(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
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
                        abstractMessage.OriginatedName = paramMessage.getChannel();
                    }
                    logger.Info("received message : " + abstractMessage.MessageName);
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
        }

        public virtual void onMulticast(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
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
        }

        public virtual void onRequest(Session paramSession, com.miracom.transceiverx.message.Message paramMessage)
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

        }

        public void onGUnicast(Session session, com.miracom.transceiverx.message.Message message)
        {

        }

        public void onGMulticast(Session ss, com.miracom.transceiverx.message.Message msg)
        {
        }

        public void onReply(Session ss, com.miracom.transceiverx.message.Message req, com.miracom.transceiverx.message.Message reply, object hint)
        {
        }

        public void onTimeout(Session ss, com.miracom.transceiverx.message.Message req)
        {
        }

        public string GetMsbControllerName()
        {
            return "highway101";
        }
    }
}
