using ACS.Communication.Msb;
using ACS.Communication.Msb.Highway101;
using ACS.Communication.Msb.Util;
using ACS.Core.Logging;
using ACS.Core.Message.Model;
using ACS.Communication.Msb.Tibrv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public abstract class AbstractTibrvListener : AbstractTibrv, IMsbControllable
    {
        protected internal ChannelDestination destination;
        protected internal bool open;
        protected internal IMsbConverter msbConverter;

        //bsw
        private Listener instance = null;
        //private Dispatcher dispatcher = null;

        private bool executeProgram = true;

        public virtual IMsbConverter MsbConverter
        {
            get
            {
                return this.msbConverter;
            }
            set
            {
                this.msbConverter = value;
            }
        }

        public ChannelDestination Destination
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.destination = value;
            }
        }


        public bool isOpen()
        {
            return this.open;
        }

        public void setOpen(bool open)
        {
            this.open = open;
        }

        public override void Init()
        {
            base.Init();
        }

        public virtual void close()
        {
            Stop();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected void createListener() throws TibrvException
        public virtual void createListener()
        {
            if ((this.destination is QueueDestination))
            {
                ((QueueDestination)this.destination).createTibrvQueue();

                NetTransport tibrvTransport = Transport.TibrvTransport;

                //if (!tibrvTransport.Valid)
                if (tibrvTransport == null)
                {
                    tibrvTransport = Transport.createTransport();
                }
                //new Listener(((QueueDestination)this.destination).TibrvQueue, this, tibrvTransport, ((QueueDestination)this.destination).Name, null);
                //new Listener(((QueueDestination)this.destination).TibrvQueue, tibrvTransport, ((QueueDestination)this.destination).Name, null);

                //new Listener(((QueueDestination)this.destination).TibrvQueue, tibrvTransport, ((QueueDestination)this.destination).Name, null).MessageReceived
                //+= new MessageReceivedEventHandler(onMsg);

                instance = new Listener(((QueueDestination)this.destination).TibrvQueue, onMsg, tibrvTransport, ((QueueDestination)this.destination).Name, null);

                //instance.MessageReceived += new MessageReceivedEventHandler(onMsg);

                logger.Info("succeeded in creating listener, destination{" + ((QueueDestination)this.destination).Name + "}");
            }
            else
            {
                //throw new DestinationTypeMismatchException("destination type should be QueueDestination");
            }
        }

        //public virtual void dispatch()
        //{
        //    QueueGroup queueGroup = new QueueGroup();
        //    queueGroup.Add(((QueueDestination)this.destination).TibrvQueue);
        //    //queueGroup.Add(Tibrv.defaultQueue());

        //    //new Dispatcher(queueGroup);
        //    dispatcher = new Dispatcher(queueGroup);
        //    //dispatcher.Join();

        //    //logger.info("succeeded in dispatching, destination{" + ((QueueDestination)this.destination).Name + "}");
        //}

        public virtual void dispatch()
        {
            QueueGroup queueGroup = new QueueGroup();
            queueGroup.Add(((QueueDestination)this.destination).TibrvQueue);
            //queueGroup.Add(Tibrv.defaultQueue());

            //new Dispatcher(queueGroup);
            //dispatcher = new Dispatcher(queueGroup);

            while(executeProgram)
            {
                try
                {
                    queueGroup.Dispatch();
                }
                catch (RendezvousException rendezvousException)
                {
                    //Console.Error.WriteLine(rendezvousException.StackTrace);
                    logger.Error(rendezvousException.StackTrace);
                }
                finally
                {

                }
            }

            //dispatcher.Join();

            //logger.info("succeeded in dispatching, destination{" + ((QueueDestination)this.destination).Name + "}");
        }


        public virtual void onMsg(object tibrvListener, MessageReceivedEventArgs tibrvMsg)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    //logger.Debug("destination{" + ((QueueDestination)this.destination).Name
                    //    + "}, received message{" + tibrvMsg.ToString() + "}");
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
                            //logger.error("can not convert, messageDataFormat should be [XML_STRING|PLAIN_STRING] if you use msbConverter");
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
                        //logger.error("can not convert, messageDataFormat should be [XML_STRING|PLAIN_STRING|OBJECT]");
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
                        //logger.error("document format is not valid, HEADER should exist");
                        return;
                    }
                    string messageName = header["MESSAGENAME"] != null ? header["MESSAGENAME"].InnerText : "";

                    XmlElement elementTransactionId = header["TRANSACTIONID"];

                    if (elementTransactionId == null)
                    {
                        //logger.error("document format is not valid, TRANSACTIONID should exist");
                        return;
                    }

                    string transactionId = Guid.NewGuid().ToString();
                    elementTransactionId.InnerText = transactionId;

                    XmlElement elementConversationId = header["CONVERSATIONID"];
                    if (elementConversationId == null)
                    {
                        //logger.error("document format is not valid, CONVERSATIONID should exist");
                        return;
                    }
                    string conversationId = elementConversationId.InnerText;

                    if (string.IsNullOrEmpty(conversationId))
                    {
                        conversationId = Guid.NewGuid().ToString();
                        elementConversationId.InnerText = conversationId;
                    }
                    XmlElement elementOriginated = document.DocumentElement["ORIGINATED"];
                    if (elementOriginated == null)
                    {
                        //logger.error("document format is not valid, ORIGINATED should exist");
                        return;
                    }
                    elementOriginated["ORIGINATEDNAME"].InnerText = dest;

                    //logger.receive(document, transactionId, messageName);
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
                }
                else
                {
                    //logger.error("can not execute workflow, message should be Document or subclass of AbstractMessage, " + object);
                }
            }
            catch (Exception e)
            {
                //logger.error("failed to handle tibrv message", e);
            }
            finally
            {

            }
        }

        public abstract void onMessage(XmlDocument paramDocument, String paramString);
        public abstract void onMessage(String paramString1, String paramString2, XmlDocument paramDocument);
        public abstract void onMessage(AbstractMessage paramAbstractMessage);
        public abstract void onAwakeMessage(String paramString1, String paramString2, XmlDocument paramDocument);
        public abstract void onAwakeMessage(AbstractMessage paramAbstractMessage);
        public bool Start()
        {
            bool returnValue = false;

            try
            {
                createListener();

                //dispatch();
                Task.Run(()=> dispatch());

                //logger.fine("succeeded in initializing, " + this);
                this.open = true;
                returnValue = true;
            }
            catch (RendezvousException e)
            {
                //throw new MsbStartException(e);
            }
            finally
            {

            }

            return returnValue;
        }

        public bool Stop()
        {
            //if ((Transport.TibrvTransport != null) && (Transport.TibrvTransport.Valid))
            if ((Transport.TibrvTransport != null))
            {
                Transport.TibrvTransport.Destroy();
                //logger.fine("succeeded in destroying transport, " + this);
            }
            this.open = false;
            //logger.fine("succeeded in stopping, " + this);

            return true;
        }

        public bool Open()
        {
            return this.open;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("tibrvListener{");
            sb.Append("destination=").Append(this.destination);
            sb.Append(", transport=").Append(this.transport);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", useDataField=").Append(this.useDataField);
            sb.Append(", dataFieldName=").Append(this.dataFieldName);
            sb.Append(", open=").Append(this.open);
            sb.Append(", dataFormat=").Append(this.DataFormat).Append("{").Append(GetDataFormatTostring()).Append("}");
            sb.Append(", msbConverter=").Append(this.msbConverter);
            sb.Append("}");

            return sb.ToString();
        }

        public string GetMsbControllerName()
        {
            return "tibrv";
        }
    }
}
