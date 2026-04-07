using ACS.Communication;
using ACS.Communication.Msb;
using ACS.Core.Base;
using ACS.Core.Material;
using ACS.Core.Material.Model;
using ACS.Core.Message;
using ACS.Core.Message.Model;
using ACS.Core.Message.Model.Control;
using ACS.Core.Message.Model.Server;
using ACS.Core.Message.Model.Ui;
using ACS.Core.Message.Model.Host;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Core.Application.Model;
using ACS.Core.Application;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using ACS.Utility;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.Json;
using System.Configuration;
using System.Diagnostics;
using ACS.Communication.Mqtt.Model;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;



namespace ACS.Manager.Message
{
    public class MessageManagerExImplement : AbstractManager, IMessageManagerEx
    {

        private String messageTemplatePath;
        private XmlDocument templateDocument;
        protected MessageNode messageNode;
        protected IMaterialManagerEx materialManager;
        protected ITransferManagerEx transferManager;
        protected IResourceManagerEx resourceManager;
        //protected SecsInterfaceManager secsInterfaceManager;
        protected NioInterfaceManager nioInterfaceManager;
        protected IApplicationManager applicationManager;
        protected IMessageAgent uiAgent;
        protected IMessageAgent esAgent;
        protected IMessageAgent tsAgent;
        protected IMessageAgent hostAgent;
        //181213 WIFI RF ZIGBEE available
        protected String rfServerName = "";

        public XmlDocument TemplateDocument
        {
            get { return templateDocument; }
            set { templateDocument = value; }
        }

        public MessageNode MessageNode
        {
            get { return messageNode; }
            set { messageNode = value; }
        }
        NioInterfaceManager NioInterfaceManager
        {
            get { return nioInterfaceManager; }
            set { nioInterfaceManager = value; }
        }
        public String MessageTemplatePath
        {
            get { return messageTemplatePath; }
            set { messageTemplatePath = value; }
        }

        public IApplicationManager ApplicationManager
        {
            get { return applicationManager; }
            set { applicationManager = value; }
        }

        public IMaterialManagerEx MaterialManager
        {
            get { return materialManager; }
            set { materialManager = value; }
        }

        public IResourceManagerEx ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        public ITransferManagerEx TransferManager
        {
            get { return transferManager; }
            set { transferManager = value; }
        }

        public IMessageAgent UiAgent
        {
            get { return uiAgent; }
            set { uiAgent = value; }
        }
        public IMessageAgent EsAgent
        {
            get { return esAgent; }
            set { esAgent = value; }
        }


        public IMessageAgent TsAgent
        {
            get { return tsAgent; }
            set { tsAgent = value; }
        }

        public IMessageAgent HostAgent
        {
            get { return hostAgent; }
            set { hostAgent = value; }
        }

        public IConfiguration Configuration { get; set; }

        public String RfServerName
        {
            get { return rfServerName; }
            set { rfServerName = value; }

        }

        public override void Init()
        {
            base.Init();
            if (this.messageTemplatePath == null)
            {
                //logger.warn("message template should be defined first, default message format will be used");
                CreateDefaultMessageFormat();
            }
            else
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                string startUpPath = System.IO.Path.GetDirectoryName(exe);
                this.messageTemplatePath = this.messageTemplatePath.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                string path = startUpPath + @"/" + this.messageTemplatePath;
                templateDocument = new XmlDocument();
                this.templateDocument.Load(path); //= XmlUtils.makeDocument(this.messageTemplatePath);
                if (this.templateDocument == null)
                {
                    //logger.fatal("template document should be created first, please check error messages");
                    return;
                }
            }
        }

        public XmlDocument GetTemplateDocument()
        {
            return templateDocument;
        }

        public MessageNode GetMessageNode()
        {
            return messageNode;
        }

        protected void CreateDefaultMessageFormat()
        {
            XmlDocument document = new XmlDocument();
            XmlElement message = document.CreateElement("MESSAGE");
            document.AppendChild(message);

            XmlElement header = document.CreateElement("HEADER");
            message.AppendChild(header);

            XmlNode messagename = document.CreateNode(XmlNodeType.Element, "MESSAGENAME", "");
            XmlNode transactionid = document.CreateNode(XmlNodeType.Element, "TRANSACTIONID", "");
            XmlNode conversationid = document.CreateNode(XmlNodeType.Element, "CONVERSATIONID", "");
            XmlNode time = document.CreateNode(XmlNodeType.Element, "TIME", "");
            XmlNode sender = document.CreateNode(XmlNodeType.Element, "SENDER", "");
            header.AppendChild(messagename);
            header.AppendChild(transactionid);
            header.AppendChild(conversationid);
            header.AppendChild(time);
            header.AppendChild(sender);

            XmlElement originated = document.CreateElement("ORIGINATED");
            message.AppendChild(originated);

            XmlNode originatedtype = document.CreateNode(XmlNodeType.Element, "ORIGINATEDTYPE", "");
            XmlNode originatedname = document.CreateNode(XmlNodeType.Element, "ORIGINATEDNAME", "");
            XmlNode machinename = document.CreateNode(XmlNodeType.Element, "MACHINENAME", "");
            XmlNode connectionid = document.CreateNode(XmlNodeType.Element, "CONNECTIONID", "");
            XmlNode username = document.CreateNode(XmlNodeType.Element, "USERNAME", "");

            originated.AppendChild(originatedtype);
            originated.AppendChild(originatedname);
            originated.AppendChild(machinename);
            originated.AppendChild(connectionid);
            originated.AppendChild(username);

            XmlElement data = document.CreateElement("DATA");
            message.AppendChild(data);
            XmlElement tail = document.CreateElement("TAIL");
            message.AppendChild(tail);

            this.templateDocument = document;

            //logger.info(XmlUtils.toStringPrettyFormat(this.templateDocument));
        }

        public XmlDocument CreateDocument(String filePath)
        {
            XmlDocument document = new XmlDocument();//XmlUtils.makeDocument(filePath);
            document.Load(filePath);
            if (document == null)
            {
                //logger.error("failed to create document, filePath{" + filePath + "}");
            }
            return document;
        }

        public bool PopulateCarrier(TransferMessageEx transferMessage)
        {
            String carrierName = transferMessage.CarrierId;
            CarrierEx carrier = this.materialManager.GetCarrier(carrierName);

            return PopulateCarrier(transferMessage, carrier);
        }

        public bool PopulateCarrier(TransferMessageEx transferMessage, CarrierEx carrier)
        {
            String carrierName = transferMessage.CarrierId;
            if (carrier == null)
            {
                //logger.error("carrier{" + carrierName + "} does not exist in Repository");
                transferMessage.Cause = "CARRIERNOTFOUND";
                return false;
            }
            //logger.info("carrier{" + carrierName + "} is populated in message");
            transferMessage.Carrier = carrier;

            PopulateCarrierProcessType(transferMessage);
            return true;
        }

        public bool PopulateCarrierProcessType(TransferMessageEx transferMessage)
        {
            bool result = false;

            CarrierEx carrier = transferMessage.Carrier;
            if (carrier == null)
            {
                //logger.error("carrier{" + transferMessage.getCarrierId() + "} does not exist in message, " + transferMessage);
                transferMessage.Cause = "CARRIERNOTFOUND";
                return false;
            }
            //logger.info("carrier{" + carrier.getId() + "} is populated in message");

            return result;
        }

        public String GetCarrierId(TransferMessageEx transferMessage)
        {
            String carrierId = transferMessage.CarrierId;
            if (string.IsNullOrEmpty(carrierId))
            {
                CarrierEx carrier = transferMessage.Carrier;
                if (carrier != null)
                {
                    carrierId = carrier.Id;
                }
                else
                {
                    TransportCommandEx transportCommand = transferMessage.TransportCommand;
                    if (transportCommand != null)
                    {
                        carrierId = transportCommand.CarrierId;
                    }
                    else
                    {
                        //logger.warn("carrier does not exist, " + transferMessage, transferMessage);
                    }
                }
            }
            return carrierId;
        }

        public bool PopulateTransportCommand(TransferMessageEx transferMessage)
        {
            String transportCommandId = transferMessage.TransportCommandId;

            TransportCommandEx transportCommand = this.transferManager.GetTransportCommand(transportCommandId);
            if (transportCommand == null)
            {
                transportCommand = this.transferManager.GetTransportCommand(transportCommandId);
            }
            if (transportCommand == null)
            {
                //logger.info("transportCommand{" + transportCommandId + "} does not exist in Repository");
                transferMessage.Cause = "TRANSPORTCOMMANDNOTFOUND";
                return false;
            }
            return PopulateTransportCommand(transferMessage, transportCommand);
        }

        public bool PopulateTransportCommand(TransferMessageEx transferMessage, TransportCommandEx transportCommand)
        {
            //logger.info("transportCommand{" + transportCommand.getId() + "} is populated in message");
            transferMessage.TransportCommand = transportCommand;
            return true;
        }

        public void PopulateTransportCommandInfo(TransferMessageEx transferMessage)
        {
            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.transferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }
            //logger.fine(transportCommand, transferMessage);
        }

        public bool Validate(String value)
        {
            if (string.IsNullOrEmpty(value))
            {
                //logger.info("value is empty, please check it out");
                return false;
            }
            //logger.info("value{" + value + "} is valid");
            return true;
        }

        public bool ValidateCarrier(TransferMessageEx transferMessage)
        {
            bool result = false;

            result = ValidateCarrier(transferMessage.CarrierId);
            if (!result)
            {
                transferMessage.Cause = "CARRIERNAMEISEMPTY";
            }
            return result;
        }

        public bool ValidateCarrier(String carrierId)
        {
            if (string.IsNullOrEmpty(carrierId))
            {
                //logger.warn("carrier Id is empty, please check it out");
                return false;
            }
            //logger.info("carrier{" + carrierId + "} is valid");
            return true;
        }

        public bool ValidateAndPopulateCarrier(TransferMessageEx transferMessage)
        {
            if (string.IsNullOrEmpty(transferMessage.CarrierId))
            {
                transferMessage.Cause = "CARRIERNAMEISEMPTY";
                //logger.warn("carrier Id is empty, please check it out");
                return false;
            }
            CarrierEx carrier = this.materialManager.GetCarrier(transferMessage.CarrierId);
            if (carrier == null)
            {
                transferMessage.Cause = "CARRIERNOTFOUND";
                //logger.fine("carrier{" + transferMessage.getCarrierId() + "} does not exist in repository", transferMessage);
                return false;
            }
            //logger.fine("carrier{" + carrier.getId() + "} is valid", transferMessage);
            //logger.fine("carrier{" + carrier.getId() + "} is populated in message, " + carrier, transferMessage);
            transferMessage.Carrier = carrier;

            return true;
        }

        public bool ValidateAndPopulateCarrierByTransportCommand(TransferMessageEx transferMessage)
        {
            bool result = false;
            if (transferMessage.TransportCommand == null)
            {
                transferMessage.Cause = "TRANSPORTCOMMANDNOTFOUND";
                //logger.warn("transportCommand is null, please check it out");
                return false;
            }
            //logger.info("carrier{" + transferMessage.getCarrierId() + "} does not exist in the message, trying to find carrier using transportCommand{" + transferMessage.getTransportCommand() + "}");

            transferMessage.CarrierId = transferMessage.TransportCommand.CarrierId;
            return ValidateAndPopulateCarrier(transferMessage);
        }

        public bool ValidateTransportCommand(String transportCommandId)
        {
            if (string.IsNullOrEmpty(transportCommandId))
            {
                //logger.fine("transportCommand id is empty, please check it out");
                return false;
            }
            //logger.info("transportCommand{" + transportCommandId + "} is valid");
            return true;
        }

        public bool ValidateTransportCommand(TransferMessageEx transferMessage)
        {
            bool result = false;

            result = ValidateTransportCommand(transferMessage.TransportCommandId);
            if (!result)
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_IDEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_IDEMPTY.Item2;
                //logger.info("transportCommand does not exist");
            }
            return result;
        }

        public bool ValidateAndPopulateTransportCommand(TransferMessageEx transferMessage)
        {
            string transportCommandId = transferMessage.TransportCommandId;

            if (string.IsNullOrEmpty(transportCommandId))
            {
                //logger.warn("transportCommand id is empty, please check it out");
                return false;
            }
            TransportCommandEx transportCommand = this.transferManager.GetTransportCommand(transportCommandId);
            if (transportCommand == null)
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                return false;
            }
            //logger.info("transportCommand{" + transportCommandId + "} is valid");
            //logger.info("transportCommand{" + transportCommandId + "} is populated in message");
            transferMessage.TransportCommand = transportCommand;
            return true;
        }

        public bool ValidateAndPopulateTransportCommandByVehicle(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            TransportCommandEx transportCommand = this.transferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand != null)
            {
                //logger.info("transportCommand{" + vehicleMessage.getTransportCommandId() + "} is valid");
                //logger.info("transportCommand{" + vehicleMessage.getTransportCommandId() + "} is populated in message");
                vehicleMessage.TransportCommand = transportCommand;
                vehicleMessage.TransportCommandId = transportCommand.JobId;
                vehicleMessage.CarrierId = transportCommand.CarrierId;
                result = true;
            }
            else
            {
                //logger.warn("transportCommand does not exist in repository");
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
            }
            return result;
        }

        //protected void fillupMessageDate(Object aMessage, String aName, String aValue)
        //{
        //  MethodInfo[] methods = aMessage.GetType().GetMethod();
        //  for (int i = 0; i < methods.length; i++) {
        //    if ((StringUtils.contains(methods[i].getName().toUpperCase(), aName)) && 
        //      (methods[i].getReturnType().getName().equals("void"))) {
        //      try
        //      {
        //        methods[i].invoke(aMessage, new Object[] { aValue });
        //      }
        //      catch (IllegalArgumentException e)
        //      {
        //        e.printStackTrace();
        //      }
        //      catch (IllegalAccessException e)
        //      {
        //        e.printStackTrace();
        //      }
        //      catch (InvocationTargetException e)
        //      {
        //        e.printStackTrace();
        //      }
        //    }
        //  }
        //}

        public bool ValidateMachineName(String machineName)
        {
            if (string.IsNullOrEmpty(machineName))
            {
                //logger.warn("machineName is empty, please check it out");
                return false;
            }
            //logger.info("machine{" + machineName + "} is valid");
            return true;
        }

        public TransferMessageEx CreateTransferMessage(XmlDocument document)
        {
            //V2 TRSJOBREQ 대응 
            TransferMessageEx transferMessage = new TransferMessageEx();
            transferMessage.ReceivedMessage = document;

            try
            {
                string messageName = XmlUtility.GetDataFromXml(document, "//Command");
                string commandType = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/CmdType");
                string finalEqp = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/FinalEQP");
                string finalPort = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/FinalPort");
                // ACS ID
                string sourceEqp = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/SourceEQP");
                // Vehicle ID
                string sourcePort = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/SourcePort");
                string transportCommandId = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/JobID");
                string carrID = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/CarrID");
                string batchID = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/BatchID");
                string priority = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/Priority");
                string processID = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/ProcessID");
                string productID = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/ProductID");
                //181020
                string stepID = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/StepID");
                // ACS ID
                string transEQP = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/TransEQP");
                string userID = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/UserID");
                //190510 KSB Add Description (Reason->DB)
                string description = XmlUtility.GetDataFromXml(document, "/Msg/DataLayer/Description");

                transferMessage.TransportCommandId = transportCommandId.Trim();
                transferMessage.MessageName = messageName.Trim();

                //181009 한글날 KSG 
                //carrier ID, batch ID MESSAGE SPEC. 추가
                if (string.IsNullOrEmpty(carrID))
                {                 
                    transferMessage.CarrierId = transportCommandId.Trim();                    
                }
                else
                {
                    transferMessage.CarrierId = carrID;
                }

                transferMessage.BatchID = batchID;
                transferMessage.CarrierType = "TRAY";
                transferMessage.Description = userID.Trim();
                transferMessage.DestMachine = finalEqp.Trim();
                transferMessage.DestUnit = finalPort.Trim();

                //181002
                int priorityNumber = 1;

                if (int.TryParse(priority.Trim(), out priorityNumber))
                {
                    transferMessage.Priority = priorityNumber;
                }
                else
                {
                    transferMessage.Priority = 1;
                }

                //not use
                //priority value of host message is '-'
                //transferMessage.Priority = Convert.ToInt32(priority.Trim());

                transferMessage.SourceMachine = sourceEqp.Trim();
                transferMessage.SourceUnit = sourcePort.Trim();
                transferMessage.UserId = userID;
                transferMessage.EqpId = transEQP.Trim();

                //181001
                transferMessage.ProcessId = processID.Trim();
                transferMessage.ProductId = productID.Trim();
                //181020
                transferMessage.StepId = stepID.Trim();

                if (messageName.Trim().ToUpper().Equals("TRSJOBREQ"))
                {
                    switch (commandType)
                    {
                        case "MOVECMD":
                            transferMessage.MessageName = "MOVECMD";
                            transferMessage.VehicleId = sourcePort.Trim();
                            break;
                        case "MOVECANCEL":
                            transferMessage.MessageName = "MOVECANCEL";
                            transferMessage.ReplyCode = AGVJobReport.JOBCANCEL_ERRCODE_HOSTCANCEL;
                            transferMessage.Cause = "ADS_CANCEL";
                            //190510 KSB
                            transferMessage.Description = description;
                            break;
                        case "MOVEUPDATE":
                            {
                                // 2018.10.05 KSG
                                // ADS MESSAGE에 SourcePort item으로부터 update target vehicle id를 입력 받는다.
                                transferMessage.MessageName = "MOVEUPDATE";
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {

                logger.Error("CreateTransferMessage(XmlDocuemnt) Error", e);
            }

            return transferMessage;
        }

        public TransferMessageEx CreateTransferMessage(UiTransportMessageEx uiMessage)
        {
            TransferMessageEx transferMessage = new TransferMessageEx();
            transferMessage.ReceivedMessage = uiMessage;

            String current = DateTime.Now.ToString("yyyyMMddHHmmss");

            String eqpId = "";
            string randomnumeric = "";
            Random random = new Random();
            for (int i = 0; i < 4; i++)
            {
                string ran = random.Next(0, 9).ToString();
                randomnumeric += ran;
            }



            String[] destPort = uiMessage.DestPortId.Split(':');
            String[] sourPort = uiMessage.SourcePortId.Split(':');

            String destMachine = destPort[0];
            String destUnit = destPort[1];
            String sourceMachine = sourPort[0];
            String sourceUnit = sourPort[1];


            String transportCommandId = sourceMachine + "-" + destMachine + "_" + current + "_" + randomnumeric + "_M";
            String carrierId = transportCommandId;



            transferMessage.TransportCommandId = transportCommandId.Trim();
            transferMessage.CarrierId = carrierId.Trim();
            transferMessage.CarrierType = "TRAY";
            transferMessage.SourceMachine = sourceMachine.Trim();
            transferMessage.SourceUnit = sourceUnit.Trim();
            transferMessage.DestMachine = destMachine.Trim();
            transferMessage.DestUnit = destUnit.Trim();
            transferMessage.MessageName = uiMessage.MessageName;

            return transferMessage;
        }

        public TransferMessageEx CreateTransferMessage(UiTransportCancelMessageEx uiMessage)
        {
            TransferMessageEx transferMessage = new TransferMessageEx();
            transferMessage.ReceivedMessage = uiMessage;

            String eqpId = "";
            String transportCommandId = "";
            if (string.IsNullOrEmpty(uiMessage.TransportCommandId))
            {
                transportCommandId = "U" + uiMessage.RequestId;
            }
            else
            {
                transportCommandId = uiMessage.TransportCommandId;
            }
            String carrierId = transportCommandId;

            transferMessage.TransportCommandId = transportCommandId.Trim();
            transferMessage.CarrierId = carrierId.Trim();
            transferMessage.CarrierType = "TRAY";
            transferMessage.Cause = "UICANCEL";
            transferMessage.MessageName = uiMessage.MessageName;

            return transferMessage;
        }

        public TransferMessageEx CreateTransferMessage(UiTransportDeleteMessageEx uiMessage)
        {
            TransferMessageEx transferMessage = new TransferMessageEx();

            transferMessage.ReceivedMessage = uiMessage;

            String eqpId = "";
            String transportCommandId = "U" + uiMessage.RequestId;

            if (string.IsNullOrEmpty(uiMessage.TransportCommandId))
            {
                transportCommandId = "U" + uiMessage.RequestId;
            }
            else
            {
                transportCommandId = uiMessage.TransportCommandId;
            }
            String carrierId = "";

            TransportCommandEx transportCommand = TransferManager.GetTransportCommand(uiMessage.TransportCommandId);

            if (transportCommand != null)
            {
                transferMessage.TransportCommand = transportCommand;
                carrierId = transportCommand.CarrierId;
            }

            //string[] source = transportCommand.Source.Split(':');
            //string[] dest = transportCommand.Source.Split(':');                     

            transferMessage.TransportCommandId = transportCommandId.Trim();

            //if(source == null || dest == null || source.Length < 2 || dest.Length < 2)
            //{
            //    return null;
            //}

            //transferMessage.SourceMachine = source[0];
            //transferMessage.SourceUnit = source[1];
            //transferMessage.DestMachine = dest[0];
            //transferMessage.DestUnit = dest[1];
            transferMessage.CarrierId = carrierId.Trim();
            transferMessage.CarrierType = "TRAY";
            transferMessage.ReplyCode = uiMessage.ErrorCode;
            transferMessage.Cause = uiMessage.ErrorMsg;
            transferMessage.MessageName = uiMessage.MessageName;

            return transferMessage;
        }

        public XmlDocument CreateDocument()
        {
            return (XmlDocument)this.templateDocument.Clone();
        }

        public void SetDefaultHeaderInfoToDocument(String messageName, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["HEADER"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "MESSAGENAME")) node.InnerText = messageName;
                if (string.Equals(node.Name, "TIME")) node.InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff");
            }
        }

        public void SetDefaultHeaderInfoToDocument(XmlDocument document)
        {
            XmlElement header = document.DocumentElement["HEADER"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "TIME")) node.InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff");
            }
        }

        public void SetDefaultOriginatedInfoToDocument(String machineName, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) node.InnerText = "N";
                if (string.Equals(node.Name, "MACHINENAME")) node.InnerText = machineName;
                if (string.Equals(node.Name, "USERNAME")) node.InnerText = "NANOTRANS";
            }

        }

        public void SetDefaultOriginatedInfoToDocument(String machineName, String userName, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) node.InnerText = "U";
                if (string.Equals(node.Name, "MACHINENAME")) node.InnerText = machineName;
                if (string.Equals(node.Name, "USERNAME")) node.InnerText = userName;
            }

        }

        public void SetDefaultOriginatedInfoToDocument(XmlDocument document)
        {
            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) node.InnerText = "U";

                if (string.Equals(node.Name, "USERNAME")) node.InnerText = "NANOTRANS";
            }
        }

        public void SetHeaderInfoToDocument(AbstractMessage abstractMessage, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["HEADER"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "MESSAGENAME")) node.InnerText = abstractMessage.MessageName;
                if (string.Equals(node.Name, "TRANSACTIONID")) node.InnerText = abstractMessage.TransactionId;
                if (string.Equals(node.Name, "CONVERSATIONID")) node.InnerText = abstractMessage.ConversationId;
                if (string.Equals(node.Name, "TIME")) node.InnerText = abstractMessage.Time;
                if (string.Equals(node.Name, "SENDER")) node.InnerText = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE]; //수정필요
                // header.getChild("SENDER").setText(System.getProperty(Executor.SYSTEM_PROPERTY_KEY_ID_VALUE));
            }
        }

        public void SetHeaderInfoToMessage(XmlDocument document, AbstractMessage abstractMessage)
        {
            String messageName = "";
            String transactionId = "";
            String conversationId = "";
            String time = "";

            XmlElement header = document.DocumentElement["HEADER"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "MESSAGENAME")) messageName = node.InnerText;
                if (string.Equals(node.Name, "TRANSACTIONID")) transactionId = node.InnerText;
                if (string.Equals(node.Name, "CONVERSATIONID")) conversationId = node.InnerText;
                if (string.Equals(node.Name, "TIME")) time = node.InnerText;
            }

            abstractMessage.MessageName = messageName;
            abstractMessage.TransactionId = transactionId;
            abstractMessage.ConversationId = conversationId;
            abstractMessage.Time = time;
        }

        public void SetOriginatedInfoToDocument(AbstractMessage abstractMessage, XmlDocument document)
        {
            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) node.InnerText = abstractMessage.OriginatedType;
                if (string.Equals(node.Name, "ORIGINATEDNAME")) node.InnerText = abstractMessage.OriginatedName;
                if (string.Equals(node.Name, "CONNECTIONID")) node.InnerText = abstractMessage.ConnectionId;
                if (string.Equals(node.Name, "MACHINENAME")) node.InnerText = abstractMessage.CurrentMachineName;
                if (string.Equals(node.Name, "USERNAME")) node.InnerText = abstractMessage.UserName;

            }
        }

        public void SetOriginatedInfoToMessage(AbstractMessage fromMessage, AbstractMessage toMessage)
        {
            toMessage.OriginatedType = fromMessage.OriginatedType;
            toMessage.OriginatedName = fromMessage.OriginatedName;
            toMessage.ConnectionId = fromMessage.ConnectionId;
            toMessage.CurrentMachineName = fromMessage.CurrentMachineName;
            toMessage.OriginatedMachineName = fromMessage.CurrentMachineName;
            toMessage.UserName = fromMessage.UserName;
        }

        public void SetHeaderInfoToMessage(AbstractMessage fromMessage, AbstractMessage toMessage)
        {
            toMessage.MessageName = fromMessage.MessageName;
            toMessage.TransactionId = fromMessage.TransactionId;
            toMessage.ConversationId = fromMessage.ConversationId;
            toMessage.Time = fromMessage.Time;
        }

        public void SetOriginatedInfoToMessage(XmlDocument document, AbstractMessage abstractMessage)
        {
            String originatedType = "";
            String originatedName = "";
            String connectionId = "";
            String machineName = "";
            String userName = "";

            XmlElement header = document.DocumentElement["ORIGINATED"];
            foreach (XmlNode node in header.ChildNodes)
            {
                if (string.Equals(node.Name, "ORIGINATEDTYPE")) originatedType = node.InnerText;
                if (string.Equals(node.Name, "ORIGINATEDNAME")) originatedName = node.InnerText;
                if (string.Equals(node.Name, "CONNECTIONID")) connectionId = node.InnerText;
                if (string.Equals(node.Name, "MACHINENAME")) machineName = node.InnerText;
                if (string.Equals(node.Name, "USERNAME")) userName = node.InnerText;
            }

            abstractMessage.OriginatedType = originatedType;
            abstractMessage.OriginatedName = originatedName;
            abstractMessage.ConnectionId = connectionId;
            abstractMessage.CurrentMachineName = machineName;
            abstractMessage.OriginatedMachineName = abstractMessage.CurrentMachineName;
            abstractMessage.UserName = userName;
        }

        public VehicleMessageEx CreateVehicleMessage(String message)
        {
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();
            vehicleMessage.ReceivedMessage = message;

            String vehicleId = "";

            vehicleMessage.VehicleId = vehicleId;
            return vehicleMessage;
        }

        public VehicleMessageEx CreateVehicleMessageFromES(XmlDocument document)
        {
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            vehicleMessage.ReceivedMessage = document;

            SetOriginatedInfoToMessage(document, vehicleMessage);


            String messageName = XmlUtility.GetDataFromXml(document, "//MESSAGENAME");
            vehicleMessage.MessageName = messageName;

            String nodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
            String stationId = "";
            String vehicleId = document.SelectSingleNode(this.messageNode.XpathNameOfVehicleId).InnerText;

            VehicleEx vehicle = this.resourceManager.GetVehicle(vehicleId);
            if (vehicle != null)
            {
                vehicleMessage.Vehicle = vehicle;
            }
            TransportCommandEx transportCommand = this.transferManager.GetTransportCommandByVehicleId(vehicleId);
            if (transportCommand != null)
            {
                vehicleMessage.TransportCommand = transportCommand;
                vehicleMessage.CarrierId = transportCommand.CarrierId;
            }
            if ((messageName.Equals("RAIL-VEHICLELOCATIONCHANGED")) || (messageName.Equals("RAIL-VEHICLELGOCOMMANDREPLY")))
            {
                if (messageName.Equals("RAIL-VEHICLELOCATIONCHANGED"))
                {
                    messageName = "RAIL-VEHICLELOCATIONCHANGED";
                }
                else
                {
                    messageName = "RAIL-VEHICLELGOCOMMANDREPLY";
                }
                nodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
            }
            else if (messageName.Equals("RAIL-VEHICLEINFOREPORT"))
            {
                messageName = "RAIL-VEHICLEINFOREPORT";
                stationId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/STATIONID");

                String runState = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RUNSTATE");
                if (runState.Equals("1"))
                {
                    runState = VehicleMessageEx.RUNSTATE_RUN;
                }
                else if (runState.Equals("0"))
                {
                    runState = VehicleMessageEx.RUNSTATE_STOP;
                }
                String fullState = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/FULLSTATE");
                if (fullState.Equals("1"))
                {
                    fullState = VehicleMessageEx.FULLSTATE_FULL;
                }
                else if (fullState.Equals("0"))
                {
                    fullState = VehicleMessageEx.FULLSTATE_EMPTY;
                }
                vehicleMessage.StationId = stationId;
                vehicleMessage.RunState = runState;
                vehicleMessage.FullState = fullState;
            }
            else if (messageName.Equals("RAIL-ALARMREPORT"))
            {
                messageName = "RAIL-ALARMREPORT";

                String errorId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/ERRORCODE");
                String errorCode = "";
                String errorLevel = "";
                String errorText = "";

                vehicleMessage.ErrorId = errorId;
                vehicleMessage.ErrorCode = errorCode;
                vehicleMessage.ErrorLevel = errorLevel;
                vehicleMessage.ErrorText = errorText;
            }
            else if (messageName.Equals("RAIL-CARRIERTRANSFERREPLY"))
            {
                messageName = "RAIL-CARRIERTRANSFERREPLY";
                String commandId = "";
                String destPortId = "";
                if (vehicleMessage.TransportCommand != null)
                {
                    commandId = vehicleMessage.TransportCommand.JobId;
                    destPortId = vehicleMessage.TransportCommand.Dest;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                }
                String priority = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/PRIORITY");
                String carrierType = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CARRIERTYPE");
                String resultCode = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RESULTCODE");
                nodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");

                vehicleMessage.TransportCommandId = commandId;
                vehicleMessage.DestPortId = destPortId;
                vehicleMessage.DestNodeId = nodeId;
                vehicleMessage.Priority = priority;
                vehicleMessage.CarrierType = carrierType;
                vehicleMessage.ResultCode = resultCode;
            }
            else if (messageName.Equals("RAIL-BATTERYVOLTAGEREPLY"))
            {
                messageName = "RAIL-BATTERYVOLTAGEREPLY";

                String voltage = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VOLTAGE");

                float batteryVoltage;
                float.TryParse(voltage, out batteryVoltage);
                vehicleMessage.BatteryVoltage = batteryVoltage;
            }
            else if (messageName.Equals("RAIL-BATTERYCAPACITYREPLY"))
            {
                messageName = "RAIL-BATTERYCAPACITYREPLY";

                String capa = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CAPACITY");

                int batteryRate = Convert.ToInt32(capa);
                vehicleMessage.BatteryRate = batteryRate;
            }
            else if (messageName.Equals("RAIL-VEHICLEACQUIRECOMPLETED"))
            {
                messageName = "RAIL-VEHICLEACQUIRECOMPLETED";
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if (messageName.Equals("RAIL-VEHICLEDEPOSITCOMPLETED"))
            {
                messageName = "RAIL-VEHICLEDEPOSITCOMPLETED";
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if (messageName.Equals("RAIL-VEHICLEOCODESEPERATOR"))
            {
                messageName = "RAIL-VEHICLEOCODESEPERATOR";
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if ((messageName.Equals("RAIL-VEHICLECHARGESTART")) || (messageName.Equals("RAIL-VEHICLECHARGECOMPLETED")))
            {
                if (messageName.Equals("RAIL-VEHICLECHARGESTART"))
                {
                    messageName = "RAIL-VEHICLECHARGESTART";
                }
                else
                {
                    messageName = "RAIL-VEHICLECHARGECOMPLETED";
                }
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if ((messageName.Equals("RAIL-DESTOCCUPIED")) || (messageName.Equals("RAIL-SOURCEEMPTY")) || (messageName.Equals("RAIL-VEHICLEEMPTY")) || (messageName.Equals("RAIL-VEHICLEOCCUPIED")))
            {
                if (messageName.Equals("RAIL-DESTOCCUPIED"))
                {
                    messageName = "RAIL-DESTOCCUPIED";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_DESTOCCUPIED;
                    vehicleMessage.Cause = "DESTOCCUPIED";
                }
                else if (messageName.Equals("RAIL-SOURCEEMPTY"))
                {
                    messageName = "RAIL-SOURCEEMPTY";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_SOURCEEMPTY;
                    vehicleMessage.Cause = "SOURCEEMPTY";
                }
                else if (messageName.Equals("RAIL-VEHICLEEMPTY"))
                {
                    messageName = "RAIL-VEHICLEEMPTY";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_VEHICLEEMPTY;
                    vehicleMessage.Cause = "VEHICLEEMPTY";
                }
                else if (messageName.Equals("RAIL-VEHICLEOCCUPIED"))
                {
                    messageName = "RAIL-VEHICLEOCCUPIED";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_VEHICLEOCCUPIED;
                    vehicleMessage.Cause = "VEHICLEOCCUPIED";
                }
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if ((messageName.Equals("RAIL-DESTPIOCONNECTERROR")) || (messageName.Equals("RAIL-DESTPIOREQUESTERROR")) || (messageName.Equals("RAIL-DESTPIORUNERROR")) || (messageName.Equals("RAIL-DESTPIOPORTCHECKERROR")))
            {
                if (messageName.Equals("RAIL-DESTPIOCONNECTERROR"))
                {
                    messageName = "RAIL-DESTPIOCONNECTERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_DESTPIOCONERROR;
                    vehicleMessage.Cause = "DESTPIOCONNECTERROR";
                }
                else if (messageName.Equals("RAIL-DESTPIOREQUESTERROR"))
                {
                    messageName = "RAIL-DESTPIOREQUESTERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_DESTPIOREQERROR;
                    vehicleMessage.Cause = "DESTPIOREQUESTERROR";
                }
                else if (messageName.Equals("RAIL-DESTPIORUNERROR"))
                {
                    messageName = "RAIL-DESTPIORUNERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_DESTPIORUNERROR;
                    vehicleMessage.Cause = "DESTPIORUNERROR";
                }
                else if (messageName.Equals("RAIL-DESTPIOPORTCHECKERROR"))
                {
                    messageName = "RAIL-DESTPIOPORTCHECKERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_DESTPIOPORTCHECKERROR;
                    vehicleMessage.Cause = "DESTPIOPORTCHECKERROR";
                }
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if ((messageName.Equals("RAIL-SOURCEPIOCONNECTERROR")) || (messageName.Equals("RAIL-SOURCEPIOREQUESTERROR")) || (messageName.Equals("RAIL-SOURCEPIORUNERROR")) || (messageName.Equals("RAIL-SOURCEPIOPORTCHECKERROR")))
            {
                if (messageName.Equals("RAIL-SOURCEPIOCONNECTERROR"))
                {
                    messageName = "RAIL-SOURCEPIOCONNECTERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_SOURCEPIOCONERROR;
                    vehicleMessage.Cause = "SOURCEPIOCONNECTERROR";
                }
                else if (messageName.Equals("RAIL-SOURCEPIOREQUESTERROR"))
                {
                    messageName = "RAIL-SOURCEPIOREQUESTERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_SOURCEPIOREQERROR;
                    vehicleMessage.Cause = "SOURCEPIOREQUESTERROR";
                }
                else if (messageName.Equals("RAIL-SOURCEPIORUNERROR"))
                {
                    messageName = "RAIL-SOURCEPIORUNERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_SOURCEPIORUNERROR;
                    vehicleMessage.Cause = "SOURCEPIORUNERROR";
                }
                else if (messageName.Equals("RAIL-SOURCEPIOPORTCHECKERROR"))
                {
                    messageName = "RAIL-SOURCEPIOPORTCHECKERROR";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_SOURCEPIOPORTCHECKERROR;
                    vehicleMessage.Cause = "SOURCEPIOPORTCHECKERROR";
                }
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if ((messageName.Equals("RAIL-CARRIERREMOVED")) || (messageName.Equals("RAIL-CARRIERLOADED")))
            {
                if (messageName.Equals("RAIL-CARRIERREMOVED"))
                {
                    messageName = "RAIL-CARRIERREMOVED";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_CARRIERREMOVED;
                    vehicleMessage.Cause = "CARRIERREMOVED";
                }
                else if (messageName.Equals("RAIL-CARRIERLOADED"))
                {
                    messageName = "RAIL-CARRIERLOADED";
                    vehicleMessage.ResultCode = AGVJobReport.JOBCANCEL_ERRCODE_CARRIERLOADED;
                    vehicleMessage.Cause = "CARRIERLOADED";
                }
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else if (messageName.Equals("RAIL-INFORM"))
            {
                String text = document.SelectSingleNode("/MESSAGE/DATA/TEXT").InnerText;
                vehicleMessage.Cause = text;
            }
            else if ((messageName.Equals("RAIL-VEHICLEMAINBOARDVERSION")) || (messageName.Equals("RAIL-VEHICLEPLCVERSION")))
            {
                if (messageName.Equals("RAIL-VEHICLEMAINBOARDVERSION"))
                {
                    messageName = "RAIL-VEHICLEMAINBOARDVERSION";
                }
                else if (messageName.Equals("RAIL-VEHICLEPLCVERSION"))
                {
                    messageName = "RAIL-VEHICLEPLCVERSION";
                }
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/VERSION").InnerText;
            }
            //KSB 추가 - RGV 사용시 Alarm 해제하기 위하여 RGV Start (M03) 이번트 처리
            else if (messageName.Equals("RAIL-VEHICLESTART") && vehicleMessage.Vehicle.Vendor.Equals("RGV"))
            {
                messageName = "RAIL-VEHICLESTART";
                vehicleMessage.Cause = "RGVSTART_M03";
                nodeId = document.SelectSingleNode("/MESSAGE/DATA/CURRENTNODEID").InnerText;
            }
            else
            {
                vehicleMessage.ResultCode = "0";
                vehicleMessage.Cause = "-";
            }

            vehicleMessage.MessageName = messageName;
            vehicleMessage.VehicleId = vehicleId;
            vehicleMessage.NodeId = nodeId;
            vehicleMessage.KeyData = document.OuterXml.ToString();

            return vehicleMessage;
        }

        public virtual VehicleMessageEx CreateVehicleMessageFromTrans(XmlDocument document)
        {
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            vehicleMessage.ReceivedMessage = document;

            SetOriginatedInfoToMessage(document, vehicleMessage);

            String messageName = XmlUtility.GetDataFromXml(document, "//MESSAGENAME");
            vehicleMessage.MessageName = messageName;

            String nodeId = "";
            String stationId = "";
            String vehicleId = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfVehicleId);
            if (messageName.Equals("RAIL-CARRIERTRANSFER"))
            {
                messageName = "C_CODE";

                String commandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/COMMANDID");
                String destPortId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTPORTID");
                String priority = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/PRIORITY");
                String carrierType = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CARRIERTYPE");
                String resultCode = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RESULTCODE");
                nodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");



                vehicleMessage.CommandId = commandId;
                vehicleMessage.DestPortId = destPortId;
                vehicleMessage.Priority = priority;
                vehicleMessage.CarrierType = carrierType;
                vehicleMessage.ResultCode = resultCode;
                vehicleMessage.NodeId = nodeId;
            }
            else if (messageName.Equals("RAIL-VEHICLELGOCOMMAND"))
            {
                messageName = "T_CODE";
                nodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
            }
            else if (!messageName.Equals("RAIL-BATTERYVOLTAGEREQUEST"))
            {
                messageName.Equals("RAIL-BATTERYCAPACITYREQUEST");
            }
            vehicleMessage.MessageName = messageName;
            vehicleMessage.VehicleId = vehicleId;
            vehicleMessage.NodeId = nodeId;
            vehicleMessage.KeyData = document.OuterXml.ToString();

            return vehicleMessage;
        }

        public VehicleMessageEx CreateVehicleMessageFromDaemon(XmlDocument document)
        {
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            vehicleMessage.ReceivedMessage = document;

            SetOriginatedInfoToMessage(document, vehicleMessage);

            String messageName = XmlUtility.GetDataFromXml(document, "//MESSAGENAME");
            vehicleMessage.MessageName = messageName;

            String nodeId = "";
            String stationId = "";
            String vehicleId = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfVehicleId);
            if (messageName.Equals("SCHEDULE-CHARGEJOB"))
            {
                messageName = "SCHEDULE-CHARGEJOB";

                String bayId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/BAYID");
                String resultCode = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RESULTCODE");

                vehicleMessage.BayId = bayId;

                vehicleMessage.ResultCode = resultCode;
            }
            else if (messageName.Equals("SCHEDULE-QUEUEJOB"))
            {
                messageName = "SCHEDULE-QUEUEJOB";

                String bayId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/BAYID");
                String resultCode = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RESULTCODE");
                IList listVehicles = this.resourceManager.GetVehiclesByBayId(bayId);

                vehicleMessage.BayId = bayId;
                vehicleMessage.Vehicles = listVehicles;
                vehicleMessage.ResultCode = resultCode;
            }
            else if (messageName.Equals("SCHEDULE-CHECKVEHICLES"))
            {
                messageName = "SCHEDULE-CHECKVEHICLES";

                String resultCode = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RESULTCODE");
                IList listVehicles = this.resourceManager.GetVehicles();

                vehicleMessage.Vehicles = listVehicles;
                vehicleMessage.ResultCode = resultCode;
            }
            else if (messageName.Equals("SCHEDULE-CALLIDLEVEHICLE"))
            {
                messageName = "SCHEDULE-CALLIDLEVEHICLE";
                String bayId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/BAYID");
                String resultCode = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/RESULTCODE");
                IList listVehicles = this.resourceManager.GetVehiclesByBayId(bayId);
                vehicleMessage.BayId = bayId;
                vehicleMessage.Vehicles = listVehicles;
                vehicleMessage.ResultCode = resultCode;
            }
            vehicleMessage.MessageName = messageName;
            vehicleMessage.VehicleId = vehicleId;
            vehicleMessage.KeyData = document.OuterXml.ToString();

            return vehicleMessage;
        }

        public VehicleMessageEx CreateVehicleMessageFromDaemon(string jsonMessage)
        {
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            var msg = JsonSerializer.Deserialize<DaemonScheduleMessage>(jsonMessage);
            string messageName = msg.Header?.MessageName ?? "";
            string bayId = msg.Data?.BayId ?? "";

            vehicleMessage.MessageName = messageName;
            vehicleMessage.KeyData = jsonMessage;

            if (messageName.Equals("SCHEDULE-CHARGEJOB"))
            {
                vehicleMessage.BayId = bayId;
            }
            else if (messageName.Equals("SCHEDULE-QUEUEJOB"))
            {
                IList listVehicles = this.resourceManager.GetVehiclesByBayId(bayId);
                vehicleMessage.BayId = bayId;
                vehicleMessage.Vehicles = listVehicles;
            }
            else if (messageName.Equals("SCHEDULE-CHECKVEHICLES"))
            {
                IList listVehicles = this.resourceManager.GetVehicles();
                vehicleMessage.Vehicles = listVehicles;
            }
            else if (messageName.Equals("SCHEDULE-CALLIDLEVEHICLE"))
            {
                IList listVehicles = this.resourceManager.GetVehiclesByBayId(bayId);
                vehicleMessage.BayId = bayId;
                vehicleMessage.Vehicles = listVehicles;
            }

            return vehicleMessage;
        }

        public AlarmMessage CreateAlarmMessage(XmlDocument document)
        {
            AlarmMessage alarmMessage = new AlarmMessage();
            alarmMessage.ReceivedMessage = document;

            SetOriginatedInfoToMessage(document, alarmMessage);

            String messageName = XmlUtility.GetDataFromXml(document, "//Command");
            String commandId = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfCommandId);
            String errorId = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfErrorId);
            String errorNumber = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfErrorNumber);
            String unitId = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfUnitId);
            String unitState = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfUnitState);
            String recoveryOptions = XmlUtility.GetDataFromXml(document, this.messageNode.XpathNameOfRecoveryOptions);

            alarmMessage.MessageName = messageName;
            alarmMessage.TransportCommandId = commandId;
            alarmMessage.ErrorId = errorId;
            alarmMessage.ErrorNumber = errorNumber;
            alarmMessage.CurrentUnitName = unitId;
            alarmMessage.UnitState = unitState;
            alarmMessage.RecoveryOptions = recoveryOptions;

            return alarmMessage;
        }

        public bool PopulateVehicle(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            VehicleEx vehicle = this.resourceManager.GetVehicle(vehicleMessage.VehicleId);
            if (vehicle != null)
            {
                vehicleMessage.Vehicle = vehicle;
                result = true;
            }
            else
            {
                //logger.error("currentVehicle{" + vehicleMessage.getVehicleId() + "} does not exist in repository");
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_RESULT_CURRENTUNIT_NOTFOUND.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_RESULT_CURRENTUNIT_NOTFOUND.Item2;
            }
            return result;
        }

        public bool ValidateVehicle(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            String vehicleId = vehicleMessage.VehicleId;
            if (string.IsNullOrEmpty(vehicleId))
            {
                result = true;
            }
            else
            {
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_CURRENTUNIT_NAMEEMPTY.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_CURRENTUNIT_NAMEEMPTY.Item2;
                //logger.warn("vehicleId is empty, please check it out");
            }
            return result;
        }

        public VehicleMessageEx CreateVehicleMessage(IPacket receivePacket) //nio이후에 다시 
        {
#if BYTE12
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            vehicleMessage.ReceivedMessage = receivePacket;
            vehicleMessage.VehicleId = receivePacket.SendId;

            String nodeId = "";
            String stationId = "";
            String messageName = "";
            String vehicleId = receivePacket.SendId;
            String commandCode = receivePacket.Command;
            if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_C))                //Command Reply
            {
                messageName = "C_CODE_REP";
                stationId = "1" + receivePacket.Data.Substring(0, 2);
                vehicleMessage.StationId = stationId;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_T))           //Tag Information
            {
                messageName = "T_CODE";
                nodeId = receivePacket.Data.Substring(receivePacket.Data.Length - 3);
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_S))           //AGV State
            {
                messageName = "S_CODE";
                stationId = "1" + receivePacket.Data.Substring(0,2);
                String runState = receivePacket.Data.Substring(2, 1);
                String fullState = receivePacket.Data.Substring(receivePacket.Data.Length-1);

                vehicleMessage.RunState = runState;
                vehicleMessage.FullState = fullState;
                vehicleMessage.StationId = stationId;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_E))           //Error Code
            {
                messageName = "E_CODE";
                String errorCode = receivePacket.Data.Substring(1, 3);
                vehicleMessage.ErrorCode = errorCode;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_R))           //Return Value
            {
                messageName = "R_CODE";
                String rType = receivePacket.Data.Substring(0, 1);
                vehicleMessage.RCodeType = rType;
                if (rType.Equals("1", StringComparison.OrdinalIgnoreCase))          //AGV 전압
                {
                    String voltage = receivePacket.Data.Substring(1, 2) + "." + receivePacket.Data.Substring(3, 1);

                    float batteryVoltage;
                    float.TryParse(voltage, out batteryVoltage);
                    vehicleMessage.BatteryVoltage = batteryVoltage;
                }
                else if (rType.Equals("2", StringComparison.OrdinalIgnoreCase))     //AGV 용량
                {
                    String capa = receivePacket.Data.Substring(2, 2);
                    //181004
                    int batteryRate = 0;
                    int.TryParse(capa, out batteryRate);
                    //int batteryRate = Convert.ToInt32(capa);
                    vehicleMessage.BatteryRate = batteryRate;
                }
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_L))           //Loading
            {
                messageName = "L_CODE";
                nodeId = receivePacket.Data.Substring(0, 4);
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_U))           //Unloading
            {
                messageName = "U_CODE";
                nodeId = receivePacket.Data.Substring(0, 4);
            }
            else if(commandCode.Equals(VehicleMessageEx.COMMAND_CODE_O))            //Loading, Unloading
            {
                messageName = "O_CODE";
                nodeId = receivePacket.Data.Substring(receivePacket.Data.Length - 3);
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_M))           //Abnormal
            {
                messageName = "M_CODE";
                nodeId = receivePacket.Data.Substring(2, 4);
                String mType = receivePacket.Data.Substring(1, 1);
                vehicleMessage.MCodeType = mType;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_H))           //Alive Check Reply
            {
                messageName = "H_CODE";
            }
            vehicleMessage.MessageName = messageName;
            vehicleMessage.VehicleId = vehicleId;
            vehicleMessage.NodeId = nodeId;

            vehicleMessage.KeyData = receivePacket.Data;

            return vehicleMessage;
#else
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            vehicleMessage.ReceivedMessage = receivePacket;
            vehicleMessage.VehicleId = receivePacket.SendId;

            String nodeId = "";
            String stationId = "";
            String messageName = "";
            String vehicleId = receivePacket.SendId;
            String commandCode = receivePacket.Command;
            if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_C))                //Command Reply
            {
                messageName = "C_CODE_REP";
                stationId = receivePacket.Data.Substring(0, 4);
                vehicleMessage.StationId = stationId;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_T))           //Tag Information
            {
                messageName = "T_CODE";
                nodeId = receivePacket.Data.Substring(2, 4);
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_S))           //AGV State
            {
                messageName = "S_CODE";
                stationId = receivePacket.Data.Substring(0, 4);
                String runState = receivePacket.Data.Substring(4, 1);
                String fullState = receivePacket.Data.Substring(5, 1);

                vehicleMessage.RunState = runState;
                vehicleMessage.FullState = fullState;
                vehicleMessage.StationId = stationId;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_E))           //Error Code
            {
                messageName = "E_CODE";
                //200423 Modify Ignore E_Code Overlap (Vehicle Null Exception)
                //String errorCode = receivePacket.Data.Substring(0, 4);
                //V1은 AlarmID 3자리를 유지하자 (NA_A_ALARMSPEC AlarmID Column이 현재 3자리라 유지해야 함)
                String errorCode = receivePacket.Data.Substring(1, 3);
                //
                vehicleMessage.ErrorCode = errorCode;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_R))           //Return Value
            {
                messageName = "R_CODE";
                String rType = receivePacket.Data.Substring(1, 1);
                vehicleMessage.RCodeType = rType;
                if (rType.Equals("1", StringComparison.OrdinalIgnoreCase))          //AGV 전압
                {
                    String voltage = receivePacket.Data.Substring(2, 2) + "." + receivePacket.Data.Substring(4, 1);

                    float batteryVoltage;
                    float.TryParse(voltage, out batteryVoltage);
                    vehicleMessage.BatteryVoltage = batteryVoltage;
                }
                else if (rType.Equals("2", StringComparison.OrdinalIgnoreCase))     //AGV 용량
                {
                    String capa = receivePacket.Data.Substring(3, 2);

                    //181004
                    int batteryRate = 0;
                    int.TryParse(capa, out batteryRate);
                    //int batteryRate = Convert.ToInt32(capa);
                    vehicleMessage.BatteryRate = batteryRate;
                }
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_L))           //Loading
            {
                messageName = "L_CODE";
                nodeId = receivePacket.Data.Substring(0, 4);
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_U))           //Unloading
            {
                messageName = "U_CODE";
                nodeId = receivePacket.Data.Substring(0, 4);
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_M))           //Abnormal
            {
                messageName = "M_CODE";
                nodeId = receivePacket.Data.Substring(2, 4);
                String mType = receivePacket.Data.Substring(1, 1);
                vehicleMessage.MCodeType = mType;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_H))           //Alive Check Reply
            {
                messageName = "H_CODE";
            }
            else
            {
                messageName = "UNKNOWNMESSAGE";
            }

            vehicleMessage.MessageName = messageName;
            vehicleMessage.VehicleId = vehicleId;
            vehicleMessage.NodeId = nodeId;

            vehicleMessage.KeyData = receivePacket.Data;

            return vehicleMessage;
#endif
        }

        public ControlMessageEx CreateControlMessage(XmlDocument document)
        {
            ControlMessageEx controlMessage = new ControlMessageEx();
            controlMessage.ReceivedMessage = document;

            controlMessage.MessageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");

            controlMessage.ApplicationName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/APPLICATIONNAME");
            controlMessage.ApplicationType = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/APPLICATIONTYPE");

            // controlMessage.SecsName= document.SelectSingleNode("/MESSAGE/DATA/SECSNAME").InnerText;  //suji
            // controlMessage.NioName= document.SelectSingleNode("/MESSAGE/DATA/NIONAME").InnerText;  //suji

            //200622 Change NIO Logic About ES.exe does not restart
            controlMessage.NioName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/NIONAME");
            //

            SetHeaderInfoToMessage(document, controlMessage);
            SetOriginatedInfoToMessage(document, controlMessage);

            return controlMessage;
        }

        public void SendVehicleMessage(XmlDocument document)
        {
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessage(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageTCodeEnter(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTCodeEnterDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleUpdateJson(string jsonMessage)
        {
            // Send(object) → Send(object, DefaultDestination) 경로를 사용.
            // Send(string) 경로는 NotImplementedException을 던지므로 object 캐스팅 사용.
            this.tsAgent.Send((object)jsonMessage);
        }

        public void SendJobReportToHost(string reportType, string jobId, string amrId,
            string actionType, string materialType)
        {
            if (this.hostAgent == null)
            {
                logger.Error("SendJobReportToHost: hostAgent is not wired");
                return;
            }

            string acsId = Configuration?["Acs:Process:Name"] ?? "ACS01";
            string destSubject = Configuration?["Acs:Host:DestSubject"] ?? "/HQ/MES01";
            string replySubject = Configuration?["Acs:Host:ReplySubject"] ?? "/HQ/ACS01";

            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(decl);

            var msg = doc.CreateElement("Msg");
            doc.AppendChild(msg);

            var cmdElem = doc.CreateElement("Command");
            cmdElem.InnerText = "JOBREPORT";
            msg.AppendChild(cmdElem);

            var header = doc.CreateElement("Header");
            msg.AppendChild(header);
            var destElem = doc.CreateElement("DestSubject");
            destElem.InnerText = destSubject;
            header.AppendChild(destElem);
            var replyElem = doc.CreateElement("ReplySubject");
            replyElem.InnerText = replySubject;
            header.AppendChild(replyElem);

            var dataLayer = doc.CreateElement("DataLayer");
            msg.AppendChild(dataLayer);
            foreach (var (name, value) in new[]
            {
                ("AcsId", acsId), ("Type", reportType), ("AmrId", amrId ?? ""),
                ("ActionType", actionType ?? ""), ("JobID", jobId),
                ("MaterialType", materialType ?? ""), ("UserID", "")
            })
            {
                var elem = doc.CreateElement(name);
                elem.InnerText = value;
                dataLayer.AppendChild(elem);
            }

            this.hostAgent.Send(doc, true, "TRSJOBREPORT");
            logger.Info($"SendJobReportToHost: Type={reportType}, JobID={jobId}, AmrId={amrId}");
        }

        public void SendCarrierTransferJson(string jsonMessage)
        {
            if (this.esAgent == null)
            {
                logger.Error("SendCarrierTransferJson: esAgent is not wired");
                return;
            }

            string destinationName = null;

            try
            {
                // 1. JSON에서 vehicleId 추출
                string vehicleId = null;
                using (JsonDocument doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out JsonElement dataEl) &&
                        dataEl.TryGetProperty("vehicleId", out JsonElement vidEl))
                    {
                        vehicleId = vidEl.GetString();
                    }
                }

                if (string.IsNullOrEmpty(vehicleId))
                {
                    logger.Error("SendCarrierTransferJson: vehicleId를 JSON에서 추출할 수 없음");
                    return;
                }

                // 2. Vehicle 조회 → CommId 획득
                VehicleEx vehicle = this.resourceManager.GetVehicle(vehicleId);
                if (vehicle == null)
                {
                    logger.Error("SendCarrierTransferJson: vehicle not found - " + vehicleId);
                    return;
                }

                // 3. NA_C_MQTT 조회 (Vehicle.CommId = MqttConfig.Name)
                MqttConfig mqttConfig = (MqttConfig)this.PersistentDao.FindByName(typeof(MqttConfig), vehicle.CommId, false);
                if (mqttConfig == null)
                {
                    logger.Error("SendCarrierTransferJson: MqttConfig not found for CommId=" + vehicle.CommId);
                    return;
                }

                // 4. Application 조회
                ACS.Core.Application.Model.Application application =
                    this.applicationManager.GetApplication(mqttConfig.ApplicationName);
                if (application == null)
                {
                    logger.Error("SendCarrierTransferJson: Application not found - " + mqttConfig.ApplicationName);
                    return;
                }

                // 5. Destination 조합: DestinationName + "/" + Application.Name
                destinationName = application.DestinationName + "/" + application.Name;
            }
            catch (Exception ex)
            {
                logger.Error("SendCarrierTransferJson: destination 조회 실패 - " + ex.Message, ex);
            }

            if (string.IsNullOrEmpty(destinationName))
            {
                logger.Error("SendCarrierTransferJson: destination을 찾을 수 없음");
                return;
            }

            if (!destinationName.StartsWith("/"))
            {
                destinationName = "/" + destinationName;
            }

            this.esAgent.Send((object)jsonMessage, destinationName, false, "");
            logger.Info($"SendCarrierTransferJson: sent to {destinationName}");
        }

        public void SendCarrierTransferJson(string jsonMessage, string vehicleId)
        {
            string destinationName = null;

            try
            {
                VehicleEx vehicle = this.resourceManager.GetVehicle(vehicleId);
                if (vehicle == null)
                {
                    logger.Error("SendCarrierTransferJson: vehicle not found - " + vehicleId);
                    return;
                }

                if ("MQTT".Equals(vehicle.CommType, StringComparison.OrdinalIgnoreCase))
                {
                    // MQTT Vehicle: MqttConfig → ApplicationName → Application.DestinationName
                    IList mqttConfigs = this.PersistentDao.FindAll(typeof(MqttConfig));
                    if (mqttConfigs != null && mqttConfigs.Count > 0)
                    {
                        MqttConfig mqttConfig = (MqttConfig)mqttConfigs[0];
                        ACS.Core.Application.Model.Application application =
                            this.applicationManager.GetApplication(mqttConfig.ApplicationName);
                        if (application != null)
                        {
                            destinationName = application.DestinationName;
                        }
                    }
                }
                else
                {
                    // NIO Vehicle: ES09_P → NIO → Application.DestinationName
                    string rfServerName = "ES09_P";
                    IList nioList = this.nioInterfaceManager.GetNioesByApplicationName(rfServerName);
                    if (nioList != null && nioList.Count > 0)
                    {
                        Nio nio = (Nio)nioList[0];
                        if (nio != null)
                        {
                            ACS.Core.Application.Model.Application application =
                                this.applicationManager.GetApplication(nio.ApplicationName);
                            if (application != null)
                            {
                                destinationName = application.DestinationName;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("SendCarrierTransferJson: destination 조회 실패 - " + ex.Message, ex);
            }

            if (string.IsNullOrEmpty(destinationName))
            {
                logger.Error("SendCarrierTransferJson: destination을 찾을 수 없음, vehicleId=" + vehicleId);
                return;
            }

            if (!destinationName.StartsWith("/"))
            {
                destinationName = "/" + destinationName;
            }

            this.esAgent.Send((object)jsonMessage, destinationName, false, "");
            logger.Info($"SendCarrierTransferJson: sent to {destinationName}, vehicleId={vehicleId}");
        }

        public void SendVehicleMessageTCodePermission(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTCodePermissionDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageCCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateCCodeDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageSCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateSCodeDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageRCodeVoltage(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateRCodeVoltageDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageRCodeCapacity(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateRCodeCapacityDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeChargeStart(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeChargeStartDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeChargeComplete(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeChargeCompleteDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeDestOccupied(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeDestOccupiedDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeSourceEmpty(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeSourceEmptyDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeVehicleEmpty(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeVehicleEmptyDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeVehicleOccupied(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeVehicleOccupiedDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }
        public void SendVehicleMessageOCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateOCodeDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageLCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateLCodeDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageUCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateUCodeDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageECode(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateECodeDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendTransportCommandSource(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateTransportCommandSourceDocument(transferMessage);
            SendMessageToAcsEs(document);
        }

        public void SendTransportCommandSource(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTransportCommandSourceDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendTransportCommandDest(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTransportCommandDestDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendTransportCommandWaitpoint(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateTransportCommandWaitpointDocument(transferMessage);
            SendMessageToAcsEs(document);
        }

        public void SendTransportCommandWaitpoint0000(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateTransportCommandWaitpoint0000Document(transferMessage);
            SendMessageToAcsEs(document);
        }

        public void SendTransportCommandWaitpoint(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTransportCommandWaitpointDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendTransportCommandWaitpoint0000(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTransportCommandWaitpoint0000Document(vehicleMessage);
            SendMessageToAcsEs(document);
        }
        //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
        public void SendTransportCommandWaitpoint0000_RGV(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTransportCommandWaitpoint0000Document_RGV(vehicleMessage);
            SendMessageToAcsEs(document);
        }
        public void SendTransportCommandVehicleDestNode(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateTransportCommandVehicleDestNodeDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendMoveCommandTarget(UiMoveVehicleMessageEx uiMessage)
        {
            XmlDocument document = CreateMoveCommandTargetDocument(uiMessage);
            SendMessageToAcsEs(document);
        }

        public void SendVehiclePermitCommand(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateVehiclePermitCommandDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendOtherVehiclePermitCommand(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateVehiclePermitCommandDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendBatteryVoltageReq(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateBatteryVoltageReqDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendBatteryCapacityReq(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateBatteryCapacityReqDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public XmlDocument CreateDocument(ControlMessage controlMessage)
        {
            XmlDocument document = CreateDocument((AbstractMessage)controlMessage);

            XmlElement data = document.DocumentElement["DATA"];

            XmlNode applicationName = document.CreateNode(XmlNodeType.Element, "APPLICATIONNAME", "");
            XmlNode applicationType = document.CreateNode(XmlNodeType.Element, "APPLICATIONTYPE", "");

            applicationName.InnerText = controlMessage.ApplicationName;
            applicationType.InnerText = controlMessage.ApplicationType;

            data.AppendChild(applicationName);
            data.AppendChild(applicationType);

            return document;
        }



        public XmlDocument CreateDocument(AbstractMessage abstractMessage)
        {
            XmlDocument document = (XmlDocument)this.templateDocument.Clone();

            SetHeaderInfoToDocument(abstractMessage, document);
            SetOriginatedInfoToDocument(abstractMessage, document);

            XmlElement tailElement = document.DocumentElement["TAIL"];
            if (abstractMessage.ReceivedMessage != null)
            {
                if (!(abstractMessage.ReceivedMessage is XmlDocument))
                {
                    tailElement.InnerText = abstractMessage.ReceivedMessage.ToString();          //{A001E020500}
                    //tailElement.addContent(abstractMessage.getReceivedMessage().toString());  -- addcontect에 string이 들어감 ... 어떻게 이럴수가? 
                }
            }
            else
            {
                tailElement.InnerText = "";
            }
            return document;
        }

        public XmlDocument CreateDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument((AbstractMessage)vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierId, "");
            element.InnerText = vehicleMessage.CarrierId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateRCodeVoltageDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = "RAIL-BATTERYVOLTAGEREPLY";
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "VOLTAGE", "");
            element.InnerText = vehicleMessage.BatteryVoltage.ToString();
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateRCodeCapacityDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = "RAIL-BATTERYCAPACITYREPLY";
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CAPACITY", "");
            element.InnerText = vehicleMessage.BatteryRate.ToString();
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeChargeStartDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = "RAIL-VEHICLECHARGESTART";
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeVersionDocument(VehicleMessageEx vehicleMessage, String messageName)
        {
            vehicleMessage.MessageName = (messageName);
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "VERSION", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeChargeCompleteDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLECHARGECOMPLETED");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeDestOccupiedDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-DESTOCCUPIED");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeSourceEmptyDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-SOURCEEMPTY");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeVehicleEmptyDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLEEMPTY");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeVehicleOccupiedDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLEOCCUPIED");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateCCodeDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-CARRIERTRANSFERREPLY");

            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];


            XmlNode command = document.CreateNode(XmlNodeType.Element, "COMMANDID", "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode dextnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, "PRIORITY", "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, "CARRIERTYPE", "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, "RESULTCODE", "");

            command.InnerText = "";
            vehicleid.InnerText = vehicleMessage.VehicleId;
            destportid.InnerText = "";
            dextnodeid.InnerText = vehicleMessage.StationId;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "0";

            data.AppendChild(command);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(dextnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTCodeEnterDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLELOCATIONCHANGED");

            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateTCodePermissionDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLELGOCOMMANDREPLY");

            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateTCodeReplyDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLELGOCOMMANDREPLY");

            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCurrentUnitPosition, "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateInformDocument(VehicleMessageEx vehicleMessage, String text)
        {
            vehicleMessage.MessageName = ("RAIL-INFORM");

            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "TEXT", "");
            element.InnerText = text;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateSCodeDocument(VehicleMessageEx vehicleMessage)
        {
            if (vehicleMessage.MessageName == "S_CODE")
            {
                vehicleMessage.MessageName = ("RAIL-VEHICLEINFOREPORT");
            }
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode stationid = document.CreateNode(XmlNodeType.Element, "STATIONID", "");
            XmlNode runstate = document.CreateNode(XmlNodeType.Element, "RUNSTATE", "");
            XmlNode fullstate = document.CreateNode(XmlNodeType.Element, "FULLSTATE", "");

            vehicleid.InnerText = vehicleMessage.VehicleId;
            stationid.InnerText = vehicleMessage.StationId;
            runstate.InnerText = vehicleMessage.RunState;
            fullstate.InnerText = vehicleMessage.FullState;
            data.AppendChild(vehicleid);
            data.AppendChild(stationid);
            data.AppendChild(runstate);
            data.AppendChild(fullstate);

            return document;
        }

        public XmlDocument CreateECodeDocument(VehicleMessageEx vehicleMessage)
        {
            if (vehicleMessage.MessageName == "E_CODE")
            {
                vehicleMessage.MessageName = ("RAIL-ALARMREPORT");
            }
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];


            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;       //"VEHICLEID"
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "ERRORCODE", "");
            element.InnerText = vehicleMessage.ErrorCode;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateOCodeDocument(VehicleMessageEx vehicleMessage)
        {
            if (vehicleMessage.MessageName == "O_CODE")
            {
                vehicleMessage.MessageName = ("RAIL-VEHICLEOCODESEPERATOR");
            }
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateLCodeDocument(VehicleMessageEx vehicleMessage)
        {
            if (vehicleMessage.MessageName == "L_CODE")
            {
                vehicleMessage.MessageName = ("RAIL-VEHICLEACQUIRECOMPLETED");
            }
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateUCodeDocument(VehicleMessageEx vehicleMessage)
        {
            if (vehicleMessage.MessageName == "U_CODE")
            {
                vehicleMessage.MessageName = ("RAIL-VEHICLEDEPOSITCOMPLETED");
            }
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateTransportCommandSourceDocument(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateDocument(transferMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";
            XmlElement data = document.DocumentElement["DATA"];

            String source = transferMessage.SourceMachine + ":" + transferMessage.SourceUnit;
            String destNodeId = this.resourceManager.GetLocation(source).StationId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = transferMessage.TransportCommandId;
            vehicleid.InnerText = transferMessage.VehicleId;
            destportid.InnerText = source;
            destnodeid.InnerText = destNodeId;
            priority.InnerText = transferMessage.Priority.ToString();
            carriertype.InnerText = transferMessage.CarrierType.ToString();
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTransportCommandSourceDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";

            XmlElement data = document.DocumentElement["DATA"];

            TransportCommandEx transportCommand = this.transferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand == null)
            {
                return null;
            }
            String dest = transportCommand.Source;
            String destNodeId = this.resourceManager.GetLocation(dest).StationId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = transportCommand.JobId;
            vehicleid.InnerText = transportCommand.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = destNodeId;
            priority.InnerText = transportCommand.Priority.ToString();
            carriertype.InnerText = transportCommand.CarrierId.ToString();
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTransportCommandDestDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";

            XmlElement data = document.DocumentElement["DATA"];

            TransportCommandEx transportCommand = this.transferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand == null)
            {
                //logger.warn("transportCommand does not exist in repository, vehicleId{" + vehicleMessage.getVehicleId() + "}");
                return null;
            }
            String dest = transportCommand.Dest;
            String destNodeId = this.resourceManager.GetLocation(dest).StationId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = transportCommand.JobId;
            vehicleid.InnerText = transportCommand.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = destNodeId;
            priority.InnerText = transportCommand.Priority.ToString();
            carriertype.InnerText = transportCommand.CarrierId.ToString();
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTransportCommandWaitpointDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";

            XmlElement data = document.DocumentElement["DATA"];

            String dest = vehicleMessage.DestPortId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = "";
            vehicleid.InnerText = vehicleMessage.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTransportCommandWaitpoint0000Document(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";

            XmlElement data = document.DocumentElement["DATA"];

            String dest = "9816";

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = "";
            vehicleid.InnerText = vehicleMessage.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }
        //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
        public XmlDocument CreateTransportCommandWaitpoint0000Document_RGV(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";

            XmlElement data = document.DocumentElement["DATA"];

            String dest = "9000";

            // 작업자가 강제적으로 RGV에 Tray를 올려 놓았을때 AB Buffer로 보내기 위함
            // AB Buffer TAGID를 Vehicle의 Path 컬럼에 값을 넣고 사용함
            if (vehicleMessage.Vehicle.Path != null)
            {
                dest = vehicleMessage.Vehicle.Path.ToString().Trim();
            }

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = "";
            vehicleid.InnerText = vehicleMessage.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }
        public XmlDocument CreateTransportCommandWaitpointDocument(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateDocument(transferMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";
            XmlElement data = document.DocumentElement["DATA"];

            String dest = transferMessage.DestNodeId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = "";
            vehicleid.InnerText = transferMessage.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTransportCommandWaitpoint0000Document(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateDocument(transferMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";

            XmlElement data = document.DocumentElement["DATA"];

            String dest = "9816";

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = "";
            vehicleid.InnerText = transferMessage.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateTransportCommandVehicleDestNodeDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";
            XmlElement data = document.DocumentElement["DATA"];

            String dest = "";
            if ((vehicleMessage.DestNodeId != null) && (!string.IsNullOrEmpty(vehicleMessage.DestNodeId)))
            {
                dest = vehicleMessage.DestNodeId;
            }
            else
            {
                dest = vehicleMessage.DestPortId;
            }

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");


            commandid.InnerText = "";
            vehicleid.InnerText = vehicleMessage.VehicleId;
            destportid.InnerText = dest;

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);

            if (!string.IsNullOrEmpty(dest) && dest.Length > 4)
            {
                dest = this.resourceManager.GetLocation(dest).StationId;
            }

            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);

            return document;
        }

        public XmlDocument CreateMoveCommandTargetDocument(UiMoveVehicleMessageEx uiMessage)
        {
            XmlDocument document = CreateDocument(uiMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";
            XmlElement data = document.DocumentElement["DATA"];

            String dest = uiMessage.NodeId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = "";
            vehicleid.InnerText = uiMessage.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = dest;
            priority.InnerText = "";
            carriertype.InnerText = "";
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);


            uiMessage.Cause = "0";

            return document;
        }

        public XmlDocument CreateVehiclePermitCommandDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-VEHICLELGOCOMMAND";

            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateBatteryVoltageReqDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-BATTERYVOLTAGEREQUEST";
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateBatteryCapacityReqDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-BATTERYCAPACITYREQUEST";
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            return document;
        }

        public UiTransportMessageEx CreateUiTransportMessage(XmlDocument document)
        {
            UiTransportMessageEx uiMessage = new UiTransportMessageEx();
            uiMessage.ReceivedMessage = document;

            uiMessage.MessageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");

            uiMessage.SourcePortId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/SOURCEPORTID");
            uiMessage.DestPortId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTPORTID");
            uiMessage.RequestId = XmlUtility.GetDataFromXml(document, "//REQUESTID");
            uiMessage.UserName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/USERID");

            SetHeaderInfoToMessage(document, uiMessage);
            SetOriginatedInfoToMessage(document, uiMessage);

            return uiMessage;
        }

        public UiTransportCancelMessageEx CreateUiTransportCancelMessage(XmlDocument document)
        {
            UiTransportCancelMessageEx uiMessage = new UiTransportCancelMessageEx();
            uiMessage.ReceivedMessage = document;

            uiMessage.MessageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");

            uiMessage.TransportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/TRANSPORTCOMMANDID");
            uiMessage.RequestId = XmlUtility.GetDataFromXml(document, "//REQUESTID");
            uiMessage.UserName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/USERID");

            SetHeaderInfoToMessage(document, uiMessage);
            SetOriginatedInfoToMessage(document, uiMessage);

            return uiMessage;
        }

        public UiTransportDeleteMessageEx CreateUiTransportDeleteMessage(XmlDocument document)
        {
            UiTransportDeleteMessageEx uiMessage = new UiTransportDeleteMessageEx();
            uiMessage.ReceivedMessage = document;

            uiMessage.MessageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");

            uiMessage.TransportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/TRANSPORTCOMMANDID");
            uiMessage.RequestId = XmlUtility.GetDataFromXml(document, "//REQUESTID");
            uiMessage.UserName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/USERID");

            //181001

            uiMessage.ErrorCode = "99";
            uiMessage.ErrorMsg = "UI_CANCEL";

            SetHeaderInfoToMessage(document, uiMessage);
            SetOriginatedInfoToMessage(document, uiMessage);

            return uiMessage;
        }

        public UiMoveVehicleMessageEx CreateUiMoveVehicleMessage(XmlDocument document)
        {
            UiMoveVehicleMessageEx uiMessage = new UiMoveVehicleMessageEx();
            uiMessage.ReceivedMessage = document;

            uiMessage.MessageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");

            uiMessage.VehicleId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");
            uiMessage.NodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");
            uiMessage.RequestId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/REQUESTID");
            uiMessage.UserName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/USERID");

            SetHeaderInfoToMessage(document, uiMessage);
            SetOriginatedInfoToMessage(document, uiMessage);

            return uiMessage;
        }

        public void ReplyMessageToUi(AbstractMessage abstractMessage)
        {
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            XmlDocument originDocument = (XmlDocument)abstractMessage.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        public XmlDocument CreateReplyDocument(AbstractMessage abstractMessage)
        {
            XmlDocument document = (XmlDocument)this.templateDocument.Clone();

            SetHeaderInfoToDocument(abstractMessage, document);
            SetOriginatedInfoToDocument(abstractMessage, document);

            XmlElement data = document.DocumentElement["DATA"];

            String resultMessage = abstractMessage.Cause.Length == 0 ? "OK" : abstractMessage.Cause;


            XmlNode element = document.CreateNode(XmlNodeType.Element, "RESULTMESSAGE", "");
            element.InnerText = resultMessage;
            data.AppendChild(element);
            return document;
        }

        public void ReplyTransportCarrierCreateNGToUi(AbstractMessage abstractMessage)
        {
            abstractMessage.MessageName = "UI-TRANSPORTREP";
            abstractMessage.Cause = "Fail to Create CARRIER";
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            UiTransportMessageEx message = (UiTransportMessageEx)abstractMessage.ReceivedMessage;
            XmlDocument originDocument = (XmlDocument)message.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        public void ReplyTransportExistJobNGToUi(AbstractMessage abstractMessage)
        {
            abstractMessage.MessageName = "UI-TRANSPORTREP";
            abstractMessage.Cause = "This TRANSPORTJOB Alreadt Exist";
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            UiTransportMessageEx message = (UiTransportMessageEx)abstractMessage.ReceivedMessage;
            XmlDocument originDocument = (XmlDocument)message.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        public void ReplyTransportJobCreateNGToUi(AbstractMessage abstractMessage)
        {
            abstractMessage.MessageName = "UI-TRANSPORTREP";
            abstractMessage.Cause = "Fail to Create TRANSPORTJOB";
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            UiTransportMessageEx message = (UiTransportMessageEx)abstractMessage.ReceivedMessage;
            XmlDocument originDocument = (XmlDocument)message.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        public void ReplyTransportMessageValidationNGToUi(AbstractMessage abstractMessage)
        {
            abstractMessage.MessageName = "UI-TRANSPORTREP";
            abstractMessage.Cause = "This TransportCommand Message is Wrong";
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            UiTransportMessageEx message = (UiTransportMessageEx)abstractMessage.ReceivedMessage;
            XmlDocument originDocument = (XmlDocument)message.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        public void ReplyTransportPathValidationNGToUi(AbstractMessage abstractMessage)
        {
            abstractMessage.MessageName = "UI-TRANSPORTREP";
            abstractMessage.Cause = "This Path is wrong";
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            UiTransportMessageEx message = (UiTransportMessageEx)abstractMessage.ReceivedMessage;
            XmlDocument originDocument = (XmlDocument)message.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        public void ReplyTransportStationValidationNGToUi(AbstractMessage abstractMessage)
        {
            abstractMessage.MessageName = "UI-TRANSPORTREP";
            abstractMessage.Cause = "This Source, Dest port is Worng";
            XmlDocument replyDocument = CreateReplyDocument(abstractMessage);
            UiTransportMessageEx message = (UiTransportMessageEx)abstractMessage.ReceivedMessage;
            XmlDocument originDocument = (XmlDocument)message.ReceivedMessage;
            if ((this.uiAgent is ISynchronousMessageAgent))
            {
                ((ISynchronousMessageAgent)this.uiAgent).Reply(replyDocument, originDocument, true, abstractMessage.MessageName);
            }
            else
            {
                this.uiAgent.Send(replyDocument, abstractMessage.OriginatedName, true, abstractMessage.MessageName);
            }
        }

        //181213 WIFI RF ZIGBEE available
        public void SendMessageToAcsEs(XmlDocument document)
        {
            if (document != null)
            {
                string agvName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");
                VehicleEx vehicle = this.resourceManager.GetVehicle(agvName);

                if (vehicle == null)
                {
                    logger.Error("not found vehicle : id is {" + agvName + "}");
                    return;
                }

                //KSB
                IList listNio = this.nioInterfaceManager.GetNiosByAGVName(agvName, "WIFI");

                if ((listNio != null) && (listNio.Count > 0))
                {
                    //WIFI
                    SendMessageToAcsEsByWifiZigbee(listNio, document);
                    return;
                }
                else   //REPEATER
                {
                    //KSB
                    #region RF분리시
                    //if (!(vehicle.BayId.Equals("ICE-CP-CLN") || vehicle.BayId.Equals("OLB(A)-AMT(A)")
                    //    || vehicle.BayId.Equals("ASSY2-ASSY3") || vehicle.BayId.Equals("OQC-PK") || vehicle.BayId.Equals("OQC-EMPTY")
                    //    || vehicle.BayId.Equals("LIFT_1F-MTP_PASS") || vehicle.BayId.Equals("RACK-MTP")))
                    //{
                    //    rfServerName = "ES10_P";
                    //}
                    //else
                    //{
                    //    rfServerName = "ES09_P";
                    //}
                    #endregion

                    rfServerName = "ES09_P";

                    IList nioList = this.nioInterfaceManager.GetNioesByApplicationName(rfServerName);

                    ACS.Core.Application.Model.Application application = this.ApplicationManager.GetApplication(rfServerName);

                    if ((nioList != null) && (nioList.Count > 0))
                    {
                        Nio nio = (Nio)nioList[0];

                        if (nio != null)
                        {
                            string esName = nio.ApplicationName;
                            application = this.applicationManager.GetApplication(esName);

                            string destinationName = application.DestinationName;
                            if (!destinationName.StartsWith("/"))
                            {
                                destinationName = "/" + application.DestinationName;
                            }
                            String messageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");
                            String transactionId = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/TRANSACTIONID");
                            String carrierName = "";
                            String transportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/COMMANDID");
                            String currentUnitName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");

                            String currentMachineName = "";
                            if (messageName.Equals("RAIL-VEHICLELGOCOMMAND", StringComparison.OrdinalIgnoreCase))
                            {
                                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
                            }
                            else if (messageName.Equals("RAIL-CARRIERTRANSFER", StringComparison.OrdinalIgnoreCase))
                            {
                                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");
                            }
                            else if (messageName.Equals("RAIL-VEHICLEORDERCOMMAND", StringComparison.OrdinalIgnoreCase))
                            {
                                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");
                            }
                            this.esAgent.Send(document, destinationName, false, "");
                            //logger.Info(XmlUtils.toStringPrettyFormat(document), transactionId, messageName, carrierName, transportCommandId, currentMachineName, currentUnitName, messageName);
                        }
                    }
                }
            }
            else
            {
                logger.Error("input agument null, document{" + document + "}");
            }

            #region 원래코드
            /* 원래 코드
            if (document != null)
            {
                string agvName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");
                VehicleEx vehicle = this.resourceManager.GetVehicle(agvName);

                if (vehicle == null)
                {
                    logger.Error("not found vehicle : id is {" + agvName + "}");
                    return;
                }

                ACS.Core.Application.Model.Application application = this.ApplicationManager.GetApplication(rfServerName);

                if ((application == null) || (application.State.Equals("inactive")))
                {
                    IList listNio = this.nioInterfaceManager.GetNiosByAGVName(agvName, "WIFI");

                    if ((listNio != null) && (listNio.Count > 0))
                    {
                        //WIFI
                        SendMessageToAcsEsByWifiZigbee(listNio, document);
                        return;
                    }

                    IList listZigbeeNio = this.nioInterfaceManager.GetNiosByAGVName(vehicle.CommId, "ZIGBEE");
                    
                    if ((listZigbeeNio != null) && (listZigbeeNio.Count > 0))
                    {
                        //ZIGBEE
                        SendMessageToAcsEsByWifiZigbee(listZigbeeNio, document);
                        return;
                    }
                }
                else
                {
                    //RF
                    //IList nioList = this.nioInterfaceManager.GetNiosByMachineName("REPEATER");

                    //KSB
                    if (!(vehicle.BayId.Equals("ICE-CP-CLN") || vehicle.BayId.Equals("OLB(A)-AMT(A)")
                        || vehicle.BayId.Equals("ASSY2-ASSY3")))
                    {
                        rfServerName = "ES10_P";
                    }
                    else
                    {
                        rfServerName = "ES09_P";
                    }

                    IList nioList = this.nioInterfaceManager.GetNioesByApplicationName(rfServerName);


                    if ((nioList != null) && (nioList.Count > 0))
                    {
                        Nio nio = (Nio)nioList[0];

                        if (nio != null)
                        {
                            string esName = nio.ApplicationName;
                            application = this.applicationManager.GetApplication(esName);

                            string destinationName = application.DestinationName;
                            if (!destinationName.StartsWith("/"))
                            {
                                destinationName = "/" + application.DestinationName;
                            }
                            String messageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");
                            String transactionId = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/TRANSACTIONID");
                            String carrierName = "";
                            String transportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/COMMANDID");
                            String currentUnitName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");

                            String currentMachineName = "";
                            if (messageName.Equals("RAIL-VEHICLELGOCOMMAND", StringComparison.OrdinalIgnoreCase))
                            {
                                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
                            }
                            else if (messageName.Equals("RAIL-CARRIERTRANSFER", StringComparison.OrdinalIgnoreCase))
                            {
                                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");
                            }
                            this.esAgent.Send(document, destinationName, false, "");
                            //logger.Info(XmlUtils.toStringPrettyFormat(document), transactionId, messageName, carrierName, transportCommandId, currentMachineName, currentUnitName, messageName);
                        }
                    }
                }
            }
            else
            {
                logger.Error("input agument null, document{" + document + "}");
            }
            */
            #endregion //원래코드 end

            #region 주석처리 코드
            //nio interface가 필요함
            //if (document != null)
            //{
            //    String agvId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");
            //    VehicleEx vehicle = this.resourceManager.GetVehicle(agvId);

            //    if (vehicle == null)
            //    {
            //        logger.Error("not found vehicle : id is {" + agvId + "}");
            //        return;
            //    }

            //    Nio nio = this.nioInterfaceManager.GetNio(vehicle.NioId);
            //    if (nio != null)
            //    {
            //        String esName = nio.ApplicationName;
            //        ACS.Core.Application.Model.Application application = this.applicationManager.GetApplication(esName);
            //        if ((application == null) || (application.State.Equals("inactive")))
            //        {
            //            logger.Warn("Can Not sendMessageToAcsEs(" + esName + ") is Not Active or not define application");
            //        }
            //        else
            //        {
            //            String destinationName = application.DestinationName;
            //            if (!destinationName.StartsWith("/"))
            //            {
            //                destinationName = "/" + application.DestinationName;
            //            }
            //            String messageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");
            //            String transactionId = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/TRANSACTIONID");
            //            String carrierName = "";
            //            String transportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/COMMANDID");
            //            String currentUnitName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");

            //            String currentMachineName = "";
            //            if (messageName.Equals("RAIL-VEHICLELGOCOMMAND", StringComparison.OrdinalIgnoreCase))
            //            {
            //                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
            //            }
            //            else if (messageName.Equals("RAIL-CARRIERTRANSFER", StringComparison.OrdinalIgnoreCase))
            //            {
            //                currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");
            //            }
            //            this.esAgent.Send(document, destinationName, false, "");
            //            //logger.Info(XmlUtils.toStringPrettyFormat(document), transactionId, messageName, carrierName, transportCommandId, currentMachineName, currentUnitName, messageName);
            //        }
            //    }
            //}
            //else
            //{
            //    logger.Error("input agument null, document: public void SendMessageToAcsEs(XmlDocument document)");

            //    if (document != null)
            //    {
            //        logger.Error("input agument null, document{" + document.InnerXml + "}");
            //    }
            //}
            #endregion //주석처리코드 End

        }
        public void SendTransportCommandDest(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateTransportCommandDestDocument(transferMessage);

            SendMessageToAcsEs(document);
        }

        public XmlDocument CreateTransportCommandDestDocument(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateDocument(transferMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";
            XmlElement data = document.DocumentElement["DATA"];

            TransportCommandEx transportCommand = this.transferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);
            if (transportCommand == null)
            {
                return null;
            }
            String dest = transportCommand.Dest;
            String destNodeId = this.resourceManager.GetLocation(dest).StationId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = transportCommand.JobId;
            vehicleid.InnerText = transportCommand.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = destNodeId;
            priority.InnerText = transportCommand.Priority.ToString();
            carriertype.InnerText = transportCommand.CarrierId;
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);



            return document;
        }

        public XmlDocument CreateMCodePIOErrorDocument(VehicleMessageEx vehicleMessage, String messageName)
        {
            vehicleMessage.MessageName = messageName;
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            vehicleid.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(vehicleid);

            XmlNode nodeid = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            nodeid.InnerText = vehicleMessage.NodeId;
            data.AppendChild(nodeid);

            return document;
        }

        public void SendVehicleMessageMCodeDestPIOConnectError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-DESTPIOCONNECTERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeDestPIORequestError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-DESTPIOREQUESTERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeDestPIORunError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-DESTPIORUNERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeDestPIOPortCheckError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-DESTPIOPORTCHECKERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeSourcePIOConnectError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-SOURCEPIOCONNECTERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeSourcePIORequestError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-SOURCEPIOREQUESTERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeSourcePIORunError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-SOURCEPIORUNERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeSourcePIOPortCheckError(VehicleMessageEx vehicleMessage)
        {
            String messageName = "RAIL-SOURCEPIOPORTCHECKERROR";
            XmlDocument document = CreateMCodePIOErrorDocument(vehicleMessage, messageName);
            this.tsAgent.Send(document);
        }

        public VehicleMessageEx CreateVehicleMessage(VehicleEx vehicle)
        {
            VehicleMessageEx vehicleMessage = new VehicleMessageEx();

            vehicleMessage.MessageName = "C_CODE";
            vehicleMessage.VehicleId = vehicle.VehicleId;
            vehicleMessage.Vehicle = vehicle;
            vehicleMessage.NodeId = "";

            return vehicleMessage;
        }

        public void SendVehicleMessageMCodeCarrierLoaded(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeCarrierLoaded(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeCarrierRemoved(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeCarrierRemoved(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public XmlDocument CreateMCodeCarrierLoaded(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = "RAIL-CARRIERLOADED";
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeCarrierRemoved(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = "RAIL-CARRIERREMOVED";
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public void SendVehicleMessageMCodePortError(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodePortErrorDocument(vehicleMessage);
            this.tsAgent.Send(document);
        }

        public XmlDocument CreateMCodePortErrorDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = "RAIL-PORTERROR";
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateTransportCommandNewDestDocument(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateDocument(transferMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-CARRIERTRANSFER";
            XmlElement data = document.DocumentElement["DATA"];

            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                return null;
            }
            String dest = this.transferManager.GetAdditionalInfo(transportCommand, "newDest");
            String destNodeId = this.resourceManager.GetLocation(dest).StationId;

            XmlNode commandid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCommandId, "");
            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            XmlNode destportid = document.CreateNode(XmlNodeType.Element, "DESTPORTID", "");
            XmlNode destnodeid = document.CreateNode(XmlNodeType.Element, "DESTNODEID", "");
            XmlNode priority = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode carriertype = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode resultcode = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");

            commandid.InnerText = transportCommand.JobId;
            vehicleid.InnerText = transportCommand.VehicleId;
            destportid.InnerText = dest;
            destnodeid.InnerText = destNodeId;
            priority.InnerText = transportCommand.Priority.ToString();
            carriertype.InnerText = transportCommand.CarrierId;
            resultcode.InnerText = "";

            data.AppendChild(commandid);
            data.AppendChild(vehicleid);
            data.AppendChild(destportid);
            data.AppendChild(destnodeid);
            data.AppendChild(priority);
            data.AppendChild(carriertype);
            data.AppendChild(resultcode);


            return document;
        }

        public void SendTransportCommandNewDest(TransferMessageEx transferMessage)
        {
            XmlDocument document = CreateTransportCommandNewDestDocument(transferMessage);
            SendMessageToAcsEs(document);
        }

        public void SendRestartNio(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateRestartNioDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendRestartNio(VehicleEx vehicle)
        {
            XmlDocument document = CreateRestartNioDocument(vehicle);
            SendMessageToAcsEs(document);
        }

        public XmlDocument CreateRestartNioDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-RESTARTNIO";
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            vehicleid.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(vehicleid);

            return document;
        }

        public XmlDocument CreateRestartNioDocument(VehicleEx vehicle)
        {
            XmlDocument document = (XmlDocument)this.templateDocument.Clone();

            SetDefaultHeaderInfoToDocument(document);

            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-RESTARTNIO";
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            vehicleid.InnerText = vehicle.VehicleId;
            data.AppendChild(vehicleid);

            return document;
        }

        public void SendVehicleMessageInform(VehicleMessageEx vehicleMessage, String text)
        {
            XmlDocument document = CreateInformDocument(vehicleMessage, text);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageInform(VehicleMessageEx vehicleMessage, String text, String description)
        {
            XmlDocument document = CreateInformDocument(vehicleMessage, text, description);
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeConveyorLoadingTimeout(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeDocument(vehicleMessage, "RAIL-CONVEYORLOADINGTIMEOUT");
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeConveyorUnloadingTimeout(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeDocument(vehicleMessage, "RAIL-CONVEYORUNLOADINGTIMEOUT");
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeStart(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeDocument(vehicleMessage, "RAIL-VEHICLESTART");
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodeStop(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeDocument(vehicleMessage, "RAIL-VEHICLESTOP");
            this.tsAgent.Send(document);
        }

        public XmlDocument CreateMCodeDocument(VehicleMessageEx vehicleMessage, String messageName)
        {
            vehicleMessage.MessageName = messageName;
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateInformDocument(VehicleMessageEx vehicleMessage, String text, String description)
        {
            vehicleMessage.MessageName = "RAIL-INFORM";

            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode vehicleid = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfPriority, "");
            XmlNode texts = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfCarrierType, "");
            XmlNode descriptions = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfResultCode, "");


            vehicleid.InnerText = vehicleMessage.VehicleId;
            texts.InnerText = text;
            descriptions.InnerText = description;

            data.AppendChild(vehicleid);
            data.AppendChild(texts);
            data.AppendChild(descriptions);

            return document;
        }

        public void SendVehicleMessageMCodeMainboardVersion(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeVersionDocument(vehicleMessage, "RAIL-VEHICLEMAINBOARDVERSION");
            this.tsAgent.Send(document);
        }

        public void SendVehicleMessageMCodePLCVersion(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeVersionDocument(vehicleMessage, "RAIL-VEHICLEPLCVERSION");
            this.tsAgent.Send(document);
        }

        public UiTruncateMessageEx CreateUiTruncateMessage(XmlDocument document)
        {
            UiTruncateMessageEx uiTruncateMessage = new UiTruncateMessageEx();
            uiTruncateMessage.ReceivedMessage = document;

            uiTruncateMessage.MessageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");

            uiTruncateMessage.TableName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/TABLENAME");
            uiTruncateMessage.PartitionId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/PARTITIONID");

            SetHeaderInfoToMessage(document, uiTruncateMessage);
            SetOriginatedInfoToMessage(document, uiTruncateMessage);

            return uiTruncateMessage;
        }


        public ControlMessageEx CreateControlMessage(string messageName, string applicationName)
        {
            ControlMessageEx controlMessage = new ControlMessageEx();

            controlMessage.MessageName = messageName;
            controlMessage.ApplicationName = applicationName;

            return controlMessage;
        }

        //181213 WIFI RF ZIGBEE available
        public void SendMessageToAcsEsByWifiZigbee(IList listNio, XmlDocument document)
        {
            ACS.Core.Application.Model.Application application;

            if ((listNio != null) && (listNio.Count > 0))
            {
                //Wifi or Zigbee
                for (IEnumerator iterator = listNio.GetEnumerator(); iterator.MoveNext();)
                {
                    Nio nio = (Nio)iterator.Current;
                    string esName = nio.ApplicationName;
                    application = this.applicationManager.GetApplication(esName);
                    if ((application == null) || (application.State.Equals("inactive")))
                    {
                        //logger.Warn("Can not SendMessageToAcsEs(" + esName + ") is Not Active or not define application");
                    }
                    else
                    {
                        string destinationName = application.DestinationName;
                        if (!destinationName.StartsWith("/"))
                        {
                            destinationName = "/" + application.DestinationName;
                        }
                        String messageName = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/MESSAGENAME");
                        String transactionId = XmlUtility.GetDataFromXml(document, "/MESSAGE/HEADER/TRANSACTIONID");
                        String carrierName = "";
                        String transportCommandId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/COMMANDID");
                        String currentUnitName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/VEHICLEID");

                        String currentMachineName = "";
                        if (messageName.Equals("RAIL-VEHICLELGOCOMMAND", StringComparison.OrdinalIgnoreCase))
                        {
                            currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
                        }
                        else if (messageName.Equals("RAIL-CARRIERTRANSFER", StringComparison.OrdinalIgnoreCase))
                        {
                            currentMachineName = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/DESTNODEID");
                        }
                        this.esAgent.Send(document, destinationName, false, "");
                        //logger.Info(XmlUtils.toStringPrettyFormat(document), transactionId, messageName, carrierName, transportCommandId, currentMachineName, currentUnitName, messageName);
                    }
                }
            }
            else
            {
                //NG!!
                logger.Fatal("public void SendMessageToAcsEsByWifiZigbee(IList listNio, XmlDocument document): " + "listNio is Null or Empty");
            }
        }
    }

}
