using System;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Alarm.Model;
using ACS.Core.Message.Model;
using ACS.Core.Message.Model.Ui;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer.Model;
using ACS.Core.Transfer;
using ACS.Core.Material.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Application;
using ACS.Core.Material;
using System.Collections;
using System.Threading;
using ACS.Core.Message;
using System.Xml;
using ACS.Core.Logging;
using ACS.Core.Base;

namespace ACS.Service
{
    public class HostSecsInterfaceServiceEx : AbstractServiceEx
    {
        public Logger logger = Logger.GetLogger(typeof(HostSecsInterfaceServiceEx));
        //private VariableInfo variableInfo;
        //private SecsWriter secsWriter;
        //private SecsInterfaceManager secsInterfaceManager;ㅣ
        //private IMessageManagerEx messageManager;ㅣ
        //private ITransferManagerEx transferManager;
        //private IMaterialManagerEx materialManager;
        //private IResourceManagerEx resourceManager;
        //private WorkflowManager workflowManager;
        
        //private Secs secs;

        public VariableInfo VariableInfo { get; set; }

        public SecsWriter SecsWriter { get; set; }

        public SecsInterfaceManager SecsInterfaceManager { get; set; }

        public WorkflowManager WorkflowManager { get; set; }

        public Secs Secs { get; set; }
      
        public String GetMessageName(XmlDocument document)
        {

            String messageName = "";// XmlUtils.getXpathData(document, Message.XPATH_NAME_MESSAGENAME);
            logger.Info("messageName : " + messageName);
            return messageName;
        }

        public String GenerateCorrelation(Secs secs)
        {

            String correlation = "";//IdGeneratorUtils.randomUUID();
            logger.Info(secs.Name + ", correlation{" + correlation + "}");
            return correlation;
        }


        //S2F50_Ack
        public bool ReplyAGVHostCommand(Secs secs, TransferMessageEx transferMessage)
        {
            String name = secs.Name;
            String correlation = "";
            String rcmd = "";
            String jobId = "";
            String hack = "0";

            long systemByte;
            if(!long.TryParse(transferMessage.TransactionId, out systemByte))
            {
                systemByte = 0;
            }

            WriteResult writeResult = new WriteResult(); //this.secsWriter.replyAGVHostCommandAcknowledge(name, correlation, rcmd, jobId, hack, systemByte);
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("replyAGVHostCommand, " + transferMessage);
            }
            else
            {
                logger.Warn("failed to replyAGVHostCommand, " + transferMessage);
            }

            return true;

        }

        //S2F50_Ack
        public bool ReplyAGVHostCommandNak(Secs secs, TransferMessageEx transferMessage)
        {

            bool result = false;

            String name = Secs.Name;
            String correlation = "";
            String rcmd = "";
            String jobId = (transferMessage.TransportCommandId != null) ? transferMessage.TransportCommandId : "";
            String hack = "3";

            long systemByte;
            if (!long.TryParse(transferMessage.TransactionId, out systemByte))
            {
                systemByte = 0;
            }

            WriteResult writeResult = new WriteResult(); //this.secsWriter.replyAGVHostCommandAcknowledge(name, correlation, rcmd, jobId, hack, systemByte);
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("replyAGVHostCommandNak, " + transferMessage);
                result = true;
            }
            else
            {
                logger.Warn("failed to replyAGVHostCommandNak, " + transferMessage);
            }
            return result;
        }

        //S5F5
        public bool SendAGVAlarmSetEvent(VehicleMessageEx message)
        {
            String name = Secs.Name;
            String correlation = "";
            String eqpId = Secs.MachineName;
            String agvName = (message.VehicleId != null) ? message.VehicleId : "";
            String alst = "1";
            String alcd = "1";
            String alId = (message.ErrorId != null) ? message.ErrorId : "";
            String altx = (message.ErrorText != null) ? message.ErrorText : "";

            WriteResult writeResult = new WriteResult(); // this.secsWriter.sendAGVAlarmReport(name, correlation, eqpId, agvName, alst, alcd, alId, altx, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVAlarmSetEvent, " + message.CarrierId + ",  " + message.CarrierId);
            }
            else
            {
                logger.Warn("failed to sendAGVAlarmSetEvent, " + message.CarrierId + ",  " + message.CarrierId);
            }
            return true;
        }

        //S5F5
        public bool SendAGVAlarmClearEvent(VehicleMessageEx message)
        {
            String name = Secs.Name;
            String correlation = "";
            String eqpId = Secs.MachineName;
            String agvName = (message.VehicleId != null) ? message.VehicleId : "";
            String alst = "0";
            String alcd = "0";
            String alId = (message.ErrorId != null) ? message.ErrorId : "";
            String altx = (message.ErrorText != null) ? message.ErrorText : "";

            WriteResult writeResult = new WriteResult(); //this.secsWriter.sendAGVAlarmReport(name, correlation, eqpId, agvName, alst, alcd, alId, altx, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVAlarmClearEvent, " + message.CarrierId + ",  " + message.CarrierId);
            }
            else
            {
                logger.Warn("failed to sendAGVAlarmClearEvent, " + message.CarrierId + ",  " + message.CarrierId);
            }
            return true;
        }


        //S6F11CEID103
        public bool SendAGVStateEvent(VehicleMessageEx vehicleMessage)
        {
            String name = Secs.Name;
            String correlation = "";
            String dataId = "";
            String ceid = "103";
            String eqpId = Secs.MachineName;
            String crst = "";
            String agvName1 = "";
            String agvAvailabilityState1 = "";
            String agvInterlockState1 = "";
            String agvRechargeState1 = "";
            String agvMoveState1 = "";
            String agvRunState1 = "";
            String agvReasonCode1 = "";
            String agvDescription1 = "";
            String agvName2 = "";
            String agvAvailabilityState2 = "";
            String agvInterlockState2 = "";
            String agvRechargeState2 = "";
            String agvMoveState2 = "";
            String agvRunState2 = "";
            String agvReasonCode2 = "";
            String agvDescription2 = "";
            //ArrayList<AGVStatusEventReport_ALARMCOUNT> alarmList = new ArrayList();


            WriteResult writeResult = new WriteResult(); //this.secsWriter.sendAGVStatusEventReportSend(name, correlation, dataId, ceid, eqpId, crst, agvName1, agvAvailabilityState1, agvInterlockState1, agvRechargeState1, agvMoveState1, agvRunState1, agvReasonCode1, agvDescription1, agvName2, agvAvailabilityState2, agvInterlockState2, agvRechargeState2, agvMoveState2, agvRunState2, agvReasonCode2, agvDescription2, alarmList, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVStateEvent, " + vehicleMessage.VehicleId + ",  " + vehicleMessage.VehicleId);
            }
            else
            {
                logger.Error("failed to sendAGVStateEvent, " + vehicleMessage.VehicleId + ",  " + vehicleMessage.VehicleId);
            }
            return true;

        }

        //S6F11CEID311
        public bool SendAGVJobStartEvent(VehicleMessageEx vehicleMessage)
        {
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand == null)
                {
                    logger.Error("failed to sendAGVJobStartEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
                    return false;
                }
            }

            String name = Secs.Name;
            String correlation = "";
            String dataId = "";
            String ceid = "311";
            String eqpId = Secs.MachineName;
            String portId = "";
            String agvName = (vehicleMessage.VehicleId != null) ? vehicleMessage.VehicleId : "";
            String jobId = (transportCommand.Id != null) ? transportCommand.Id : "";
            String jobType = (transportCommand.JobType != null) ? transportCommand.JobType : "";
            String currentLoc = (vehicleMessage.Vehicle.CurrentNodeId != null) ? vehicleMessage.Vehicle.CurrentNodeId : "";
            String source = (transportCommand.Source != null) ? transportCommand.Source : "";
            String sourceLoc = "";
            String sourcePortId = "";
            if (!string.IsNullOrEmpty(source))
            {
                sourceLoc = source.Split(':')[0];
                sourcePortId = source.Split(':')[1];
            }

            String dest = (transportCommand.Dest != null) ? transportCommand.Dest : "";
            String finalLoc = "";
            String finalPortId = "";
            if (!string.IsNullOrEmpty(dest))
            {
                finalLoc = dest.Split(':')[0];
                finalPortId = dest.Split(':')[1];
            }
            String midLoc = "";
            String midPortId = "";
            String originLoc = "";
            String description = "";


            WriteResult writeResult = new WriteResult(); //this.secsWriter.sendAGVJobStartEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVJobStartEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            else
            {
                logger.Warn("failed to sendAGVJobStartEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            return true;
        }

        //S6F11CEID312
        public bool SendAGVJobCompleteEvent(VehicleMessageEx vehicleMessage)
        {
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand == null)
                {
                    logger.Error("failed to sendAGVJobCompleteEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
                    return false;
                }
            }
            String name = Secs.Name;
            String correlation = "";
            String dataId = "";
            String ceid = "312";
            String eqpId = Secs.MachineName;
            String portId = "";
            String agvName = (vehicleMessage.VehicleId != null) ? vehicleMessage.VehicleId : "";
            String jobId = (transportCommand.Id != null) ? transportCommand.Id : "";
            String jobType = (transportCommand.JobType != null) ? transportCommand.JobType : "";
            String currentLoc = vehicleMessage.NodeId;
            String source = (transportCommand.Source != null) ? transportCommand.Source : "";
            String sourceLoc = "";
            String sourcePortId = "";

            String[] sourceArr;
            String[] sourcePortIdArr;
            String[] destArr;
            String[] destPortIdArr;

            if (!string.IsNullOrEmpty(source))
            {
                sourceArr=source.Split(':');
                sourcePortIdArr = source.Split(':');

                sourceLoc = sourceArr[0];
                sourcePortId = sourcePortIdArr[1];
            }

            String dest = (transportCommand.Dest != null) ? transportCommand.Dest : "";
            String finalLoc = "";
            String finalPortId = "";
            if (!string.IsNullOrEmpty(dest))
            {
                destArr = dest.Split(':');
                destPortIdArr = dest.Split(':');
                finalLoc = destArr[0];
                finalPortId = destPortIdArr[1];
            }
            String midLoc = "";
            String midPortId = "";
            String originLoc = "";
            String description = "";

            WriteResult writeResult = new WriteResult(); //this.secsWriter.sendAGVJobCompleteEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVJobCompleteEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            else
            {
                logger.Error("failed to sendAGVJobCompleteEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            return true;

        }

        //S6F11CEID313
        public bool SendAGVJobCancelEvent(VehicleMessageEx vehicleMessage)
        {
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand == null)
                {
                    logger.Error("failed to sendAGVJobCancelEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
                    return false;
                }
            }

            String name = Secs.Name;
            String correlation = "";
            String dataId = "";
            String ceid = "313";
            String eqpId = Secs.MachineName;
            String portId = "";
            String agvName = (vehicleMessage.VehicleId != null) ? vehicleMessage.VehicleId : "";
            String jobId = (transportCommand.Id != null) ? transportCommand.Id : "";
            String jobType = (transportCommand.JobType != null) ? transportCommand.JobType : "";
            String currentLoc = (vehicleMessage.Vehicle.CurrentNodeId != null) ? vehicleMessage.Vehicle.CurrentNodeId : "";
            String source = (transportCommand.Source != null) ? transportCommand.Source : "";
            String sourceLoc = "";
            String sourcePortId = "";

            String[] sourceArr;
            String[] sourcePortIdArr;
            String[] destArr;
            String[] destPortIdArr;

            if (!string.IsNullOrEmpty(source))
            {
                sourceArr =  source.Split(':');
                sourcePortIdArr=source.Split(':');

                sourceLoc =sourceArr[0];
                sourcePortId = sourcePortIdArr[1];
            }

            String dest = (transportCommand.Dest != null) ? transportCommand.Dest : "";
            String finalLoc = "";
            String finalPortId = "";
            if (!string.IsNullOrEmpty(dest))
            {
                destArr = source.Split(':');
                destPortIdArr = source.Split(':');
                finalLoc = destArr[0];
                finalPortId = destPortIdArr[1];
            }
            String midLoc = "";
            String midPortId = "";
            String originLoc = "";
            String description = "";


            WriteResult writeResult = new WriteResult(); //this.secsWriter.sendAGVJobCancelEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVJobCancelEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            else
            {
                logger.Error("failed to sendAGVJobCancelEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            return true;

        }

        //S6F11CEID318
        public bool SendAGVLocationEvent(VehicleMessageEx vehicleMessage)
        {
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            String jobId;
            String jobType;
            String source;
            String dest;

            if (transportCommand == null)
            {
                jobId = "";
                jobType = "";
                source = "";
                dest = "";
            }
            else
            {
                jobId = (transportCommand.Id != null) ? transportCommand.Id : "";
                jobType = (transportCommand.JobType != null) ? transportCommand.JobType : "";
                source = (transportCommand.Source != null) ? transportCommand.Source : "";
                dest = (transportCommand.Dest != null) ? transportCommand.Dest : "";
            }

            String name = Secs.Name;
            String correlation = "";
            String dataId = "";
            String ceid = "318";
            String eqpId = Secs.MachineName;
            String portId = "";
            String agvName = (vehicleMessage.VehicleId != null) ? vehicleMessage.VehicleId : "";
            //String jobId = (transportCommand.Id != null) ? transportCommand.Id : "";
            //String jobType = (transportCommand.JobType != null) ? transportCommand.JobType : "";
            //String currentLoc = (vehicleMessage.Vehicle.CurrentNodeId != null) ? vehicleMessage.Vehicle.CurrentNodeId : "";
            String currentLoc = vehicleMessage.NodeId;
            //String source = (transportCommand.Source != null) ? transportCommand.Source : "";
            String sourceLoc = "";
            String sourcePortId = "";

            String[] sourceArr;
            String[] sourcePortIdArr;
            String[] destArr;
            String[] destPortIdArr;

            if (!string.IsNullOrEmpty(source))
            {
                sourceArr = source.Split(':');
                sourcePortIdArr = sourcePortId.Split(':');
                
                sourceLoc = sourceArr[0];
                sourcePortId = sourcePortIdArr[1];
            }

            //String dest = (transportCommand.Dest != null) ? transportCommand.Dest : "";
            String finalLoc = "";
            String finalPortId = "";

       
            if (!string.IsNullOrEmpty(dest))
            {
                destArr = source.Split(':');
                destPortIdArr = sourcePortId.Split(':');

                finalLoc = destArr[0];
                finalPortId = destPortIdArr[1];
            }
            String midLoc = "";
            String midPortId = "";
            String originLoc = "";
            String description = "";


            WriteResult writeResult = new WriteResult(); //this.secsWriter.sendAGVLocationEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
            if (writeResult.getResultCode() == 0)
            {
                logger.Info("sendAGVLocationEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            else
            {
                logger.Error("failed to sendAGVLocationEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
            }
            return true;

        }

        ////
        ////	//S2F50
        //public bool replyAGVHostCommand(VehicleMessage message)
        //{
        //    String name = "";
        //    String correlation = "";
        //    String rcmd = "";
        //    String jobId = "";
        //    String hack = "";

        //    long systemByte = 0;//long.Parse(VehicleMessage.TransactionId);

        //    WriteResult writeResult = this.secsWriter.replyAGVHostCommandAcknowledge(name, correlation, rcmd, jobId, hack, systemByte);
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("replyAGVHostCommand, " + message);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to replyAGVHostCommand, " + message);
        //    }

        //    return true;

        //}

        ////S5F5
        //public bool sendAGVAlarmEvent(VehicleMessage message)
        //{
        //    String name = "";
        //    String correlation = "";
        //    String eqpId = "";
        //    String agvName = "";
        //    String alst = "";
        //    String alcd = "";
        //    String alId = "";
        //    String altx = "";

        //    WriteResult writeResult = this.secsWriter.sendAGVAlarmReport(name, correlation, eqpId, agvName, alst, alcd, alId, altx, new Properties());
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    return true;
        //}

        ////S6F11CEID103
        //public bool sendAGVStateEvent(VehicleMessage message)
        //{
        //    String name = "";
        //    String correlation = "";
        //    String dataId = "";
        //    String ceid = "";
        //    String eqpId = "";
        //    String crst = "";
        //    String agvName1 = "";
        //    String agvAvailabilityState1 = "";
        //    String agvInterlockState1 = "";
        //    String agvRechargeState1 = "";
        //    String agvMoveState1 = "";
        //    String agvRunState1 = "";
        //    String agvReasonCode1 = "";
        //    String agvDescription1 = "";
        //    String agvName2 = "";
        //    String agvAvailabilityState2 = "";
        //    String agvInterlockState2 = "";
        //    String agvRechargeState2 = "";
        //    String agvMoveState2 = "";
        //    String agvRunState2 = "";
        //    String agvReasonCode2 = "";
        //    String agvDescription2 = "";
        //    List alarmList = new ArrayList();


        //    WriteResult writeResult = this.secsWriter.sendAGVStatusEventReportSend(name, correlation, dataId, ceid, eqpId, crst, agvName1, agvAvailabilityState1, agvInterlockState1, agvRechargeState1, agvMoveState1, agvRunState1, agvReasonCode1, agvDescription1, agvName2, agvAvailabilityState2, agvInterlockState2, agvRechargeState2, agvMoveState2, agvRunState2, agvReasonCode2, agvDescription2, alarmList, new Properties());
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    return true;

        //}

        ////S6F11CEID311
        //public bool sendAGVJobStartEvent(VehicleMessage message)
        //{
        //    String name = "";
        //    String correlation = "";
        //    String dataId = "";
        //    String ceid = "";
        //    String eqpId = "";
        //    String portId = "";
        //    String agvName = "";
        //    String jobId = "";
        //    String jobType = "";
        //    String currentLoc = "";
        //    String sourceLoc = "";
        //    String sourcePortId = "";
        //    String finalLoc = "";
        //    String finalPortId = "";
        //    String midLoc = "";
        //    String midPortId = "";
        //    String originLoc = "";
        //    String description = "";


        //    WriteResult writeResult = this.secsWriter.sendAGVJobStartEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    return true;
        //}

        ////S6F11CEID312
        //public bool sendAGVJobCompleteEvent(VehicleMessage vehicleMessage)
        //{
        //    String name = secs.Name;
        //    String correlation = "";
        //    String dataId = "";
        //    String ceid = "312";
        //    String eqpId = secs.MachineName;
        //    String portId = Integer.toString(secs.getHsmsPort());
        //    String agvName = vehicleMessage.VehicleId;

        //    TransportCommand transportCommand = vehicleMessage.TransportCommand;
        //    String jobId = transportCommand.Id;
        //    String jobType = "";
        //    String currentLoc = transportCommand.Dest;
        //    String source = transportCommand.Source;
        //    String sourceLoc = source.Split(':')[0];
        //    String sourcePortId = source.Split(':')[1];
        //    String dest = transportCommand.Dest;
        //    String finalLoc = dest.Split(':')[0];
        //    String finalPortId = dest.Split(':')[1];
        //    String midLoc = "";
        //    String midPortId = "";
        //    String originLoc = "";
        //    String description = "";


        //    WriteResult writeResult = this.secsWriter.sendAGVJobCompleteEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("sendAGVAlarmEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to sendAGVAlarmEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
        //    }
        //    return true;

        //}

        ////S6F11CEID313
        //public bool sendAGVJobCancelEvent(VehicleMessage message)
        //{
        //    String name = "";
        //    String correlation = "";
        //    String dataId = "";
        //    String ceid = "";
        //    String eqpId = "";
        //    String portId = "";
        //    String agvName = "";
        //    String jobId = "";
        //    String jobType = "";
        //    String currentLoc = "";
        //    String sourceLoc = "";
        //    String sourcePortId = "";
        //    String finalLoc = "";
        //    String finalPortId = "";
        //    String midLoc = "";
        //    String midPortId = "";
        //    String originLoc = "";
        //    String description = "";


        //    WriteResult writeResult = this.secsWriter.sendAGVJobCancelEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to sendAGVAlarmEvent, " + message.CarrierId + ",  " + message.CarrierId);
        //    }
        //    return true;

        //}

        ////S6F11CEID318
        //public bool sendAGVLocationEvent(VehicleMessage vehicleMessage)
        //{
        //    String name = secs.Name;
        //    String correlation = "";
        //    String dataId = "";
        //    String ceid = "318";
        //    String eqpId = "";
        //    String portId = "";
        //    String agvName = "";
        //    String jobId = "";
        //    String jobType = "";
        //    String currentLoc = "";
        //    String sourceLoc = "";
        //    String sourcePortId = "";
        //    String finalLoc = "";
        //    String finalPortId = "";
        //    String midLoc = "";
        //    String midPortId = "";
        //    String originLoc = "";
        //    String description = "";


        //    WriteResult writeResult = this.secsWriter.sendAGVLocationEventReportSend(name, correlation, dataId, ceid, eqpId, portId, agvName, jobId, jobType, currentLoc, sourceLoc, sourcePortId, finalLoc, finalPortId, midLoc, midPortId, originLoc, description, new Properties());
        //    if (writeResult.getResultCode() == 0)
        //    {
        //        logger.Info("sendAGVAlarmEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
        //    }
        //    else
        //    {
        //        logger.Warn("failed to sendAGVAlarmEvent, " + vehicleMessage.CarrierId + ",  " + vehicleMessage.CarrierId);
        //    }
        //    return true;

        //}
    }

    public class Secs
    {
        public string Name
        {
            get;
            set;
        }

        internal string MachineName
        {
            get;
            set;
        }
    }

    public class VariableInfo
    {
    }

    public class SecsWriter
    {
    }

    public class SecsInterfaceManager
    {
    }

    public class WorkflowManager
    {
    }

    class WriteResult
    {
        public int getResultCode()
        {
            return 0;
        }
    }


}