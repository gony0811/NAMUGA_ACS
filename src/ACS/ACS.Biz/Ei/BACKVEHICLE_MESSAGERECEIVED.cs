using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Spring.Context;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Communication.Socket.Model;
using ACS.Communication.Socket;
using System.Xml;

namespace ACS.Biz.Ei
{
    public class BACKVEHICLE_MESSAGERECEIVED : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService ;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        { 
            get { return commandJobList;}  
            set { commandJobList = value;} 
        }

        public override int ExecuteJob(object[] args)
        {
            ReceivePacket receivePacket = (ReceivePacket)args[0];
            Nio nio = (Nio)args[2];

            VehicleMessageEx vehicleMsg;
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");



            if (InterfaceService.CheckNio(receivePacket, nio))
            {
                vehicleMsg = InterfaceService.CreateVehicleMessage(receivePacket);

                string messageName = InterfaceService.GetMessageName(vehicleMsg);

                switch(messageName)
                {
                    case "T_CODE":      //Tag Information
                        if(InterfaceService.CheckGarbageNode(vehicleMsg))
                        {
                            string tCodeType = InterfaceService.GetTCodeType(vehicleMsg);
                            SwitchOfTCodeType(tCodeType, messageName, vehicleMsg);
                        }
                        else
                        {
                            //Terminate
                        }
                        break;
                    case "E_CODE":      //Error Code
                        InterfaceService.SendVehicleMessageECode(messageName, vehicleMsg);
                        break;
                    case "R_CODE":      //Return Value          
                        string rCodeType = InterfaceService.GetRCodeType(vehicleMsg);
                        SwitchOfRCodeType(rCodeType, messageName, vehicleMsg);
                        break;
                    case "L_CODE":      //Loading
                        InterfaceService.SendVehicleMessageLCode(messageName, vehicleMsg);
                        break;
                    case "U_CODE":      //Unloading
                        InterfaceService.SendVehicleMessageUCode(messageName, vehicleMsg);
                        break;
                    case "S_CODE":      //AGV State
                        InterfaceService.SendVehicleMessageSCode(messageName, vehicleMsg);
                        break;
                    case "C_CODE":      //Command
                        InterfaceService.SendVehicleMessageCCode(messageName, vehicleMsg);
                        break;
                    case "M_CODE":      //Abnormal
                        string mCodeType = InterfaceService.GetMCodeType(vehicleMsg);
                        SwitchOfMCodeType(mCodeType, messageName, vehicleMsg);
                        break;
                    case "H_CODE":      //Alive Check 
                        VehicleInterfaceService.ReceiveNioHeartCode(vehicleMsg);
                        break;
                    default :
                        break;
                }
            }
            else
            {
                vehicleMsg = InterfaceService.CreateVehicleMessage(receivePacket);
                InterfaceService.SendVehicleMessageInformMismatchNIO(vehicleMsg, nio);
            }

            return 0;
        }
        public void SwitchOfMCodeType(string mCodeType,string msgName, VehicleMessageEx VehicleMsg)
        {
            switch (mCodeType)
            {
                case MCodeConsts.CHARGESTARTED:
                    InterfaceService.SendVehicleMessageMCodeChargeStart(msgName,VehicleMsg);
                    break;
                case MCodeConsts.CHARGECOMPLETED:
                    InterfaceService.SendVehicleMessageMCodeChargeComplete(msgName, VehicleMsg);
                    break;
                case MCodeConsts.VEHICLEEMPTY:
                    InterfaceService.SendVehicleMessageMCodeVehicleEmpty(msgName, VehicleMsg);
                    break;
                case MCodeConsts.VEHICLEOCCUPIED:
                    InterfaceService.SendVehicleMessageMCodeVehicleOccupied(msgName, VehicleMsg);
                    break;
                case MCodeConsts.DestPIOConnectError:
                    InterfaceService.SendVehicleMessageMCodeDestPIOConnectError(VehicleMsg);
                    break;
                case MCodeConsts.DestPIORequestError:
                    InterfaceService.SendVehicleMessageMCodeDestPIORequestError(VehicleMsg);
                    break;
                case MCodeConsts.DestPIORunError:
                    InterfaceService.SendVehicleMessageMCodeDestPIORunError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIOConnectError:
                    InterfaceService.SendVehicleMessageMCodeSourcePIOConnectError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIORequestError:
                    InterfaceService.SendVehicleMessageMCodeSourcePIORequestError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIORunError:
                    InterfaceService.SendVehicleMessageMCodeSourcePIORunError(VehicleMsg);
                    break;
                case MCodeConsts.CarrierLoaded:
                    InterfaceService.SendVehicleMessageMCodeCarrierLoaded(VehicleMsg);
                    break;
                case MCodeConsts.CarrierRemoved:
                    InterfaceService.SendVehicleMessageMCodeCarrierRemoved(VehicleMsg);
                    break;
                case MCodeConsts.PortError:
                    InterfaceService.SendVehicleMessageMCodePortError(VehicleMsg);
                    break;
                case MCodeConsts.ConveyorLoadingTimeOut:
                    InterfaceService.SendVehicleMessageMCodeConveyorLoadingTimeout(VehicleMsg);
                    break;
                case MCodeConsts.ConveyorUnloadingTimeOut:
                    InterfaceService.SendVehicleMessageMCodeConveyorUnloadingTimeout(VehicleMsg);
                    break;
                default:
                    break;
            }
        }
        public void SwitchOfRCodeType(string rCodeType, string msgName, VehicleMessageEx VehicleMsg)
        {
            switch (rCodeType)
            {
                case RCodeConsts.VOLTAGE:
                    InterfaceService.SendVehicleMessageRCodeVoltage(msgName, VehicleMsg);
                    break;
                case RCodeConsts.CAPACITY:
                    InterfaceService.SendVehicleMessageRCodeCapacity(msgName, VehicleMsg);
                    break;
              
                default:
                    break;
            }
        }
        public void SwitchOfTCodeType(string tCodeType, string msgName, VehicleMessageEx VehicleMsg)
        {
            switch (tCodeType)
            {
                case TCodeConsts.ENTER:
                    InterfaceService.SendVehicleMessageTCodeEnter(msgName, VehicleMsg);
                    break;
                case TCodeConsts.PERMISSION:
                    InterfaceService.SendVehicleMessageTCodePermission(msgName, VehicleMsg);
                    break;

                default:
                    break;
            }
        }
    }
}
