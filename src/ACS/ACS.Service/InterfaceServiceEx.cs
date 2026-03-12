using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Message.Model.Control;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Path.Model;
using ACS.Framework.Message.Model.Host;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using ACS.Utility;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Xml.XPath;
using log4net;
using System.Net.NetworkInformation;
using System.Configuration;
using ACS.Extension.Framework.Base;
using ACS.Extension.Manager;

namespace ACS.Service
{
    public class InterfaceServiceEx : AbstractServiceEx
    {
        public override void Init()
        {
            base.Init();
        }

        public bool CheckCarrierByTransportCommand(TransferMessageEx transferMessage)
        {

            return this.MessageManager.ValidateAndPopulateCarrierByTransportCommand(transferMessage);

        }

        public bool CheckCarrier(TransferMessageEx transferMessage)
        {

            return this.MessageManager.ValidateAndPopulateCarrier(transferMessage);
        }


        public bool CheckVehicle(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle = vehicleMessage.Vehicle;
            if (vehicle != null)
                return true;

            String vehicleId = vehicleMessage.VehicleId;
            vehicle = this.ResourceManager.GetVehicle(vehicleId);
            if (vehicle != null)
                return true;
            else
                return false;
        }

        public bool CheckNio(ReceivePacket receivePacket, Nio nio)
        {
            logger.Info("Receive Packet AGV ID is [" + receivePacket.SendId + "], The registered AGV name is [" + nio.getName() + "(" + nio.RemoteIp + ":" + nio.Port + ")].");

            if (nio.MachineName.Equals("WIFI"))
            {
                if (receivePacket.SendId.Equals(nio.getName(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public bool CheckTransportCommand(TransferMessageEx transferMessage)
        {
            bool result = false;
            result = this.MessageManager.ValidateTransportCommand(transferMessage);
            return result;
        }

        public bool CheckTransportCommand(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            result = this.MessageManager.ValidateTransportCommand(vehicleMessage.TransportCommandId);

            if (!result)
            {
                logger.Info("transportCommand does not exist in the message, trying to find transportCommand using vehicle{" + vehicleMessage + "}");

                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message");
                    vehicleMessage.TransportCommandId = transportCommand.Id;
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {
                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }
            else
            {
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message");
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {
                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }

            return result;
        }

        public bool CheckTransportCommandState(VehicleMessageEx vehicleMessage, string state)
        {
            bool result = false;
            TransportCommandEx transportCommand = null;
            result = this.MessageManager.ValidateTransportCommand(vehicleMessage.TransportCommandId);

            if (!result)
            {
                logger.Info("transportCommand does not exist in the message, trying to find transportCommand using vehicle{" + vehicleMessage + "}");

                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message");
                    vehicleMessage.TransportCommandId = transportCommand.Id;
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {
                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message");
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;

                    result = true;
                }
                else
                {
                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }

            if (transportCommand != null)
            {
                if (!transportCommand.State.Equals(state))
                {
                    result = false;
                }
                else
                {
                    result = true;
                }
            }

            return result;
        }

        public TransferMessageEx CreateTransferMessage(XmlDocument document)
        {
            return this.MessageManager.CreateTransferMessage(document);
        }

        public TransferMessageEx CreateTransferCommandMessage(UiTransportMessageEx uiMessage)
        {
            return this.MessageManager.CreateTransferMessage(uiMessage);
        }

        public TransferMessageEx CreateTransferCancelMessage(UiTransportCancelMessageEx uiMessage)
        {
            return this.MessageManager.CreateTransferMessage(uiMessage);
        }

        public TransferMessageEx CreateTransferDeleteMessage(UiTransportDeleteMessageEx uiMessage)
        {
            return this.MessageManager.CreateTransferMessage(uiMessage);
        }

        public VehicleMessageEx CreateVehicleMessage(ReceivePacket receivePacket)
        {
            return this.MessageManager.CreateVehicleMessage(receivePacket);
        }

        public VehicleMessageEx CreateVehicleMessageFromES(XmlDocument document)
        {
            return this.MessageManager.CreateVehicleMessageFromES(document);
        }

        public VehicleMessageEx CreateVehicleMessageFromTrans(XmlDocument document)
        {
            return this.MessageManager.CreateVehicleMessageFromTrans(document);
        }

        public VehicleMessageEx CreateVehicleMessageFromDaemon(XmlDocument document)
        {
            return this.MessageManager.CreateVehicleMessageFromDaemon(document);
        }

        //public AlarmEx createAlarmMessage(XmlDocument document) {
        //
        //    return this.MessageManager.CreateTransferMessage(document);
        //}

        public string GetMessageName(AbstractMessage abstractMessage)
        {
            return abstractMessage.MessageName;
        }

        public string GetRCodeType(VehicleMessageEx vehicleMessage)
        {
            string rCodeType = vehicleMessage.KeyData.Substring(1, 1);
            return rCodeType;
        }

        public String GetMCodeType(VehicleMessageEx vehicleMessage)
        {
            string mCodeType = vehicleMessage.KeyData.Substring(0, 2);
            return mCodeType;
        }

        public string GetTCodeType(VehicleMessageEx vehicleMessage)
        {
            string tCodeType = vehicleMessage.KeyData.Substring(1, 1);
            return tCodeType;
        }

        public bool CheckGarbageNode(VehicleMessageEx vehicleMessage)
        {
            int node = 0;
            string getNode = vehicleMessage.NodeId;
            bool result = false;


            if (!string.IsNullOrEmpty(getNode) && (getNode.Equals("0201") || getNode.Equals("0202") || getNode.Equals("0209")))
            {
                return result;
            }

            if (Int32.TryParse(getNode, out node))
            {
                result = true;
            }

            return result;
        }

        public bool CheckTransferMessage(TransferMessageEx transferMessage)
        {

            if (string.IsNullOrEmpty(transferMessage.TransportCommandId))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_IDEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_IDEMPTY.Item2;
                logger.Warn("transportCommandId does not exist in message, " + transferMessage);
                return false;
            }
            if (string.IsNullOrEmpty(transferMessage.CarrierId))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_CARRIER_NAMEEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_CARRIER_NAMEEMPTY.Item2;
                logger.Warn("carrierId does not exist in message, " + transferMessage);
                return false;
            }
            if (string.IsNullOrEmpty(transferMessage.SourceMachine))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_SOURCEMACHINE_NAMEEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_SOURCEMACHINE_NAMEEMPTY.Item2;
                logger.Warn("sourceMachine does not exist in message, " + transferMessage);
                return false;
            }
            if (string.IsNullOrEmpty(transferMessage.SourceUnit))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_SOURCEUNIT_NAMEEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_SOURCEUNIT_NAMEEMPTY.Item2;
                logger.Warn("sourceUnit does not exist in message, " + transferMessage);
                return false;
            }
            if (string.IsNullOrEmpty(transferMessage.DestMachine))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_DESTMACHINE_NAMEEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_DESTMACHINE_NAMEEMPTY.Item2;
                logger.Warn("destMachine does not exist in message, " + transferMessage);
                return false;
            }
            if (string.IsNullOrEmpty(transferMessage.DestUnit))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_DESTUNIT_NAMEEMPTY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_DESTUNIT_NAMEEMPTY.Item2;
                logger.Warn("destUnit does not exist in message, " + transferMessage);
                return false;
            }

            return true;
        }

        public bool CheckTransferCommandSourceDest(TransferMessageEx transferMessage)
        {

            String srcMachineId = transferMessage.SourceMachine;
            String srcPortId = transferMessage.SourceUnit;
            String destMachineId = transferMessage.DestMachine;
            String destPortId = transferMessage.DestUnit;

            String sourceLocationId = srcMachineId + ":" + srcPortId;
            String destLocationId = destMachineId + ":" + destPortId;

            if (sourceLocationId.Equals(destLocationId))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_SOURCEDESTMACHINE_DUPLICATE.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_SOURCEDESTMACHINE_DUPLICATE.Item2;
                logger.Warn("Same Source, Dest Machine, " + transferMessage);
                return false;
            }


            if (!this.ResourceManager.CheckLocation(sourceLocationId))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_SOURCEMACHINE_NOTFOUND.Item2;
                logger.Warn("sourceLocation does not exist in DB, " + transferMessage);
                return false;
            }
            if (!this.ResourceManager.CheckLocation(destLocationId))
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2;
                logger.Warn("destLocation does not exist in DB, " + transferMessage);
                return false;
            }

            return true;
        }

        public bool CheckTransferCommand(TransferMessageEx transferMessage)
        {

            if (this.TransferManager.ExistTransportCommand(transferMessage.TransportCommandId))
            {
                return true;
            }
            return false;
        }

        public bool CheckTransferCommand(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            result = this.MessageManager.ValidateTransportCommand(vehicleMessage.TransportCommandId);

            if (!result)
            {
                logger.Info("transportCommand does not exist in the message, trying to find transportCommand using vehicle{" + vehicleMessage.VehicleId + "}");

                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message", vehicleMessage);
                    vehicleMessage.TransportCommandId = transportCommand.Id;
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {

                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage, vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }
            else
            {
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message", vehicleMessage);

                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {
                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage, vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }

            return result;
        }

        public String GetSCodeType(VehicleMessageEx vehicleMessage)
        {

            String result = "";
            // 1. Run : VehicleDepated
            // 2. stop & full & station=source : AcquireCompeted&CarrierInstalled
            // 3. stop & empty & station=dest : depositCompeleted&CarrierRemoved&TransferCompleted
            String runState = vehicleMessage.RunState;
            if (runState.Equals(VehicleMessageEx.RUNSTATE_RUN))
            {

                result = VehicleMessageEx.MESSAGE_TYPE_VEHICLEDEPARTED;
            }
            else if (runState.Equals(VehicleMessageEx.RUNSTATE_STOP))
            {

                VehicleEx vehicle = vehicleMessage.Vehicle;
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(vehicle.TransportCommandId);
                if (transportCommand != null)
                {

                    String currentNodeId = vehicle.CurrentNodeId;
                    LocationEx location = this.PathManager.GetLocationByPortId(transportCommand.Source);
                    if (location != null)
                    {

                        StationEx station = this.ResourceManager.GetStation(location.StationId);
                        String occupied = vehicleMessage.Occupied;
                        if (occupied.Equals(VehicleMessageEx.TRUE))
                        {

                            NodeEx sourceNode = this.PathManager.SearchNodeByStationAsSource(station);
                            if (currentNodeId.Equals(sourceNode.Id))
                            {
                                result = VehicleMessageEx.MESSAGE_TYPE_ACQUIRECOMPLETED;
                            }
                        }
                        else if (occupied.Equals(VehicleMessageEx.FALSE))
                        {

                            NodeEx destNode = this.PathManager.SearchNodeByStationAsDest(station);
                            if (currentNodeId.Equals(destNode.Id))
                            {
                                result = VehicleMessageEx.MESSAGE_TYPE_TRANSFERCOMPLETED;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public bool CheckAlarm(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            result = this.MessageManager.ValidateTransportCommand(vehicleMessage.TransportCommandId);

            if (!result)
            {
                logger.Info("transportCommand does not exist in the message, trying to find transportCommand using vehicle{" + vehicleMessage.VehicleId + "}");

                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message");
                    vehicleMessage.TransportCommandId = transportCommand.Id;
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {

                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }
            else
            {
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                if (transportCommand != null)
                {
                    logger.Info("transportCommandId{" + transportCommand.Id + "} will be set in the message");
                    vehicleMessage.TransportCommand = transportCommand;
                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                    result = true;
                }
                else
                {

                    logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                    vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item1;
                    vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTFOUND.Item2;
                    result = false;
                }
            }

            return result;
        }

        public void SendVehicleMessageCCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageCCode(messageName, vehicleMessage);
        }

        public void SendVehicleMessageTCodeEnter(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageTCodeEnter(messageName, vehicleMessage);
        }

        public void SendVehicleMessageTCodePermission(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageTCodePermission(messageName, vehicleMessage);
        }

        public void SendVehicleMessageSCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageSCode(messageName, vehicleMessage);
        }

        public void SendVehicleMessageRCodeVoltage(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageRCodeVoltage(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeChargeStart(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeChargeStart(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeChargeComplete(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeChargeComplete(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeDestOccupied(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeDestOccupied(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeSourceEmpty(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeSourceEmpty(messageName, vehicleMessage);
        }
        public void SendVehicleMessageMCodeVehicleEmpty(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeVehicleEmpty(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeVehicleOccupied(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeVehicleOccupied(messageName, vehicleMessage);
        }
        public void SendVehicleMessageMCodeAgvChargingFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvChargingFail(messageName, vehicleMessage);
        }
        public void SendVehicleMessageMCodeRecoverMissMagTag(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoverMissMagTag(messageName, vehicleMessage);
        }
        public void SendVehicleMessageMCodeRecoverMissMagTagFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoverMissMagTagFail(messageName, vehicleMessage);
        }
        public void SendVehicleMessageMCodeRecoverMissMagTagSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoverMissMagTagSuccess(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeRecoverAgvOutRail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoverAgvOutRail(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeRecoverAgvOutRailSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoverAgvOutRailSuccess(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeRecoverAgvOutRailFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoverAgvOutRailFail(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvAutoStart(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvAutoStart(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvAutoStartSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvAutoStartSuccess(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvAutoStartFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvAutoStartFail(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvTurnPbsOff(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvTurnPbsOff(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvTurnPbsOffSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvTurnPbsOffSuccess(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvTurnPbsOffFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvTurnPbsOffFail(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeRecoveryAgvBack(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoveryAgvBack(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeRecoveryAgvBackSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoveryAgvBackSuccess(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeRecoveryAgvBackFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeRecoveryAgvBackFail(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvSensorSonic(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvSensorSonic(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvSensorSonicSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvSensorSonicSuccess(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeAgvSensorSonicFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeAgvSensorSonicFail(messageName, vehicleMessage);
        }

        public void SendVehicleMessageMCodeHmiVersion(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeHmiVersion(messageName, vehicleMessage);
        }

        public void SendVehicleMessageRCodeCapacity(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageRCodeCapacity(messageName, vehicleMessage);
        }

        public void SendVehicleMessageOCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageOCode(messageName, vehicleMessage);
        }

        public void SendVehicleMessageLCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageLCode(messageName, vehicleMessage);
        }

        public void SendVehicleMessageUCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageUCode(messageName, vehicleMessage);
        }

        public void SendVehicleMessageECode(String messageName, VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageECode(messageName, vehicleMessage);
        }



        public void SendTransportMessageSource(TransferMessageEx transferMessage)
        {
            this.MessageManager.SendTransportCommandSource(transferMessage);
        }

        public void SendTransportMessageSource(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendTransportCommandSource(vehicleMessage);
        }

        public void SendTransportMessageDest(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendTransportCommandDest(vehicleMessage);
        }

        public void SendGoWaitpoint(VehicleMessageEx vehicleMessage)
        {
            //Task task = new Task(new Action(() =>
            //{
            //    int retryCount = 3;
            //    int retryDelay = 3000;
            //    int curCount = 0;

            //    while (curCount != retryCount)
            //    {            
            //        this.MessageManager.SendTransportCommandWaitpoint(vehicleMessage);
            //        curCount++;
            //        Thread.Sleep(retryDelay);
            //    }

            //    logger.Debug("Send Waitpoint task completed.");
            //    return;
            //}));

            //task.Start();  
            this.MessageManager.SendTransportCommandWaitpoint(vehicleMessage);
        }

        public void SendGoWaitpoint0000(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendTransportCommandWaitpoint0000(vehicleMessage);
        }
        //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
        public void SendGoWaitpoint0000_RGV(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendTransportCommandWaitpoint0000_RGV(vehicleMessage);
        }
        public void SendGoWaitpoint0000(TransferMessageEx transferMessage)
        {
            this.MessageManager.SendTransportCommandWaitpoint0000(transferMessage);
        }
        public void SendGoWaitpoint(TransferMessageEx transferMessage)
        {
            this.MessageManager.SendTransportCommandWaitpoint(transferMessage);
        }

        public void SendTransportMessageVehicleDestNode(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendTransportCommandVehicleDestNode(vehicleMessage);
        }

        public void SendMoveMessageTarget(UiMoveVehicleMessageEx uiMessage)
        {
            this.MessageManager.SendMoveCommandTarget(uiMessage);
        }

        public void SendVehiclePermitMessage(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehiclePermitCommand(vehicleMessage);
        }

        public void SendOtherSideVehiclePermitMessage(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendOtherVehiclePermitCommand(vehicleMessage);
        }

        public void SendBatteryVoltageReqMessage(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendBatteryVoltageReq(vehicleMessage);
        }

        public void SendBatteryCapacityReqMessage(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendBatteryCapacityReq(vehicleMessage);
        }

        public void ReportAlarmReport(VehicleMessageEx vehicleMessage)
        {
#if V3
            #region V3
           //E CODE
           /*		 <Msg>
                     <Command>TRSALARMREPORT</Command>
                     <Header>
                       <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
                       <ReplySubject>/BN1/BNL2/ACS/SERVER/BMTCM02</ReplySubject>
                       <AppName>B1ACS02F_A01</AppName>
                       <ProcName>B1ACS02F_A01</ProcName>
                       <SeqNo>21429403#0004</SeqNo>
                     </Header>
                     <DataLayer>
                       <ACSName>B1ACS02F_A01</ACSName>
                       <TRSSet>
                         <TRS>
                           <TRSName>13</TRSName>
                           <AlarmID>281</AlarmID>
                           <AlarmState>2</AlarmState>
                           <AlarmLevel>2</AlarmLevel>
                           <AlarmText>Front Detect Sensor Error</AlarmText>
                         </TRS>
                       </TRSSet>
                     </DataLayer>
                   </Msg>
           */
           String messageName = "TRSALARMREPORT";

           try
           {
               if (this.HostMessageManager.UseSend(messageName))
               {

                   XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                   if (sendingMessage != null)
                   {
                       XmlNode ParentNode = sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet");
                       XmlElement alarmSet = sendingMessage.CreateElement("TRS");

                       XmlNode TRSName = sendingMessage.CreateElement("TRSName");
                       XmlNode AlarmID = sendingMessage.CreateElement("AlarmID");
                       XmlNode AlarmState = sendingMessage.CreateElement("AlarmState");
                       XmlNode AlarmLevel = sendingMessage.CreateElement("AlarmLevel");
                       XmlNode AlarmText = sendingMessage.CreateElement("AlarmText");


                       TRSName.InnerText = vehicleMessage.VehicleId;
                       AlarmID.InnerText = vehicleMessage.ErrorId;
                       AlarmState.InnerText = AlarmSpecEx.STATE_ALARM_SET;
                       AlarmLevel.InnerText = vehicleMessage.ErrorLevel;
                       AlarmText.InnerText = vehicleMessage.ErrorText;

                       alarmSet.AppendChild(TRSName);
                       alarmSet.AppendChild(AlarmID);
                       alarmSet.AppendChild(AlarmState);
                       alarmSet.AppendChild(AlarmLevel);
                       alarmSet.AppendChild(AlarmText);

                       ParentNode.AppendChild(alarmSet);

                       vehicleMessage.SendingMessage = sendingMessage;
                       this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                   }

               }
               else
               {
                   logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
               }
           }
           catch (Exception e)
           {
               logger.Warn("failed to report send{" + messageName + "}", e);
           }
            #endregion V3
#else

            #region V2
            String messageName = "TRSSTATEREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    if (sendingMessage != null)
                    {
                        string AvailableState = "";
                        string ReasonComment = "";

                        VehicleEx vehicle = vehicleMessage.Vehicle;
                        if (vehicle == null)
                        {
                            vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                            vehicleMessage.Vehicle = vehicle;
                        }
                        if (vehicle == null)
                        {
                            return;
                        }

                        if (vehicle.ConnectionState.Equals(VehicleEx.CONNECTIONSTATE_DISCONNECT))
                        {
                            AvailableState = "DOWN";
                            ReasonComment = VehicleEx.CONNECTIONSTATE_DISCONNECT;
                        }
                        else if (vehicle.State.Equals(VehicleEx.STATE_BANNED))
                        {
                            AvailableState = "DOWN";
                            ReasonComment = VehicleEx.STATE_BANNED;
                        }
                        else if (vehicle.Installed.Equals(VehicleEx.INSTALL_REMOVE))
                        {
                            AvailableState = "DOWN";
                            ReasonComment = VehicleEx.INSTALL_REMOVE;
                        }
                        else if (vehicle.ProcessingState.Equals(VehicleEx.PROCESSINGSTATE_CHARGE))
                        {
                            AvailableState = "DOWN";
                            ReasonComment = VehicleEx.PROCESSINGSTATE_CHARGE;
                        }

                        sendingMessageTemp = sendingMessage;

                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/TRSName", vehicleMessage.VehicleId);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AvailableState", AvailableState);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/MoveState", vehicleMessage.RunState);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/RechargeState", "");
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/JobID", vehicleMessage.TransportCommandId);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/CurLoc", vehicleMessage.NodeId);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/ReasonCode", "");
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/ReasonComment", ReasonComment);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmID", vehicleMessage.ErrorId);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmState", AlarmSpecEx.STATE_ALARM_SET);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmLevel", vehicleMessage.ErrorLevel);
                        XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmText", vehicleMessage.ErrorText);

                        ////180928 ACS→ADS Report XML Format Pair
                        //sendingMessage.PreserveWhitespace = true;
                        //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");

                        //180928 ACS→ADS Report XML Format Pair
                        sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                        //sendingMessage.PreserveWhitespace = true;

                        //TEST 한줄 -> 띄어쓰기
                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                        settings.NewLineChars = Environment.NewLine;
                        settings.NewLineHandling = NewLineHandling.Replace;

                        StringBuilder sb = new StringBuilder();
                        XmlWriter writer = XmlWriter.Create(sb, settings);

                        sendingMessage.Save(writer);
                        sendingMessageTemp.PreserveWhitespace = true;
                        sendingMessageTemp.InnerXml = sb.ToString();

                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
            #endregion V2

#endif
        }

        public void ReportAlarmClearReport(VehicleMessageEx vehicleMessage)
        {
#if V3
            #region V3 TRSALARMREPORT
            //  //    E CODE
            //  //    /*		 <Msg>
            //  //              <Command>TRSALARMREPORT</Command>
            //  //              <Header>
            //  //                <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //  //                <ReplySubject>/BN1/BNL2/ACS/SERVER/BMTCM02</ReplySubject>
            //  //                <AppName>B1ACS02F_A01</AppName>
            //  //                <ProcName>B1ACS02F_A01</ProcName>
            //  //                <SeqNo>21429403#0004</SeqNo>
            //  //              </Header>
            //  //              <DataLayer>
            //  //                <ACSName>B1ACS02F_A01</ACSName>
            //  //                <TRSSet>
            //  //                  <TRS>
            //  //                    <TRSName>13</TRSName>
            //  //                    <AlarmID>281</AlarmID>
            //  //                    <AlarmState>2</AlarmState>
            //  //                    <AlarmLevel>2</AlarmLevel>
            //  //                    <AlarmText>Front Detect Sensor Error</AlarmText>
            //  //                  </TRS>
            //  //                  <TRS>
            //  //                    <TRSName>13</TRSName>
            //  //                    <AlarmID>281</AlarmID>
            //  //                    <AlarmState>2</AlarmState>
            //  //                    <AlarmLevel>2</AlarmLevel>
            //  //                    <AlarmText>Front Detect Sensor Error</AlarmText>
            //  //                  </TRS>
            //  //                </TRSSet>
            //  //              </DataLayer>
            //  //            </Msg>
            //  //    */
        
            #endregion V3 TRSALARMREPORT
            String messageName = "TRSALARMREPORT";
            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);

                    if (sendingMessage != null)
                    {
                     
                        //XElement alarmSet = XmlUtils.getXmlElement(sendingMessage, "/Msg/DataLayer/TRSSet");
                        // XmlElement alarmSet = (XmlElement)sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet");
                        XmlNode ParentNode = sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet");
                       // XmlElement alarmSet = sendingMessage.DocumentElement["TRS"];
                        // XmlElement alarmSet = sendingMessage.CreateElement("TRS");

                        IList alarmList = this.AlarmManager.GetAlarmsByVehicleId(vehicleMessage.VehicleId);

                        if (alarmList != null)
                        {
                            for (IEnumerator iterator = alarmList.GetEnumerator(); iterator.MoveNext(); )
                            {
                                AlarmEx alarm = (AlarmEx)iterator.Current;
                                XmlElement alarmSet = sendingMessage.CreateElement("TRS");
                                AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarm.AlarmId);
                               
                                
            #region V3
                                XmlNode TRSName = sendingMessage.CreateElement("TRSName");
                                XmlNode AlarmID = sendingMessage.CreateElement("AlarmID");
                                XmlNode AlarmState = sendingMessage.CreateElement("AlarmState");
                                XmlNode AlarmLevel = sendingMessage.CreateElement("AlarmLevel");
                                XmlNode AlarmText = sendingMessage.CreateElement("AlarmText");

                                TRSName.InnerText = vehicleMessage.VehicleId;
                                AlarmID.InnerText = alarm.AlarmId;
                                AlarmState.InnerText = AlarmSpecEx.STATE_ALARM_CLEARED;
                                AlarmLevel.InnerText = ((alarmSpec != null) ? alarmSpec.Severity : AlarmSpecEx.SEVERITY_LIGHT);
                                AlarmText.InnerText = alarm.AlarmText;

                                alarmSet.AppendChild(TRSName);
                                alarmSet.AppendChild(AlarmID);
                                alarmSet.AppendChild(AlarmState);
                                alarmSet.AppendChild(AlarmLevel);
                                alarmSet.AppendChild(AlarmText);
            #endregion V3

                                ParentNode.AppendChild(alarmSet);
                            }
                            //ParentNode.AppendChild(alarmSet);
                          
                            vehicleMessage.SendingMessage = sendingMessage;
                            this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                        }
                    }

                }
                else
                {
                    logger.Info ("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }


#else
            #region V2
            #region V2 TRSSTATEREPORT
            //   <Msg>	
            //      <Command>TRSSTATEREPORT</Command>	
            //      <Header>	
            //          <DestSubject>/BN1/BNL2/MOS/CLIENT/ADSmgr</DestSubject>	
            //          <ReplySubject>/BN1/BNL2/ACS/FACTORY2/ACSmgr4</ReplySubject>	
            //          <AppName></AppName>	
            //          <ProcName></ProcName>	
            //          <SeqNo></SeqNo>	
            //      </Header>	
            //      <DataLayer>	
            //          <EqpID>LC</EqpID>	
            //          <TRSSet>	
            //            <TRS>	
            //              <TRSName>45</TRSName>	
            //              <AvailableState>UP</AvailableState>	
            //              <MoveState></MoveState>	
            //              <RechargeState></RechargeState>	
            //              <JobID>B2MTT59_LD01_IP01_180701011903_A</JobID>	
            //              <CurLoc>123</CurLoc>	
            //              <ReasonCode></ReasonCode>	
            //              <ReasonComment></ReasonComment>	
            //              <AlarmID>E0281</AlarmID>	
            //              <AlarmState>Set</AlarmState>	
            //              <AlarmLevel>Heavy</AlarmLevel>	
            //              <AlarmText>OBS Abnormal</AlarmText>	
            //            </TRS>	
            //          </TRSSet>	
            //      </DataLayer>	
            //</Msg>	
            #endregion V2 TRSSTATEREPORT
            String messageName = "TRSSTATEREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    if (sendingMessage != null)
                    {
                        IList alarmList = this.AlarmManager.GetAlarmsByVehicleId(vehicleMessage.VehicleId);

                        if (alarmList.Count > 0)
                        {
                            foreach (AlarmEx alarm in alarmList)
                            {
                                XmlElement alarmSet = sendingMessage.CreateElement("TRS");
                                XmlNode ParentNode = sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet");
                                AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarm.AlarmId);
                                string AvailableState = "";
                                string ReasonComment = "";

                                VehicleEx vehicle = vehicleMessage.Vehicle;
                                if (vehicle == null)
                                {
                                    vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                                    vehicleMessage.Vehicle = vehicle;
                                }
                                if (vehicle == null)
                                {
                                    return;
                                }

                                if (vehicle.ConnectionState.Equals(VehicleEx.CONNECTIONSTATE_DISCONNECT))
                                {
                                    AvailableState = "DOWN";
                                    ReasonComment = VehicleEx.CONNECTIONSTATE_DISCONNECT;
                                }
                                else if (vehicle.State.Equals(VehicleEx.STATE_BANNED))
                                {
                                    AvailableState = "DOWN";
                                    ReasonComment = VehicleEx.STATE_BANNED;
                                }
                                else if (vehicle.Installed.Equals(VehicleEx.INSTALL_REMOVE))
                                {
                                    AvailableState = "DOWN";
                                    ReasonComment = VehicleEx.INSTALL_REMOVE;
                                }
                                else if (vehicle.ProcessingState.Equals(VehicleEx.PROCESSINGSTATE_CHARGE))
                                {
                                    AvailableState = "DOWN";
                                    ReasonComment = VehicleEx.PROCESSINGSTATE_CHARGE;
                                }

                                sendingMessageTemp = sendingMessage;

                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/TRSName", vehicleMessage.VehicleId);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AvailableState", AvailableState);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/MoveState", vehicleMessage.RunState);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/RechargeState", "");
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/JobID", vehicleMessage.TransportCommandId);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/CurLoc", vehicleMessage.NodeId);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/ReasonCode", "");
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/ReasonComment", ReasonComment);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmID", alarm.AlarmId);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmState", AlarmSpecEx.STATE_ALARM_CLEARED);
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmLevel", ((alarmSpec != null) ? alarmSpec.Severity : AlarmSpecEx.SEVERITY_LIGHT));
                                XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmText", alarm.AlarmText);

                            }

                            ////180928 ACS→ADS Report XML Format Pair
                            //sendingMessage.PreserveWhitespace = true;
                            //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");

                            //180928 ACS→ADS Report XML Format Pair
                            sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                            //sendingMessage.PreserveWhitespace = true;

                            //TEST 한줄 -> 띄어쓰기
                            XmlWriterSettings settings = new XmlWriterSettings();
                            settings.Indent = true;
                            settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                            settings.NewLineChars = Environment.NewLine;
                            settings.NewLineHandling = NewLineHandling.Replace;

                            StringBuilder sb = new StringBuilder();
                            XmlWriter writer = XmlWriter.Create(sb, settings);

                            sendingMessage.Save(writer);
                            sendingMessageTemp.PreserveWhitespace = true;
                            sendingMessageTemp.InnerXml = sb.ToString();
                            //
                            
                            vehicleMessage.SendingMessage = sendingMessageTemp;
                            this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                        }
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
            #endregion V2


#endif
        }

        public void ReportAGVLocation(VehicleMessageEx vehicleMessage)
        {
            //    //T CODE
            //    /*<Msg>
            //     <Command>TRSMOVEREPORT</Command>
            //     <Header>
            //       <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //       <ReplySubject>/BN1/BNL2/ACS/SERVER/BMTCM02</ReplySubject>
            //       <AppName>B1ACS02F_A01</AppName>
            //       <ProcName>B1ACS02F_A01</ProcName>
            //       <SeqNo>53704935#0004</SeqNo>
            //     </Header>
            //     <DataLayer>
            //       <ACSName>B1ACS02F_A01</ACSName>
            //       <TRSSet>
            //         <TRS>
            //           <TRSName>24</TRSName>
            //           <JobID>H015426_527</JobID>
            //           <MatType></MatType>
            //           <CurLoc>4137</CurLoc>
            //           <SourceLoc>B1BND35N_G11</SourceLoc>
            //           <FinalLoc>B1STK02F_A01</FinalLoc>
            //           <RouteLoc></RouteLoc>
            //           <OriginLoc></OriginLoc>
            //           <Qty></Qty>
            //         </TRS>
            //       </TRSSet>
            //     </DataLayer>
            //   </Msg>*/
            String messageName = "TRSMOVEREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //data insert
                    String jobId = vehicleMessage.TransportCommandId;
                    String matType = "";
                    String currntLoc = vehicleMessage.NodeId;
                    String srcLoc = "";
                    String finalLoc = "";
                    String routeLoc = "";
                    String originLoc = "";

                    sendingMessageTemp = sendingMessage;

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/JobID", jobId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/MatType", matType);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/CurLoc", currntLoc);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/SourceLoc", srcLoc);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/FinalLoc", finalLoc);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/RouteLoc", routeLoc);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/OriginLoc", originLoc);

                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/JobID").InnerText = jobId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/MatType").InnerText = matType;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/CurLoc").InnerText = currntLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/SourceLoc").InnerText = srcLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/FinalLoc").InnerText = finalLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/RouteLoc").InnerText = routeLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/TRSSet/TRS/OriginLoc").InnerText = originLoc;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }
        public XmlDocument SetXmlData(XmlDocument sendingMessage, string SelectSingleNode, string setValue)
        {
            XmlNode setXmlData = sendingMessage.SelectSingleNode(SelectSingleNode);
            setXmlData.InnerText = setValue;

            return sendingMessage;
        }
        //AGV Arrived Source Port
        public void ReportAGVArrivedSourcePort(VehicleMessageEx vehicleMessage)
        {
            //         //ARRIVED
            //         /*<Msg>
            //          <Command>TRSJOBREPORT</Command>
            //          <Header>
            //               <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //               <ReplySubject>/BN1/BNL2/MOS/SERVER/BMTCM01</ReplySubject>
            //             </Header>
            //             <DataLayer>
            //                    <ACSName>B1ACS01F_A01</ACSName>
            //                    <EqpID></EqpID>
            //                    <PortID></PortID>
            //                    <Type>ARRIVED</Type>
            //                    <ErrCode></ErrCode>
            //                    <ErrMsg></ErrMsg>
            //                    <TRSName>66</TRSName>
            //                    <JobID>H233221_576</JobID>
            //                    <MatType></MatType>
            //                    <CurLoc>7111</CurLoc>
            //                    <FinalLoc>B1STK01F_A01</FinalLoc>
            //              </DataLayer>
            //        </Msg>
            //*/
            String messageName = "TRSJOBREPORT";

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return;
            }
            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_SOURCE_ARRIVED);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = transportCommand.Id;
                    //KSB
                    //string carrID = "";                    
                    string carrID = transportCommand.CarrierId;
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = transportCommand.Priority.ToString();
                    string userId = "";
                    string description = transportCommand.Description;
                    string errorCode = "0";

                    VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.Vehicle.Id);
                    string transEQP = "";
                    string currentLoc = vehicle.CurrentNodeId;
                    if (!String.IsNullOrEmpty(vehicle.BayId))
                    {
                        transEQP = vehicle.BayId.Split('_')[0];
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CurrentLoc", currentLoc);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);

                    //180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        //AGV Arrived Dest Port
        public void ReportAGVArrivedDestPort(VehicleMessageEx vehicleMessage)
        {
            //         //ARRIVED
            //         /*<Msg>
            //          <Command>TRSJOBREPORT</Command>
            //          <Header>
            //               <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //               <ReplySubject>/BN1/BNL2/MOS/SERVER/BMTCM01</ReplySubject>
            //             </Header>
            //             <DataLayer>
            //                    <ACSName>B1ACS01F_A01</ACSName>
            //                    <EqpID></EqpID>
            //                    <PortID></PortID>
            //                    <Type>ARRIVED</Type>
            //                    <ErrCode></ErrCode>
            //                    <ErrMsg></ErrMsg>
            //                    <TRSName>66</TRSName>
            //                    <JobID>H233221_576</JobID>
            //                    <MatType></MatType>
            //                    <CurLoc>7111</CurLoc>
            //                    <FinalLoc>B1STK01F_A01</FinalLoc>
            //              </DataLayer>
            //        </Msg>
            //*/
            String messageName = "TRSJOBREPORT";

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return;
            }
            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_DEST_ARRIVED);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = transportCommand.Id;
                    //KSB
                    //string carrID = vehicleMessage.CarrierId;
                    string carrID = transportCommand.CarrierId;
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string userId = "";
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = "0";

                    VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.Vehicle.Id);
                    string transEQP = "";
                    string currentLoc = vehicle.CurrentNodeId;
                    if (!String.IsNullOrEmpty(vehicle.BayId))
                    {
                        transEQP = vehicle.BayId.Split('_')[0];
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    //KSB
                    //XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", "");
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CurrentLoc", currentLoc);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReportAGVState(VehicleMessageEx vehicleMessage)
        {
            //    //T CODE
            //    /* <Msg>
            //         <Command>TRSSTATEREPORT</Command>
            //         <Header>
            //           <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //           <ReplySubject>/BN1/BNL2/ACS/SERVER/BMTCM02</ReplySubject>
            //           <AppName>B1ACS02F_A01</AppName>    <ProcName>B1ACS02F_A01</ProcName>
            //           <SeqNo>04629009#0004</SeqNo>
            //         </Header>
            //         <DataLayer>
            //           <ACSName>B1ACS02F_A01</ACSName>
            //           <ACSState></ACSState>
            //           <Type></Type>
            //           <TRSSet>
            //             <TRS>
            //               <TRSName>53</TRSName>
            //               <AvailableState>UP</AvailableState>
            //               <InterlockState>OFF</InterlockState>
            //               <MoveState>RUNNING</MoveState>
            //               <RechargeState>NORMAL</RechargeState>
            //               <RunState>RUN</RunState>
            //               <ReasonCode></ReasonCode>
            //               <ReasonComment>0</ReasonComment>
            //               <AlarmID></AlarmID>
            //               <AlarmState></AlarmState>
            //               <AlarmLevel></AlarmLevel>
            //               <AlarmText></AlarmText>
            //             </TRS>
            //           </TRSSet>
            //         </DataLayer>
            //       */
            String messageName = "TRSSTATEREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    string trsName = vehicleMessage.VehicleId;
                    string availableState = "UP";
                    string moveState = vehicleMessage.RunState;
                    string rechargeState = "";
                    string jobId = vehicleMessage.TransportCommand.Id;
                    string reasonCode = "";
                    string reasonComment = "";
                    string alarmId = "";
                    string alarmState = "";
                    string alarmLevel = "";
                    string alarmText = "";

                    VehicleEx vehicle = vehicleMessage.Vehicle;
                    if (vehicle == null)
                    {
                        vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        vehicleMessage.Vehicle = vehicle;
                    }
                    if (vehicle == null)
                    {
                        return;
                    }

                    if (vehicle.ConnectionState.Equals(VehicleEx.CONNECTIONSTATE_DISCONNECT))
                    {
                        availableState = "DOWN";
                        reasonComment = VehicleEx.CONNECTIONSTATE_DISCONNECT;
                    }
                    else if (vehicle.State.Equals(VehicleEx.STATE_BANNED))
                    {
                        availableState = "DOWN";
                        reasonComment = VehicleEx.STATE_BANNED;
                    }
                    else if (vehicle.Installed.Equals(VehicleEx.INSTALL_REMOVE))
                    {
                        availableState = "DOWN";
                        reasonComment = VehicleEx.INSTALL_REMOVE;
                    }
                    else if (vehicle.ProcessingState.Equals(VehicleEx.PROCESSINGSTATE_CHARGE))
                    {
                        availableState = "DOWN";
                        reasonComment = VehicleEx.PROCESSINGSTATE_CHARGE;
                    }

                    AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(vehicleMessage.VehicleId);
                    if (alarm != null)
                    {
                        alarmId = alarm.AlarmId;
                        alarmText = alarm.AlarmText;
                        alarmState = "SET";
                        alarmLevel = this.AlarmManager.GetAlarmSpecByAlarmId(alarmId).Severity;
                    }

                    sendingMessageTemp = sendingMessage;

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/TRSName", trsName);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AvailableState", availableState);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/MoveState", moveState);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/RechargeState", rechargeState);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/ResonCode", reasonCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/ResonComment", reasonComment);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmID", alarmId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmState", alarmState);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmLevel", alarmLevel);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TRSSet/TRS/AlarmText", alarmText);

                    ////180928 ACS→ADS Report XML Format Pair
                    ////sendingMessage.PreserveWhitespace = true;
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReportAGVUnloadComplete(VehicleMessageEx vehicleMessage)
        {
            //         //U CODE 
            //         //COMPLETE
            //         /*<Msg>
            //          <Command>TRSJOBREPORT</Command>
            //          <Header>
            //               <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //               <ReplySubject>/BN1/BNL2/MOS/SERVER/BMTCM01</ReplySubject>
            //             </Header>
            //             <DataLayer>
            //                    <ACSName>B1ACS01F_A01</ACSName>
            //                    <EqpID></EqpID>
            //                    <PortID></PortID>
            //                    <Type>START/COMPLETE/CANCEL</Type>
            //                    <ErrCode></ErrCode>
            //                    <ErrMsg></ErrMsg>
            //                    <TRSName>66</TRSName>
            //                    <JobID>H233221_576</JobID>
            //                    <MatType></MatType>
            //                    <CurLoc>7111</CurLoc>
            //                    <FinalLoc>B1STK01F_A01</FinalLoc>
            //              </DataLayer>
            //        </Msg>
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_COMPLETE);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = vehicleMessage.TransportCommandId;
                    string carrID = vehicleMessage.CarrierId;
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string userId = "";
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = "0";

                    string transEQP = "";

                    if (!String.IsNullOrEmpty(vehicleMessage.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    //KSB
                    //XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", "");
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}
                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReportAGVloadComplete(VehicleMessageEx vehicleMessage)
        {
            //         //L CODE
            //         //MES 梨�姨붿찉梨섓옙�뇫�겂占쎌찓姨띿콠��夷섏괜姨붿찉梨쀬쮬夷붿괜姨붿찉梨숋옙�듌�뀚占쎌쭬�깘梨�姨붿찉 梨뷂옙�떌占쎌솙�긾�뿈�뵻�긾�룧�긿占쎌찓姨띿콠夷띿㎟梨�姨붿찉梨숋옙�뭼占쏙옙
            //         //PICKUPED
            //         /*<Msg>
            //          <Command>TRSJOBREPORT</Command>
            //          <Header>
            //               <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //               <ReplySubject>/BN1/BNL2/MOS/SERVER/BMTCM01</ReplySubject>
            //             </Header>
            //             <DataLayer>
            //                    <ACSName>B1ACS01F_A01</ACSName>
            //                    <EqpID></EqpID>
            //                    <PortID></PortID>
            //                    <Type>PICKUPED</Type>
            //                    <ErrCode></ErrCode>
            //                    <ErrMsg></ErrMsg>
            //                    <TRSName>66</TRSName>
            //                    <JobID>H233221_576</JobID>
            //                    <MatType></MatType>
            //                    <CurLoc>7111</CurLoc>
            //                    <FinalLoc>B1STK01F_A01</FinalLoc>
            //              </DataLayer>
            //        </Msg>
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_PICKUP);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = vehicleMessage.TransportCommandId;
                    string carrID = vehicleMessage.CarrierId;
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string userId = "";
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = "0";

                    string transEQP = "";
                    string currentLoc = "";
                    if (!String.IsNullOrEmpty(vehicleMessage.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                        currentLoc = vehicle.CurrentNodeId;
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    //KSB
                    //XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", "");
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CurrentLoc", currentLoc);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void requestAGVJobCancel(VehicleMessageEx vehicleMessage)
        {
            //m-code 05~08 : abnormal transportcmd case
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_CANCEL);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = vehicleMessage.TransportCommandId;
                    //KSB
                    //string carrID = vehicleMessage.CarrierId;
                    string carrID = vehicleMessage.TransportCommand.CarrierId;

                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string userId = "";
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = vehicleMessage.ResultCode;
                    string errorMsg = vehicleMessage.Cause;
                    string transEQP = "";

                    if (!String.IsNullOrEmpty(vehicleMessage.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    //KSB
                    //XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", "");
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }


                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }

        }

        public void requestAGVJobCancel(TransferMessageEx transferMessage)
        {
            //Host JOBCANCEL
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_CANCEL);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_CANCEL);
                    sendingMessageTemp = sendingMessage;

                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;
                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = AGVJobReport.JOBCANCEL_ERRCODE_HOSTCANCEL;
                    string processId = transferMessage.ProcessId;
                    string productId = transferMessage.ProductId;

                    //181001 ADS Error Commant is "ADS_CANCEL"
                    string errorMsg = "ADS_CANCEL";
                    //string errorMsg = transferMessage.Cause;

                    string transEQP = "";

                    transEQP = XmlUtility.GetDataFromXml((XmlDocument)transferMessage.ReceivedMessage, "/Msg/DataLayer/TransEQP");

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    //KSB
                    //XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", "");
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProcessID", processId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProductID", productId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    transferMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }

        }

        public void RequestAGVJobCancelAsDestOccupied(VehicleMessageEx vehicleMessage)
        {
            //m-code 05~08 : abnormal transportcmd case
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_CANCEL);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = vehicleMessage.TransportCommandId;
                    string carrID = vehicleMessage.TransportCommand.CarrierId;
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string userId = "";
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = vehicleMessage.ResultCode;
                    string errorMsg = vehicleMessage.Cause;
                    string transEQP = "";

                    if (!String.IsNullOrEmpty(vehicleMessage.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    //KSB
                    //XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", "");
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }

        }

        public void ReportAGVReplyCommand(VehicleMessageEx vehicleMessage)
        {
            //C_CODE_REP
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_JOBSTART);
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string jobId = vehicleMessage.TransportCommandId;
                    string carrID = vehicleMessage.CarrierId;
                    
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string userId = "";
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = vehicleMessage.ResultCode;
                    string errorMsg = vehicleMessage.Cause;

                    string transEQP = "";
                    string currentLoc = "";
                    if (!String.IsNullOrEmpty(vehicleMessage.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                        currentLoc = vehicle.CurrentNodeId;
                    }

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CurrentLoc", currentLoc);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReportAGVArrivedReplyCommand(VehicleMessageEx vehicleMessage)
        {
            //C_CODE_REP
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_ARRIVED);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/Type").InnerText = TransportCommandEx.TYPE_ARRIVED;
                    sendingMessageTemp = sendingMessage;

                    //data insert
                    //				String appName = "";
                    String eqpId = "";
                    String portId = "";
                    String jobId = vehicleMessage.TransportCommandId;
                    String matType = "";
                    String curLoc = "";
                    if (vehicleMessage.Vehicle != null)
                    {
                        curLoc = vehicleMessage.Vehicle.CurrentNodeId;
                    }
                    String finalLoc = "";
                    if (vehicleMessage.TransportCommand != null)
                    {
                        finalLoc = vehicleMessage.TransportCommand.Dest;
                    }
                    String resultCode = "0";

                    //				//XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/ACSName", appName);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/EqpID", eqpId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/PortID", portId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/ErrCode", resultCode);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    ////XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TrayID", carrierId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/MatType", matType);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/CurLoc", curLoc);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/FinalLoc", finalLoc);

                    sendingMessage.SelectSingleNode("/Msg/DataLayer/EqpID").InnerText = eqpId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/PortID").InnerText = portId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/ErrCode").InnerText = resultCode;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/JobID").InnerText = jobId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/MatType").InnerText = matType;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CurLoc").InnerText = curLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/FinalLoc").InnerText = finalLoc;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = (sendingMessage);
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));

                    //}

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveCmd(TransferMessageEx transferMessage)
        {
            //         //MOVECMD_REP
            //         //START
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                     <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                      < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                      < ProcessID > MSP380 </ ProcessID >
            //                      < ProductID > AMB585NE01 - 036 </ ProductID >
            //                      < StepID > MD370 </ StepID >
            //                      < Quantity > 240 </ Quantity >
            //                      < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //             < TransUnit ></ TransUnit > --AGV Name
            //             < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //              <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //sendingMessage.PreserveWhitespace = true;

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_CMDREPLY);
                    sendingMessageTemp = sendingMessage;

                    string dest = ConfigurationManager.AppSettings["HibernateConnectionString"];
                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    //string transUnit = transferMessage.VehicleId; // 181021 ADS 요청으로 아무 값도 올리지 않음.
                    string transUnit = string.Empty;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;

                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = "0";

                    //181001
                    string processId = transferMessage.ProcessId;
                    string productId = transferMessage.ProductId;
                    //181020
                    string stepID = transferMessage.StepId;

                    string transEQP = transferMessage.EqpId;

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);

                    //181001
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProcessID", processId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProductID", productId);
                    //181020
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/StepID", stepID);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    ////sendingMessage.PreserveWhitespace = true;
                    //sendingMessage.PreserveWhitespace = false;

                    //if (sendingMessage != null)
                    //{
                    //    transferMessage.SendingMessage = (sendingMessage);
                    //    this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveCmd(VehicleMessageEx vehicleMessage)
        {
            //         //MOVECMD_REP
            //         //START
            //         /*<Msg>
            //          <Command>TRSJOBREPORT</Command>
            //          <Header>
            //               <DestSubject>/BN1/BNY1/MOS/CLIENT/MHSmgr</DestSubject>
            //               <ReplySubject>/BN1/BNL2/MOS/SERVER/BMTCM01</ReplySubject>
            //             </Header>
            //             <DataLayer>
            //                    <ACSName>B1ACS01F_A01</ACSName>
            //                    <EqpID></EqpID>
            //                    <PortID></PortID>
            //                    <Type>START/COMPLETE/CANCEL</Type>
            //                    <ErrCode></ErrCode>
            //                    <ErrMsg></ErrMsg>
            //                    <TRSName>66</TRSName>
            //                    <JobID>H233221_576</JobID>
            //                    <MatType></MatType>
            //                    <CurLoc>7111</CurLoc>
            //                    <FinalLoc>B1STK01F_A01</FinalLoc>
            //              </DataLayer>
            //        </Msg>
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/Type").InnerText = TransportCommandEx.TYPE_RECEIVE;
                    sendingMessageTemp = sendingMessage;

                    //data insert
                    //				String appName = "";
                    String eqpId = "";
                    String portId = "";
                    String jobId = vehicleMessage.TransportCommandId;
                    String matType = "";
                    String curLoc = vehicleMessage.NodeId;
                    String finalLoc = vehicleMessage.TransportCommand.Dest;
                    String resultCode = "0";

                    //				//XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/ACSName", appName);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/EqpID", eqpId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/PortID", portId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/ErrCode", resultCode);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    ////XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/TrayID", carrierId);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/MatType", matType);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/CurLoc", curLoc);
                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/FinalLoc", finalLoc);

                    sendingMessage.SelectSingleNode("/Msg/DataLayer/EqpID").InnerText = eqpId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/PortID").InnerText = portId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/ErrCode").InnerText = resultCode;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/JobID").InnerText = jobId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/MatType").InnerText = matType;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CurLoc").InnerText = curLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/FinalLoc").InnerText = finalLoc;

                    //if (sendingMessage != null)
                    //{
                    //    vehicleMessage.SendingMessage = sendingMessage;
                    //    this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    //}

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + vehicleMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveCmdNak3(TransferMessageEx transferMessage)
        {
            //         //MOVECMD_REP Nak
            //         //CALCEL
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                     <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                      < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                      < ProcessID > MSP380 </ ProcessID >
            //                      < ProductID > AMB585NE01 - 036 </ ProductID >
            //                      < StepID > MD370 </ StepID >
            //                      < Quantity > 240 </ Quantity >
            //                      < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //             < TransUnit ></ TransUnit > --AGV Name
            //             < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //              <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_CMDREPLY);
                    sendingMessageTemp = sendingMessage;

                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;
                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = (!string.IsNullOrEmpty(transferMessage.ReplyCode)) ? transferMessage.ReplyCode : "03";
                    string resultMessage = transferMessage.Cause;

                    string curLoc = sourceEQP + ":" + sourceUnit;
                    string finalLoc = finalEQP + ":" + finalUnit;

                    string transEQP = transferMessage.EqpId;

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", resultMessage);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    ////sendingMessage.PreserveWhitespace = true;
                    //sendingMessage.PreserveWhitespace = false;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                        //logger.Info("send{" + messageName + "} Nak 3" + transferMessage.ToString());

                        // 2017.10.24 keyhan added
                        String bayId = "";

                        if (string.IsNullOrEmpty(resultMessage) && resultMessage.Equals("COMMANDALREADYREQUESTED"))
                        {
                            //SJP 1013
                            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(jobId);
                            if (transportCommand != null)
                            {
                                bayId = transportCommand.BayId;
                            }
                        }
                        else if (string.IsNullOrEmpty(resultMessage) && resultMessage.Equals("CARRIERHASANOTHERTRANSPORTJOB"))
                        {
                            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByCarrierId(carrID);
                            if (transportCommand != null)
                            {
                                bayId = transportCommand.BayId;
                            }
                        }
                        if (jobId.StartsWith("U"))
                        {
                            //20190830 ksb
                            this.HistoryManager.CreateTransportCommandHistory(jobId, carrID, curLoc, finalLoc, TransportCommandEx.JOBTYPE_ACSMOVE, "", transferMessage.Cause, bayId);
                        }
                        else
                        {
                            this.HistoryManager.CreateTransportCommandHistory(jobId, carrID, curLoc, finalLoc, TransportCommandEx.JOBTYPE_AUTOCALL, "", transferMessage.Cause, bayId);
                        }

                        if (this.ResourceManager.GetLocation(curLoc) != null && this.ResourceManager.GetLocation(finalLoc) != null)
                        {
                            String source = this.ResourceManager.GetLocation(curLoc).StationId;
                            this.CreateInformAutoCallFail(resultMessage, source, jobId);
                        }
                        else
                        {
                            this.CreateInformAutoCallFail(resultMessage, " ", jobId);
                        }

                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveCmdNak5(TransferMessageEx transferMessage)
        {
            //         //MOVECMD_REP Nak
            //         //CALCEL
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                     <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                      < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                      < ProcessID > MSP380 </ ProcessID >
            //                      < ProductID > AMB585NE01 - 036 </ ProductID >
            //                      < StepID > MD370 </ StepID >
            //                      < Quantity > 240 </ Quantity >
            //                      < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //             < TransUnit ></ TransUnit > --AGV Name
            //             < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //              <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", TransportCommandEx.TYPE_CMDREPLY);
                    sendingMessageTemp = sendingMessage;

                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;
                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = (!string.IsNullOrEmpty(transferMessage.ReplyCode)) ? transferMessage.ReplyCode : "05";
                    string resultMessage = transferMessage.Cause;

                    string curLoc = sourceEQP + ":" + sourceUnit;
                    string finalLoc = finalEQP + ":" + finalUnit;

                    string transEQP = transferMessage.EqpId;

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", resultMessage);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                        //logger.Info("send{" + messageName + "} Nak 5" + transferMessage.ToString());
                        if (jobId.StartsWith("U"))
                        {
                            this.HistoryManager.CreateTransportCommandHistory(jobId, carrID, curLoc, finalLoc, TransportCommandEx.JOBTYPE_ACSMOVE, "", transferMessage.Cause);
                        }
                        else
                        {
                            this.HistoryManager.CreateTransportCommandHistory(jobId, carrID, curLoc, finalLoc, TransportCommandEx.JOBTYPE_AUTOCALL, "", transferMessage.Cause);
                        }

                        if (this.ResourceManager.GetLocation(curLoc) != null && this.ResourceManager.GetLocation(finalLoc) != null)
                        {
                            String source = this.ResourceManager.GetLocation(curLoc).StationId;
                            this.CreateInformAutoCallFail(resultMessage, source, jobId);
                        }
                        else
                        {
                            this.CreateInformAutoCallFail(resultMessage, " ", jobId);
                        }
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveCmdByResultCode(TransferMessageEx transferMessage, int result)
        {

            String messageName = "TRSJOBREPORT";
            String cmdType = "";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    if (result == 99)
                    {
                        cmdType = TransportCommandEx.TYPE_CANCEL;
                    }

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CmdType", cmdType);
                    sendingMessageTemp = sendingMessage;

                    string TransportCommandId;
                    if (transferMessage.TransportCommandId != null)
                    {
                        TransportCommandId = transferMessage.TransportCommandId.Trim();
                        //20181012 KSG
                        //ADS CANCEL NOT ACCEPT CAUSE '_M'
                        //TransportCommandId = TransportCommandId.Substring(0, TransportCommandId.Length - 1);
                        //TransportCommandId += 'M';
                    }
                    else
                    {
                        TransportCommandId = transferMessage.TransportCommandId;
                    }

                    string jobId = TransportCommandId;
                    //KSB
                    //string carrID = "";
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;
                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = result.ToString();
                    string resultMessage = transferMessage.Cause;

                    string curLoc = sourceEQP + ":" + sourceUnit;
                    string finalLoc = finalEQP + ":" + finalUnit;

                    string transEQP = "";

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    VehicleEx vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", resultMessage);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                        logger.Info("send{" + messageName + "} Nak " + errorCode + ", " + transferMessage.ToString());
                    }

                }
                else
                {
                    logger.Info("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ConvertEventMessage(String eventMessageName, AbstractMessage eventMessage)
        {

            if (this.HostMessageManager.UseSend(eventMessageName))
            {
                Object sendingMessage = this.HostMessageManager.Convert(eventMessageName, eventMessage);
                eventMessage.SendingMessage = (sendingMessage);
            }
            else
            {
                logger.Info("send{" + eventMessageName + "} not used", eventMessage);
            }
        }

        public void ConvertRequestMessage(String requestMessageName, AbstractMessage requestMessage)
        {

            if (this.HostMessageManager.UseSend(requestMessageName))
            {

                Object sendingMessage = this.HostMessageManager.Convert(requestMessageName, requestMessage);
                requestMessage.SendingMessage = (sendingMessage);

            }
            else
            {
                logger.Info("send{" + requestMessageName + "} not used", requestMessage);
            }
        }

        public void SendMessageToHost(String eventMessageName, AbstractMessage eventMessage)
        {

            if (this.HostMessageManager.UseSend(eventMessageName))
            {
                if (eventMessage.SendingMessage != null)
                {
                    this.HostMessageManager.SendMessageToHost(eventMessage, this.HostMessageManager.GetSendHostMessageName(eventMessageName));
                }
            }
            else
            {
                logger.Info("send{" + eventMessageName + "} not used", eventMessage);
            }
        }

        public Object RequestToHost(String requestMessageName, BaseMessage requestMessage)
        {

            try
            {
                this.ConvertRequestMessage(requestMessageName, requestMessage);
                return this.SendRequestMessageToHost(requestMessageName, requestMessage);
            }
            catch (Exception e)
            {
                logger.Warn("failed to request{" + requestMessageName + "}", e);
                return null;
            }
        }

        public Object SendRequestMessageToHost(String requestMessageName, AbstractMessage requestMessage)
        {

            Object replyMessage = null;

            if (requestMessage.SendingMessage != null)
            {
                replyMessage = this.HostMessageManager.RequestMessageToHost(requestMessage, this.HostMessageManager.GetSendHostMessageName(requestMessageName));
            }
            else
            {
                logger.Info("failed to convert request{" + requestMessageName + "}, please check hostMessageConverter", requestMessage);
            }

            return replyMessage;
        }



        // UI
        public UiTransportMessageEx CreateUiTransportMessage(XmlDocument XmlDocument)
        {
            return this.MessageManager.CreateUiTransportMessage(XmlDocument);
        }

        public UiTransportCancelMessageEx CreateUiTransportCancelMessage(XmlDocument XmlDocument)
        {
            return this.MessageManager.CreateUiTransportCancelMessage(XmlDocument);
        }

        public UiTransportDeleteMessageEx CreateUiTransportDeleteMessage(XmlDocument XmlDocument)
        {
            return this.MessageManager.CreateUiTransportDeleteMessage(XmlDocument);
        }

        public UiMoveVehicleMessageEx CreateUiMoveVehicleMessage(XmlDocument XmlDocument)
        {
            return this.MessageManager.CreateUiMoveVehicleMessage(XmlDocument);
        }

        public void ReplyMessageToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyMessageToUi(abstractMessage);
        }

        //REP Transport case by case
        public void ReplyTransportMessageValidationNGToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyTransportMessageValidationNGToUi(abstractMessage);
        }

        public void ReplyTransportStationValidationNGToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyTransportStationValidationNGToUi(abstractMessage);
        }

        public void ReplyTransportExistJobNGToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyTransportExistJobNGToUi(abstractMessage);
        }

        public void ReplyTransportCarrierCreateNGToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyTransportCarrierCreateNGToUi(abstractMessage);
        }

        public void ReplyTransportJobCreateNGToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyTransportJobCreateNGToUi(abstractMessage);
        }

        public void ReplyTransportPathValidationNGToUi(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyTransportPathValidationNGToUi(abstractMessage);
        }

        public void CyclePingTest(Nio nio)
        {

            //		String ip = "107.125.122.235";
            String ip = nio.RemoteIp;
            String pingResult = "";
            String pingCommand = "ping " + ip + " -t";
            logger.Info("start cycle ping : " + nio);
            try
            {

                while (true)
                {
                    //ACS.Utility.SystemUtility.PerformCommand()
                    //Runtime runTime = Runtime.getRuntime();

                    //Process process = runTime.exec(pingCommand);

                    //StreamReader _in = new StreamReader(new InputStreamReader(process.InputStream()));
                    //String inputLine = _in.ReadLine();
                    //logger.Info("machineName{" + nio.getName() + "}, ip{" + ip + "}," + _in.ReadLine());
                    //while((inputLine = _in.ReadLine()) != null) {

                    //    //logger.Info("machineName{" + nio.getName() + "}, ip{" + ip + "}," + inputLine);
                    //    pingResult += inputLine;
                    //    Thread.Sleep(10000);

                    //    _in = new StreamReader(new InputStreamReader(process.getInputStream()));
                    //    _in.ReadLine();
                    //    _in.ReadLine();
                    //}
                    //_in.Close();

                    bool pingable = false;
                    Ping pinger = null;

                    while (true)
                    {
                        try
                        {
                            pinger = new Ping();
                            logger.Info("machineName{" + nio.getName() + "}, ip{" + ip + "}");

                            PingReply reply = pinger.Send(ip);
                            pingable = reply.Status == IPStatus.Success;
                            logger.Info("machineName{" + nio.getName() + "}, ip{" + ip + "}, " + pingable.ToString());
                            Thread.Sleep(10000);
                        }
                        catch (PingException ex)
                        {
                            logger.Error(ex);
                        }
                        finally
                        {
                            if (pinger != null)
                            {
                                pinger.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

                logger.Error(e);
            }
        }

        public bool CheckTransportCommandDest(TransferMessageEx transferMessage)
        {

            bool result = false;

            String destMachineId = transferMessage.DestMachine;
            String destPortId = transferMessage.DestUnit;
            String destLocationId = destMachineId + ":" + destPortId;

            result = this.ResourceManager.CheckLocation(destLocationId);
            if (!result)
            {

                transferMessage.ReplyCode = AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_DESTMACHINE_NOTFOUND.Item2;
                logger.Warn("destLocation does not exist in DB, " + transferMessage);
            }
            return result;
        }

        public bool IsSourceVehicle(TransferMessageEx transferMessage)
        {

            bool result = false;
            //"B2OLA02_OL01_IP01"
            // 181011 KSG..
            // check Wrong source port/unit, sourceunit is vehicle id 
            String vehicleId = transferMessage.SourceUnit.Trim();

            //check vehicle exist in DB
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleId);

            if (vehicle != null)
            {
                transferMessage.Vehicle = (vehicle);
                transferMessage.VehicleId = (vehicleId);
                result = true;
            }
            else
            {

                result = CheckTransferCommandSourceDest(transferMessage);
            }

            return result;
        }
        public void SendTransportMessageDest(TransferMessageEx transferMessage)
        {
            this.MessageManager.SendTransportCommandDest(transferMessage);
        }

        public void SendVehicleMessageMCodeDestPIOConnectError(VehicleMessageEx vehicleMessage)
        {

            this.MessageManager.SendVehicleMessageMCodeDestPIOConnectError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeDestPIORequestError(VehicleMessageEx vehicleMessage)
        {

            this.MessageManager.SendVehicleMessageMCodeDestPIORequestError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeDestPIORunError(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeDestPIORunError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeDestPIOPortCheckError(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeDestPIOPortCheckError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeSourcePIOConnectError(VehicleMessageEx vehicleMessage)
        {

            this.MessageManager.SendVehicleMessageMCodeSourcePIOConnectError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeSourcePIORequestError(VehicleMessageEx vehicleMessage)
        {

            this.MessageManager.SendVehicleMessageMCodeSourcePIORequestError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeSourcePIORunError(VehicleMessageEx vehicleMessage)
        {

            this.MessageManager.SendVehicleMessageMCodeSourcePIORunError(vehicleMessage);
        }

        public void SendVehicleMessageMCodeSourcePIOPortCheckError(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeSourcePIOPortCheckError(vehicleMessage);
        }

        public void SendPermitMessageToWaitVehicles(VehicleMessageEx vehicleMessage)
        {

            IList vehicleList = vehicleMessage.Vehicles;

            if (vehicleList != null && vehicleList.Count > 0)
            {
                //logger.Info("vehicleMessage.Vehicles exist. " + vehicleMessage);
                for (IEnumerator iterator = vehicleList.GetEnumerator(); iterator.MoveNext(); )
                {
                    VehicleEx vehicle = (VehicleEx)iterator.Current;
                    logger.Info("vehicle=[" + vehicle.ToString() + "]");
                    vehicleMessage.VehicleId = (vehicle.Id);
                    vehicleMessage.NodeId = (vehicle.CurrentNodeId);

                    logger.Info("sendVehiclePermitMessage. " + vehicleMessage);
                    
                    this.SendVehiclePermitMessage(vehicleMessage);
                }
            }
            else
            {
                //logger.Warn("vehicleMessage.Vehicles is null. " + vehicleMessage);
            }
        }


        public VehicleMessageEx CreateVehicleMessage(TransportCommandEx transportCommand)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(transportCommand.VehicleId);
            return this.MessageManager.CreateVehicleMessage(vehicle);
        }
        public VehicleMessageEx CreateVehicleMessageByStealTransportCommand(VehicleMessageEx oldVehicleMessage, TransportCommandEx stillTransportCommand)
        {

            VehicleEx vehicle = oldVehicleMessage.Vehicle;
            VehicleMessageEx newVehicleMessage = this.MessageManager.CreateVehicleMessage(vehicle);
            newVehicleMessage.TransportCommand = (stillTransportCommand);
            newVehicleMessage.TransportCommandId = (stillTransportCommand.Id);

            return newVehicleMessage;
        }


        public void SendVehicleMessageMCodeCarrierLoaded(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeCarrierLoaded(vehicleMessage);
        }


        public void SendVehicleMessageMCodeCarrierRemoved(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeCarrierRemoved(vehicleMessage);
        }


        public void SendVehicleMessageMCodePortError(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodePortError(vehicleMessage);
        }

        // Add 20190813
        public void ReplyMoveUpdate(TransferMessageEx transferMessage)
        {
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CmdType").InnerText = TransportCommandEx.TYPE_CMDUPDATEREPLY;
                    sendingMessageTemp = sendingMessage;

                    string[] source = transferMessage.TransportCommand.Source.Split(':');
                    string[] dest = transferMessage.TransportCommand.Dest.Split(':');

                    string transEQP = "";

                    if (!String.IsNullOrEmpty(transferMessage.TransportCommand.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(transferMessage.TransportCommand.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                    }
                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.TransportCommand.CarrierId;
                    string transUnit = transferMessage.TransportCommand.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string userId = "";
                    //string priority = "-";
                    string priority = transferMessage.TransportCommand.Priority.ToString();
                    string description = transferMessage.TransportCommand.Description;
                    string errorCode = "0";

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", "");
                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }
                }
                else
                {
                    //logger.Info ("send{" + messageName + "} not used" + VehicleMessageEx.ToString());
                }
            }
            catch (Exception e)
            {
                //logger.Warn("failed to report send{" + messageName + "}", e);
            }

        }
        // Add 20190813
        
        public void ReplyMoveUpdate1(TransferMessageEx transferMessage)
        {


            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {
                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/Type").InnerText = TransportCommandEx.TYPE_CMDUPDATEREPLY;
                    sendingMessageTemp = sendingMessage;

                    //data insert
                    String eqpId = "";
                    String portId = "";
                    String jobId = transferMessage.TransportCommandId;
                    String matType = "";


                    String finalLoc = transferMessage.TransportCommand.Dest;
                    String resultCode = "0";

                    sendingMessage.SelectSingleNode("/Msg/DataLayer/EqpID").InnerText = eqpId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/PortID").InnerText = portId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/ErrCode").InnerText = resultCode;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/JobID").InnerText = jobId;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/MatType").InnerText = matType;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/FinalLoc").InnerText = finalLoc;
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/ErrMsg").InnerText = "MOVEUPDATEPASS";

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }

                }
                else
                {
                    //logger.Info ("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                //logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveUpdateNak3(TransferMessageEx transferMessage)
        {
            //         //MOVEUPDATE_REP Nak
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                < ProcessID > MSP380 </ ProcessID >
            //                < ProductID > AMB585NE01 - 036 </ ProductID >
            //                < StepID > MD370 </ StepID >
            //                < Quantity > 240 </ Quantity >
            //                < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //                < TransUnit ></ TransUnit > --AGV Name
            //                < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //              <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CmdType").InnerText = TransportCommandEx.TYPE_CMDUPDATEREPLY;
                    sendingMessageTemp = sendingMessage;

                    //data insert

                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;

                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = "3";
                    string errorMsg = "MOVEUPDATEFAIL";

                    //181001
                    string processId = transferMessage.ProcessId;
                    string productId = transferMessage.ProductId;

                    string transEQP = transferMessage.EqpId;

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    //181001
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProcessID", processId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProductID", productId);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }
                }
                else
                {
                    //logger.Info ("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                //logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveUpdateNak5(TransferMessageEx transferMessage)
        {
            //         //MOVEUPDATE_REP Nak
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                < ProcessID > MSP380 </ ProcessID >
            //                < ProductID > AMB585NE01 - 036 </ ProductID >
            //                < StepID > MD370 </ StepID >
            //                < Quantity > 240 </ Quantity >
            //                < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //                < TransUnit ></ TransUnit > --AGV Name
            //                < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //               <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CmdType").InnerText = TransportCommandEx.TYPE_CMDUPDATEREPLY;
                    sendingMessageTemp = sendingMessage;

                    //data insert
                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;

                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = "5";
                    string errorMsg = "MOVEUPDATEFAIL";

                    //181001
                    string processId = transferMessage.ProcessId;
                    string productId = transferMessage.ProductId;

                    string transEQP = transferMessage.EqpId;

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    //181001
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProcessID", processId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProductID", productId);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }
                }
                else
                {
                    //logger.Info ("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                //logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void SendTransportMessageNewDest(TransferMessageEx transferMessage)
        {
            this.MessageManager.SendTransportCommandNewDest(transferMessage);
        }

        public void ReplyMoveUpdateNak99(TransferMessageEx transferMessage)
        {
            //         //MOVEUPDATE_REP Nak
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                < ProcessID > MSP380 </ ProcessID >
            //                < ProductID > AMB585NE01 - 036 </ ProductID >
            //                < StepID > MD370 </ StepID >
            //                < Quantity > 240 </ Quantity >
            //                < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //                < TransUnit ></ TransUnit > --AGV Name
            //                < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //                <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, transferMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CmdType").InnerText = TransportCommandEx.TYPE_CMDUPDATEREPLY;
                    sendingMessageTemp = sendingMessage;

                    //data insert

                    string jobId = transferMessage.TransportCommandId;
                    string carrID = transferMessage.CarrierId;
                    string transUnit = transferMessage.VehicleId;
                    string sourceEQP = transferMessage.SourceMachine;
                    string sourceUnit = transferMessage.SourceUnit;
                    string finalEQP = transferMessage.DestMachine;
                    string finalUnit = transferMessage.DestUnit;

                    string priority = transferMessage.Priority.ToString();
                    string userId = transferMessage.UserId;
                    string description = transferMessage.Description;
                    string errorCode = "99";
                    string errorMsg = "MOVEUPDATEFAIL";

                    //181001
                    string processId = transferMessage.ProcessId;
                    string productId = transferMessage.ProductId;

                    string transEQP = transferMessage.EqpId;

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", errorMsg);

                    //181001
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProcessID", processId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ProductID", productId);

                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        transferMessage.SendingMessage = (sendingMessageTemp);
                        this.HostMessageManager.SendMessageToHost(transferMessage, this.HostMessageManager.GetSendHostMessageName(messageName));
                    }
                }
                else
                {
                    //logger.Info ("send{" + messageName + "} not used" + transferMessage.ToString());
                }
            }
            catch (Exception e)
            {
                //logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void ReplyMoveUpdate(VehicleMessageEx vehicleMessage)
        {
            //         //MOVEUPDATE_REP
            // < Msg >
            //        < Command > TRSJOBREPORT </ Command >
            //        < Header >
            //                < DestSubject >/ BN1 / BNY2 / MOS / CLIENT / MHS </ DestSubject >
            //                < ReplySubject >/ BN1 / BNY2 / ACS </ ReplySubject >
            //        </ Header >
            //        < DataLayer >
            //                < CmdType > MOVECMD_REP </ CmdType > --MOVEUPDATE_REP, MOVECANCEL_REP 동일 포멧 사용
            //                < JobID > Q310579928_41V3752499_171106013831_A </ JobID > --필수 : JobID는 ADS의 송신 값 사용
            //                <CarrID> Q310579928A741DZ366141V3752499</ CarrID >
            //                < BatchID > A3NE1R7ANFLBG090 </ BatchID >
            //                < ProcessID > MSP380 </ ProcessID >
            //                < ProductID > AMB585NE01 - 036 </ ProductID >
            //                < StepID > MD370 </ StepID >
            //                < Quantity > 240 </ Quantity >
            //                < TransEQP > B2ACS01 </ TransEQP > --ACS Name
            //                < TransUnit ></ TransUnit > --AGV Name
            //                < CurrentLoc ></ CurrentLoc >
            //                < SourceEQP > B2CPA01_CP01 </ SourceEQP >
            //                < SourcePort > B2CPA01_CP01_OP01 </ SourcePort >
            //                < FinalEQP > B2OLA03_OL01 </ FinalEQP >
            //                < FinalPort > B2OLA03_OL01_IP01 </ FinalPort >
            //                < Priority > 5 </ Priority >
            //                < UserID > ADS_UD_NORMAL </ UserID >
            //                < Description ></ Description >
            //                < ErrCode > 3 </ ErrCode > --필수, ErrCode 0은 정상 수행가능, 이외 값 ACS에서 Fail Case 정의, Code는 1~99 사용
            //               <ErrMsg> Source Port is not registered in ACS </ ErrMsg > --ErrCode 0은 내용 불필요, 0이 아닌 경우 Fail 사유 기록
            //        </ DataLayer >
            // </ Msg >
            //*/
            String messageName = "TRSJOBREPORT";

            try
            {
                if (this.HostMessageManager.UseSend(messageName))
                {

                    XmlDocument sendingMessage = (XmlDocument)this.HostMessageManager.Convert(messageName, vehicleMessage);
                    XmlDocument sendingMessageTemp = new XmlDocument();

                    //XmlUtils.setXmlData(sendingMessage, "/Msg/DataLayer/Type", TransportCommandEx.TYPE_RECEIVE);
                    sendingMessage.SelectSingleNode("/Msg/DataLayer/CmdType").InnerText = TransportCommandEx.TYPE_CMDUPDATEREPLY;
                    sendingMessageTemp = sendingMessage;

                    string[] source = vehicleMessage.TransportCommand.Source.Split(':');
                    string[] dest = vehicleMessage.TransportCommand.Dest.Split(':');

                    string transEQP = "";

                    if (!String.IsNullOrEmpty(vehicleMessage.VehicleId))
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                        if (!String.IsNullOrEmpty(vehicle.BayId))
                        {
                            transEQP = vehicle.BayId.Split('_')[0];
                        }
                    }
                    string jobId = vehicleMessage.TransportCommandId;
                    string carrID = vehicleMessage.TransportCommand.CarrierId;
                    string transUnit = vehicleMessage.VehicleId;
                    string sourceEQP = source[0];
                    string sourceUnit = source[1];
                    string finalEQP = dest[0];
                    string finalUnit = dest[1];
                    string userId = "";
                    //KSB
                    //string priority = "-";
                    string priority = vehicleMessage.TransportCommand.Priority.ToString();
                    string description = vehicleMessage.TransportCommand.Description;
                    string errorCode = "0";

                    //KSB - CarrID
                    //CarrID 와 JobID 가 같으면 CarrID는 null 처리해서 해야함
                    if (string.Equals(jobId, carrID, StringComparison.CurrentCultureIgnoreCase))
                    {
                        carrID = "";                    
                    }

                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransEQP", transEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/JobID", jobId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/CarrID", carrID);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/TransUnit", transUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourceEQP", sourceEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/SourcePort", sourceUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalEQP", finalEQP);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/FinalPort", finalUnit);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Priority", priority);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/UserID", userId);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/Description", description);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrCode", errorCode);
                    XmlUtility.SetDataToXml(sendingMessage, "/Msg/DataLayer/ErrMsg", "");
                    ////180928 ACS→ADS Report XML Format Pair
                    //sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //180928 ACS→ADS Report XML Format Pair
                    sendingMessage.InnerXml = sendingMessage.OuterXml.Replace("\u200B", "");
                    //sendingMessage.PreserveWhitespace = true;

                    //TEST 한줄 -> 띄어쓰기
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.IndentChars = "    ";  // '\t' 4개의 공백문자
                    settings.NewLineChars = Environment.NewLine;
                    settings.NewLineHandling = NewLineHandling.Replace;

                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create(sb, settings);

                    sendingMessage.Save(writer);
                    sendingMessageTemp.PreserveWhitespace = true;
                    sendingMessageTemp.InnerXml = sb.ToString();
                    //

                    if (sendingMessage != null)
                    {
                        vehicleMessage.SendingMessage = sendingMessageTemp;
                        this.HostMessageManager.SendMessageToHost(vehicleMessage, this.HostMessageManager.GetSendHostMessageName(messageName));

                    }

                }
                else
                {
                    //logger.Info ("send{" + messageName + "} not used" + VehicleMessageEx.ToString());
                }
            }
            catch (Exception e)
            {
                //logger.Warn("failed to report send{" + messageName + "}", e);
            }
        }

        public void CreateInformAutoCallFail(String resultMessage, String source, String jobId)
        {


            String message = "AUTOCALL failed for the following reason [" + resultMessage + "]";
            String desc = "Please search TransportCmdHistory by JOBID [" + jobId + "]";

            Inform inform = new Inform();
            inform.Id = Guid.NewGuid().ToString();
            inform.Time = (DateTime.Now);
            inform.Type = (Inform.INFORM_TYPE_NOTICE);
            inform.Message = (message);
            inform.Source = (source);
            inform.Description = (desc);

            this.ResourceManager.CreateInform(inform);

        }

        public void CreateInformVehicleMismachNIO(VehicleMessageEx vehicleMessage)
        {

            IList informList = this.ResourceManager.GetUIInformByMessage(vehicleMessage.VehicleId, vehicleMessage.Cause);

            if (informList.Count == 0)
            {

                Inform inform = new Inform();
                inform.Id = Guid.NewGuid().ToString();

                inform.Time = DateTime.Now;
                inform.Type = (Inform.INFORM_TYPE_IMPORTANT);
                inform.Message = (vehicleMessage.Cause);
                inform.Source = (vehicleMessage.VehicleId);
                inform.Description = (vehicleMessage.Cause);    //.ReceivedMessage);
                //20190830 KSB
                //String description = XmlUtils.getXpathData((XmlDocument) vehicleMessage.ReceivedMessage, "//DESCRIPTION");
                //190510 KSB Add Description (Reason->DB)
                string description = XmlUtility.GetDataFromXml((XmlDocument)vehicleMessage.ReceivedMessage, "/Msg/DataLayer/Description");

                //XmlDocument document = (XmlDocument)vehicleMessage.ReceivedMessage;
                //String description = document.SelectSingleNode("//DESCRIPTION").InnerText;

                if (string.IsNullOrEmpty(description))
                {

                    inform.Description = (description);
                    //logger.Error(description);
                }
                else
                {

                    inform.Description = ("Check registered NIO information of AGV [" + vehicleMessage.VehicleId + "].");
                    //logger.Error("Check registered NIO information of AGV ["+vehicleMessage.VehicleId+"].");
                }

                this.ResourceManager.CreateInform(inform);
            }
        }

        public bool CheckAndPopulateTransportCommand(TransferMessageEx transferMessage)
        {

            bool result = false;

            result = this.MessageManager.ValidateTransportCommand(transferMessage);

            if (result)
            {

                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);

                //SJP 1013
                if (transportCommand != null)
                {
                    transferMessage.TransportCommand = (transportCommand);
                    transferMessage.CarrierId = transportCommand.CarrierId;
                    transferMessage.VehicleId = transportCommand.VehicleId;
                    result = true;
                }
                else //LSJ 1018 COMMANDID = NULL RETURN FALSE
                {
                    result = false;
                }
            }
            return result;
        }

        public void SendVehicleMessageInformMismatchNIO(VehicleMessageEx vehicleMessage, Nio nio)
        {

            String text = "[" + vehicleMessage.VehicleId + "] NIO Information is incorrect. The registered with [" + nio.getName() + "]AGV-(" + nio.RemoteIp + ":" + nio.Port + ")";
            this.MessageManager.SendVehicleMessageInform(vehicleMessage, text);
        }

        public void SendVehicleMessageMCodeConveyorLoadingTimeout(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeConveyorLoadingTimeout(vehicleMessage);
        }

        public void SendVehicleMessageMCodeConveyorUnloadingTimeout(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeConveyorUnloadingTimeout(vehicleMessage);
        }


        public void SendVehicleMessageInformHeartCodeTimeout(VehicleMessageEx vehicleMessage)
        {

            String text = "Timeout Heart Code";
            String description = "Check wifi module of AGV [" + vehicleMessage.VehicleId + "]";
            this.MessageManager.SendVehicleMessageInform(vehicleMessage, text, description);
        }

        public void SendVehicleMessageTCode(String messageName, VehicleMessageEx vehicleMessage)
        {
            if (CheckGarbageNode(vehicleMessage))
            {
                String tCodeType = GetTCodeType(vehicleMessage);
                if (tCodeType.Equals("0"))
                {
                    this.MessageManager.SendVehicleMessageTCodeEnter(messageName, vehicleMessage);
                }
                else if (tCodeType.Equals("3"))
                {
                    this.MessageManager.SendVehicleMessageTCodePermission(messageName, vehicleMessage);
                }
            }
        }


        public void SendVehicleMessageMCodeStart(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeStart(vehicleMessage);
        }


        public void SendVehicleMessageMCodeStop(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeStop(vehicleMessage);
        }

        public void SendVehicleMessageMCodeMainboardVersion(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodeMainboardVersion(vehicleMessage);
        }

        public void SendVehicleMessageMCodePLCVersion(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleMessageMCodePLCVersion(vehicleMessage);
        }

        public UiTruncateMessageEx CreateUiTruncateMessage(XmlDocument XmlDocument)
        {
            return this.MessageManager.CreateUiTruncateMessage(XmlDocument);
        }

        public void ReplyMessage(AbstractMessage abstractMessage)
        {
            this.MessageManager.ReplyMessageToUi(abstractMessage);
        }

        public ControlMessageEx CreateControlMessage(XmlDocument document)
        {
            return this.MessageManager.CreateControlMessage(document);
        }

        public void SendVehicleOrderMessage(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehicleOrderCommand(vehicleMessage);
        }

        public void SendVehiclePermit0031Message(VehicleMessageEx vehicleMessage)
        {
            this.MessageManager.SendVehiclePermit0031Command(vehicleMessage);
        }
    }
}