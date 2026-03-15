using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using ACS.Communication.Socket.Model;
using ACS.Communication.Socket;
using System.Collections;
using System.Threading;
using ACS.Core.Logging;
using ACS.Core.Base;
using ACS.Core.Message.Model;

namespace ACS.Service
{
    public class VehicleInterfaceServiceEx : AbstractServiceEx
    {
        public Logger logger = Logger.GetLogger(typeof(VehicleInterfaceServiceEx));

        //private NioInterfaceManager NioInterfaceManager;
        //private ITransferManagerEx transferManager;
        // public IResourceManagerEx ResourceManager { get; set; } 
        //private ApplicationManagerImplement applicationManager;


#if BYTE12
        private String sendId = "JS";    
#else
        private String sendId = "A";
#endif
        //private static SimpleDateFormat simpleDateFormat = new SimpleDateFormat("HHmmss");  //DataTime.Now.ToString("HHmmss")

        public Hashtable transactionMap = new Hashtable();

        public NioInterfaceManager NioInterfaceManager { get; set; }
        //public ApplicationManagerImplement ApplicationManager { get { return this.applicationManager; } set { this.applicationManager = value; } }

        public String SendId { get { return this.sendId; } set { this.sendId = value; } }

        public IList GetNioes()
        {
            IDictionary nioInterfaces = this.NioInterfaceManager.NioInterfaces;
            IList nioes = new ArrayList();
            for (IEnumerator iterator = nioInterfaces.Values.GetEnumerator(); iterator.MoveNext();)
            {
                SocketClient socketClient = (SocketClient)iterator.Current;
                nioes.Add(socketClient.Nio);
            }
            return nioes;
        }

        public int StartNio(Nio nio)
        {
            return this.NioInterfaceManager.StartNioInterface(nio.getName());
        }

        public bool SendMessage(String message)
        {
            IDictionary nioes = this.NioInterfaceManager.NioInterfaces;
            //for (IEnumerator iterator = nioes.values().GetEnumerator(); iterator.MoveNext();) {

            //    AbstractSocketService abstractSocketService = (AbstractSocketService) iterator.Current;
            //    if(abstractSocketService.isSessionOpened()) {

            //        abstractSocketService.send(message);
            //    }
            //}
            return true;
        }

        public bool SendMessage(SendPacket sendPacket)
        {

            //		Map nioes = this.NioInterfaceManager.GetNioInterfaces();
            //		for (Iterator iterator = nioes.values().iterator(); iterator.hasNext();) {
            //			
            //			AbstractSocketService abstractSocketService = (AbstractSocketService) iterator.next();
            //			if(abstractSocketService.isSessionOpened()) {
            //				
            //				abstractSocketService.send(sendPacket);
            //			}
            //		}
            //		return true;


            //		String applicationName = this.applicationManager.getApplicationName();
            //		
            //		if (checkWifiNioByApplicationName(applicationName)) 
            //		{
            //			//wifi�씠硫�
            //			String vehicleName = sendPacket.getRevID();
            //			AbstractSocketService abstractSocketService = (AbstractSocketService)this.NioInterfaceManager.GetNioInterface(vehicleName);
            //			if(abstractSocketService.isSessionOpened()) {
            //				
            //				abstractSocketService.send(sendPacket);
            //			}
            //			else
            //			{
            //				//logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.toString());
            //			}
            //		}
            //		else
            //		{
            //			//以묎퀎湲곗슜�씠硫�
            //			Map nioes = this.NioInterfaceManager.GetNioInterfaces();
            //			for (Iterator iterator = nioes.values().iterator(); iterator.hasNext();) {
            //				
            //				AbstractSocketService abstractSocketService = (AbstractSocketService) iterator.next();
            //				if(abstractSocketService.isSessionOpened()) {
            //					
            //					abstractSocketService.send(sendPacket);
            //				}
            //			}
            //		}

            //		if (this.NioInterfaceManager.GetNioInterface(sendPacket.getSendID()) != null) 
            if (this.NioInterfaceManager.GetNioInterface(sendPacket.RevId) != null)
            {
                //wifi & Zigbee
                //			String vehicleName = sendPacket.getSendID();
                String vehicleName = sendPacket.RevId;
                AbstractSocketService abstractSocketService = (AbstractSocketService)this.NioInterfaceManager.GetNioInterface(vehicleName);
                if (abstractSocketService.SessionOpened)
                {
                    abstractSocketService.Send(sendPacket);
                }
                else
                {
                    logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
                }
            }
            else
            {
                //RF
                IDictionary nioes = this.NioInterfaceManager.NioInterfaces;

                for (IEnumerator iterator = nioes.Values.GetEnumerator(); iterator.MoveNext();)
                {
                    AbstractSocketService abstractSocketService = (AbstractSocketService)iterator.Current;
                    if (abstractSocketService.SessionOpened)
                    {
                        abstractSocketService.Send(sendPacket);
                    }
                }
            }

            return true;
        }

        public bool SendTransferSourcePort(TransferMessageEx transferMessage)
        {
            String vehicleId = transferMessage.VehicleId;
            String commandCode = VehicleMessageEx.COMMAND_CODE_C;
            String portId = transferMessage.SourceMachine + ":" + transferMessage.SourceUnit;
            LocationEx sourcePortLocation = this.ResourceManager.GetLocationByPortId(portId);
            String tagNumber = sourcePortLocation.StationId;

            String commandType;
            //KSB
            if (sourcePortLocation.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                commandType = VehicleMessageEx.C_CODE_TYPE_LEFTLOAD;
            else //if(destPortLocation.Direction.Equals("RIGHT", StringComparison.OrdinalIgnoreCase))
                commandType = VehicleMessageEx.C_CODE_TYPE_RIGHTLOAD;

            //		SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, tagNumber, commandType);
            SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, tagNumber + commandType);

            return this.SendMessage(sendPacket);
        }

        public bool SendVehicleGoCommand(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            String commandCode = VehicleMessageEx.COMMAND_CODE_T;
            String nodeId = vehicleMessage.NodeId;
            String commandType = "01";

            SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, commandType + nodeId);

            return this.SendMessage(sendPacket);
        }

        public bool SendTransferCommand(VehicleMessageEx vehicleMessage)
        {
#if BYTE12
            String vehicleId = vehicleMessage.VehicleId.Substring(vehicleMessage.VehicleId.Length-2);
            String commandCode = VehicleMessageEx.COMMAND_CODE_C;
            String destNodeId = vehicleMessage.NodeId.PadLeft(3,'0');
            String commandType = "01";
           
            destNodeId = vehicleMessage.NodeId.Substring(vehicleMessage.NodeId.Length - 2);

            //180925
            if (!string.IsNullOrEmpty(vehicleMessage.NodeId) && !string.IsNullOrEmpty(vehicleMessage.CCodeType))
            {
                SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, destNodeId + commandType);

                return this.SendMessage(sendPacket);
            }
            else
            {
                logger.Warn("if (!string.IsNullOrEmpty(vehicleMessage.NodeId) && !string.IsNullOrEmpty(vehicleMessage.CCodeType))");
                return false;
            }
            
#else
            String vehicleId = vehicleMessage.VehicleId;
            String commandCode = VehicleMessageEx.COMMAND_CODE_C;
            String destNodeId = vehicleMessage.NodeId.PadLeft(4, '0');
            String commandType = vehicleMessage.CCodeType;

            //180925
            if (!string.IsNullOrEmpty(vehicleMessage.NodeId) && !string.IsNullOrEmpty(vehicleMessage.CCodeType))
            {
                SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, destNodeId + commandType);

                return this.SendMessage(sendPacket);
            }
            else
            {
                logger.Warn("if (!string.IsNullOrEmpty(vehicleMessage.NodeId) && !string.IsNullOrEmpty(vehicleMessage.CCodeType))");
                return false;
            }
#endif
        }

        public bool SendTransferDestPort(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            String commandCode = VehicleMessageEx.COMMAND_CODE_C;
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleId);

            if (transportCommand == null)
            {
                //logger.Warn("transportCommand does not exist in repository, " + vehicleMessage, vehicleMessage);
                logger.Warn("transportCommand does not exist in repository, " + vehicleMessage);
                return false;
            }
            String portId = transportCommand.Dest;
            LocationEx destPortLocation = this.ResourceManager.GetLocationByPortId(portId);
            String tagNumber = destPortLocation.StationId.PadLeft(4, '0');

            String commandType;
            if (destPortLocation.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                commandType = VehicleMessageEx.C_CODE_TYPE_LEFTUNLOAD;
            else //if(destPortLocation.Direction.Equals("RIGHT", StringComparison.OrdinalIgnoreCase))
                commandType = VehicleMessageEx.C_CODE_TYPE_RIGHTUNLOAD;

            //		SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, tagNumber, commandType);
            SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, tagNumber + commandType);

            return this.SendMessage(sendPacket);
        }

        public bool SendTCodeReply(ReceivePacket receivePacket)
        {
            //String stationId = receivePacket.getStation().substring(1) + receivePacket.getNumber();
            //		SendPacket sendPacket = new SendPacket(receivePacket.getSendID(), receivePacket.getRevID(), receivePacket.getCommand(), receivePacket.getStation(), receivePacket.getSubNumber());
            SendPacket sendPacket = new SendPacket(receivePacket.SendId, receivePacket.RevId, receivePacket.Command, "");
            return this.SendMessage(sendPacket);
        }

        public bool SendTCodeReply(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            String commandCode = VehicleMessageEx.COMMAND_CODE_T;

            String tagNumber = vehicleMessage.NodeId;
            String commandType = "01";

            //		SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, tagNumber, commandType);
            SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, commandType + tagNumber);

            return this.SendMessage(sendPacket);
        }

        public String GetMessageName(ReceivePacket receivePacket)
        {

            String messageName = "";
            String commandCode = receivePacket.Command;
            if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_C))
            {
                messageName = "C_CODE_REP";
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_S))
            {

                messageName = "S_CODE";
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_T))
            {

                messageName = "T_CODE";
            }

            return messageName;
        }

        private int UpdateNioState(Nio nio)
        {
            return this.NioInterfaceManager.UpdateNioState(nio);
        }


        public int UpdateNioStateConnect(Nio nio)
        {
            nio.State = Nio.NIO_STATE_CONNECED;
            return this.NioInterfaceManager.UpdateNioState(nio);
        }

        public int UpdateNioStateDisconnect(Nio nio)
        {

            if (!nio.State.Equals(Nio.NIO_STATE_CLOSED) && !nio.State.Equals(Nio.NIO_STATE_UNLOADED))
            {

                nio.State = Nio.NIO_STATE_CONNECTING;
                return this.NioInterfaceManager.UpdateNioState(nio);
            }
            else
            {

                //logger.fine("already changed state{" + nio.getState() + "}, " + nio);
                return 0;
            }
        }

        private bool CheckWifiNioByApplicationName(String applicationName)
        {
            IList listNio = this.NioInterfaceManager.GetNioesByApplicationName(applicationName);

            if (listNio != null && listNio.Count > 0)
            {
                for (IEnumerator iterator = listNio.GetEnumerator(); iterator.MoveNext();)
                {
                    Nio nio = (Nio)iterator.Current;
                    String esType = nio.MachineName;
                    if (esType.Equals("WIFI"))
                    {
                        return true;
                    }
                }
            }
            else
            {
                logger.Warn("Can Not Find Nio Interface By APP : " + applicationName);
            }
            return false;
        }

        public bool RestartNio(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            SocketClient socketClient = (SocketClient)this.NioInterfaceManager.GetNioInterface(vehicleId);

            //		Nio nio = this.NioInterfaceManager.getNio(vehicleId, socketClient.getNio().getApplicationName());
            Nio nio = socketClient.Nio;
            return RestartNio(nio);
        }

        public bool RestartNio(Nio nio)
        {
            bool result = false;

            if (nio.State.Equals(Nio.NIO_STATE_CONNECED))
            {

                result = this.NioInterfaceManager.StopNioInterface(nio.getName());

                if (result)
                {

                    //logger.fine("closed nio{" + nio.getName() + "}");
                    Thread.Sleep(500);
                    if (this.NioInterfaceManager.StartNioInterface(nio.getName()) > 0)
                    {
                        //logger.fine("started nio{" + nio.getName() + "}, ");
                    }
                    else
                    {
                        logger.Error("can't started nio{" + nio.getName() + "}, ");
                    }
                }
                else
                {
                    //logger.Warn("can not closed nio{" + nio.getName() + "}, ");
                }
            }
            return result;
        }


        public bool SendNioHeartCode(VehicleMessageEx vehicleMessage)
        {

            String vehicleId = vehicleMessage.VehicleId;
            String commandCode = VehicleMessageEx.COMMAND_CODE_H;
            //String transactionId = simpleDateFormat.format(new DateTime());
#if BYTE12
            String transactionId = DateTime.Now.ToString("mmss");
#else
            String transactionId = DateTime.Now.ToString("HHmmss");
#endif
            SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, transactionId);
            this.transactionMap[vehicleId] = transactionId;

            return this.SendMessage(sendPacket);
        }

        public void ReceiveNioHeartCode(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            if (!string.IsNullOrEmpty(vehicleMessage.KeyData))
            {
                String receiveTransactionId = vehicleMessage.KeyData;
                String sendTransactionId = (String)this.transactionMap[vehicleId];

                if (!string.IsNullOrEmpty(sendTransactionId) && receiveTransactionId.Equals(sendTransactionId))
                {
                    this.transactionMap.Remove(vehicleId);
                }
                else
                {
                    logger.Warn("unmatched transactionId, sendTransactionId{" + sendTransactionId + "}, receiveTransactionId{" + receiveTransactionId + "}, vehicleId{" + vehicleId + "}");
                }
            }
            else
            {
                logger.Warn("please check message, {" + vehicleMessage.KeyData + "}, vehicleId{" + vehicleId + "}");
            }
        }


        public bool IsExistTransactionId(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            String vehicleId = vehicleMessage.VehicleId;
            result = this.transactionMap.ContainsKey(vehicleId);

            return result;
        }

        public bool DeleteTransactionId(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            String vehicleId = vehicleMessage.VehicleId;
            //if (this.transactionMap.Remove(vehicleId) != null)
            {
                this.transactionMap.Remove(vehicleId);
                result = true;
            }

            return result;
        }


        public bool StopNio(Nio nio)
        {
            return this.NioInterfaceManager.StopNioInterface(nio.getName());
        }

        public bool SendVehicleOrderCommand(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            String commandCode = "O";
            // 2019.05.28 - O code 02 is insert current node id to AGV
            String nodeId = vehicleMessage.NodeId;

            if (string.IsNullOrEmpty(nodeId))
            {
                logger.Error("NodeId is null!!, Please check this.");
                return false;
            }
            //nodeId = nodeId.split(",")[0];
            //dv.kien 2019.09.26 change from first node to second node

            //20230314 ORDER_ONEPOINT
            //nodeId = nodeId.Split(',')[1];
            //

            //20230417 ORDER_THREE
            //if (nodeId.Split(',').Length > 1)
            if(nodeId.Contains(','))
            {
                if (nodeId.Split(',')[1] == "0000")
                {
                    return false;
                }
                else
                {
                    nodeId = nodeId.Replace(",", "");
                }
            }
            else
            {
                //20230418 ORDER_NORMAL 12 Length
                nodeId = nodeId + nodeId + nodeId;
                //
            }
            //

            String commandType = "00";
            if (VehicleMessageExs.ORDER_NODE_TURN_LEFT_RUN.Equals(vehicleMessage.KeyData))
            {
                commandType = "07"; //211
            }
            else if (VehicleMessageExs.ORDER_NODE_TURN_RIGHT_RUN.Equals(vehicleMessage.KeyData))
            {
                commandType = "06"; //212
            }
            else if (VehicleMessageExs.ORDER_NODE_TURN_LEFT_BACK.Equals(vehicleMessage.KeyData))
            {
                commandType = "09"; //221
            }
            else if (VehicleMessageExs.ORDER_NODE_TURN_RIGHT_BACK.Equals(vehicleMessage.KeyData))
            {
                commandType = "08"; //222
            }
            else if (VehicleMessageExs.ORDER_NODE_CHANGE_LINE_LEFT.Equals(vehicleMessage.KeyData))
            {
                commandType = "10";
            }
            else if (VehicleMessageExs.ORDER_NODE_CHANGE_LINE_RIGHT.Equals(vehicleMessage.KeyData))
            {
                commandType = "11";
            }
            //20230307 ORDER_VISION
            else if (VehicleMessageExs.ORDER_NODE_VISION.Equals(vehicleMessage.KeyData))
            {
                commandType = "50";
                logger.Fatal("20230307: ES - " + vehicleMessage.VehicleId + commandCode + commandType + nodeId);
            }
            //

            // // O code 03 is turn order. 02 is CCW 90. 01 is start
            // String nodeId = "0201";
            // String commandType = "03";
            logger.Warn("Sending " + vehicleMessage.VehicleId + commandCode + commandType + nodeId);
            SendPacket sendPacket = new SendPacket(vehicleId, this.sendId, commandCode, commandType + nodeId);

            return this.SendMessage(sendPacket);
        }
    }
}

