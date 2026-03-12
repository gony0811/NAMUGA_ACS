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
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;
using log4net;
using System.Xml;
using ACS.Framework.Resource;

namespace ACS.Biz.Ei
{
    public static class TCodeConsts
    {
        //T_Code Type
        public const string ENTER = "0";
        public const string PERMISSION = "3";

    }
    public static class RCodeConsts
    {
        //R_Code Type
        public const string VOLTAGE = "1";
        public const string CAPACITY = "2";

    }
    public static class MCodeConsts
    {
        //M_Code Type
        public const string CHARGESTARTED = "01";
        public const string CHARGECOMPLETED = "02";
        public const string START = "03";
        public const string STOP = "04";

        public const string VEHICLEEMPTY = "07";
        public const string VEHICLEOCCUPIED = "08";
        public const string DestPIOPortCheckError = "05";
        public const string DestPIOConnectError = "15";
        public const string DestPIORequestError = "25";
        public const string DestPIORunError = "35";
        public const string SourcePIOPortCheckError = "06";
        public const string SourcePIOConnectError = "16";
        public const string SourcePIORequestError = "26";
        public const string SourcePIORunError = "36";
        public const string CarrierLoaded = "40";
        public const string CarrierRemoved = "50";
        public const string PortError = "99";
        //KSB V2 사용
        //public const string ConveyorLoadingTimeOut = "46";
        //public const string ConveyorUnloadingTimeOut = "45";
        //KSB V1, V3 사용
        public const string ConveyorLoadingTimeOut = "70";
        public const string ConveyorUnloadingTimeOut = "60";

        public const string MAINBOARDVERSION = "80";
        public const string PLCVERSION = "90";

        //KKH PIO Error Recovery Log 20200807
        public const string AGVCHARGINGFAIL = "12";
        public const string RECOVERMISSMAGTAG = "41";
        public const string RECOVERMISSMAGTAGFAIL = "42";
        public const string RECOVERMISSMAGTAGSUCCESS = "43";
        public const string RECOVERAGVOUTRAIL = "51";
        public const string RECOVERAGVOUTRAILSUCCESS = "52";
        public const string RECOVERAGVOUTRAILFAIL = "53";
        public const string AGVAUTOSTART = "61";
        public const string AGVAUTOSTARTSUCCESS = "62";
        public const string AGVAUTOSTARTFAIL = "63";
        public const string AGVTURNPBSOFF = "71";
        public const string AGVTURNPBSOFFSUCCESS = "72";
        public const string AGVTURNPBSOFFFAIL = "73";
        public const string RECOVERYAGVBACK = "81";
        public const string RECOVERYAGVBACKSUCCESS = "82";
        public const string RECOVERYAGVBACKFAIL = "83";
        public const string AGVSENSORSONIC = "91";
        public const string AGVSENSORSONICSUCCESS = "92";
        public const string AGVSENSORSONICFAIL = "93";
        public const string HMIVERSION = "94";
    }

    public class VEHICLE_MESSAGERECEIVED : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(VEHICLE_MESSAGERECEIVED));
        public InterfaceServiceEx InterfaceService;
        public IResourceManagerEx ResourceManager;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        //public ResourceServiceEx ResourceService;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("ES VEHICLE_MESSAGERECEIVED BIZ Start============================================");
            ReceivePacket receivePacket = (ReceivePacket)args[0];
            Nio nio = (Nio)args[1];

            VehicleMessageEx vehicleMsg;
            ResourceManager = (IResourceManagerEx)ApplicationContext.GetObject("ResourceManager");
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            //ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");

            if (InterfaceService.CheckNio(receivePacket, nio))
            {
                //ReceivePacket 정의 되어야 함

                //181004
                //Verify Length
#if BYTE12
                if (receivePacket.Data.Length != 4)
                {
                    Logger.Fatal("receivePacket.Data.Length != 4 : " + receivePacket.Data);
                    return 0;
                }
#else
                if (receivePacket.Data.Length != 6)
                {
                    Logger.Fatal("receivePacket.Data.Length != 6 : " + receivePacket.Data);
                    return 0;
                }
#endif

                vehicleMsg = InterfaceService.CreateVehicleMessage(receivePacket);          //{A001C123456}
                string messageName = InterfaceService.GetMessageName(vehicleMsg);

                //bool isExistAGVNumberInDatabase = ResourceService.CheckVehicle(vehicleMsg);

                ////180928
                //if (isExistAGVNumberInDatabase)
                //{


                //KSB
                //ES09_P RF통신일경우만 사용...
                vehicleMsg.Vehicle = ResourceManager.GetVehicle(vehicleMsg.VehicleId);
                #region RF분리시(V1-1F, ES09만 사용중)
                ////2019.08.25 KSB KIT-자재 관련
                ////LIFT_1F-MTP_PASS, RACK-MTP 구간만 사용중  : ES09_P 에서만 사용하고 있음
                //if (!(vehicleMsg.Vehicle.BayId.Equals("KIT-MAT") || vehicleMsg.Vehicle.BayId.Equals("KIT-DIZI") || vehicleMsg.Vehicle.BayId.Equals("KIT-CP")))
                //{
                //    return 0;
                //}
                #endregion
                #region RF분리시(V1-2F, ES09만 사용중)
                ////2019.09.07 KSB 다빈치 구간만 임시로 사용중                
                //if (!(vehicleMsg.Vehicle.BayId.Equals("LIFT-FOD") || vehicleMsg.Vehicle.BayId.Equals("FOD-EMPTY") || vehicleMsg.Vehicle.BayId.Equals("FOD-DIZI")
                //   || vehicleMsg.Vehicle.BayId.Equals("JEST-EMPTY") || vehicleMsg.Vehicle.BayId.Equals("DIZI-LIFT_PASS")))
                //{
                //    return 0;
                //}
                #endregion


                switch (messageName)
                {
                    case "T_CODE":      //Tag Information
                        InterfaceService.SendVehicleMessageTCode(messageName, vehicleMsg);
                        logger.Debug("VEHICLE_MESSAGERECEIVED send MSG 'RAIL-VEHICLELOCATIONCHANGED(T_CODE)' to TS ");
                        break;
                    case "E_CODE":      //Error Code
                        InterfaceService.SendVehicleMessageECode(messageName, vehicleMsg);
                        break;
                    case "R_CODE":      //Return Value       
                        string rCodeType = InterfaceService.GetRCodeType(vehicleMsg);
                        SwitchOfRCodeType(rCodeType, messageName, vehicleMsg);
                        break;
                    case "O_CODE":      //Loading, Unloading
                        InterfaceService.SendVehicleMessageOCode(messageName, vehicleMsg);
                        break;
                    case "L_CODE":      //Loading
                        InterfaceService.SendVehicleMessageLCode(messageName, vehicleMsg);
                        logger.Debug("VEHICLE_MESSAGERECEIVED send MSG 'RAIL-VEHICLEACQUIRECOMPLETED(L_CODE)' to TS ");
                        break;
                    case "U_CODE":      //Unloading                        
                        InterfaceService.SendVehicleMessageUCode(messageName, vehicleMsg);
                        logger.Debug("VEHICLE_MESSAGERECEIVED send MSG 'RAIL-VEHICLEDEPOSITCOMPLETED(U_CODE)' to TS ");
                        break;
                    case "S_CODE":      //AGV State
                        //20230425 S_CODE Off
                        //InterfaceService.SendVehicleMessageSCode(messageName, vehicleMsg);
                        //logger.Debug("VEHICLE_MESSAGERECEIVED send MSG 'RAIL-VEHICLEINFOREPORT(S_CODE)' to TS");
                        //
                        break;
                    case "C_CODE":      //Command
                        InterfaceService.SendVehicleMessageCCode(messageName, vehicleMsg);
                        break;
                    case "M_CODE":      //Abnormal
                        //20230419 M_CODE Off
                        //vehicleMsg.Vehicle = ResourceManager.GetVehicle(vehicleMsg.VehicleId);
                        //if (!vehicleMsg.Vehicle.BayId.Equals("S0_CP"))
                        //{
                        //    string mCodeType = InterfaceService.GetMCodeType(vehicleMsg);
                        //    SwitchOfMCodeType(mCodeType, messageName, vehicleMsg);
                        //}
                        //
                        break;
                    case "H_CODE":      //Alive Check
                        VehicleInterfaceService.ReceiveNioHeartCode(vehicleMsg);
                        break;
                    case "C_CODE_REP":
                        TransferService.DeleteTransportCommandRequest(vehicleMsg);
                        InterfaceService.SendVehicleMessageCCode(messageName, vehicleMsg);//RAIL-CARRIERTRANSFERREPLY                       
                        logger.Debug("VEHICLE_MESSAGERECEIVED send MSG 'RAIL-CARRIERTRANSFERREPLY(C_CODE_REP)' to TS finish");
                        break;
                    default:
                        break;
                }
                //}
                //else
                //{
                //    //NG : Not AGV Number in Vehicle Database
                //    String message = vehicleMsg.MessageName + " VEHICLE [" + vehicleMsg.VehicleId + "] is not exist in ACS!!";
                //    String desc = " Add VEHICLE [" + vehicleMsg.VehicleId + "] to ACS.";

                //    logger.Info(message + desc);
                //}
            }
            else
            {
                logger.Debug("CheckNio FAIL ");
                vehicleMsg = InterfaceService.CreateVehicleMessage(receivePacket);
                InterfaceService.SendVehicleMessageInformMismatchNIO(vehicleMsg, nio);
                logger.Debug("VEHICLE_MESSAGERECEIVED Abnormal Case. SendVehicleMessageInformMismatchNIO to TS finish");
            }

            logger.Debug("ES VEHICLE_MESSAGERECEIVED BIZ End============================================");
            return 0;
        }
        public void SwitchOfMCodeType(string mCodeType, string msgName, VehicleMessageEx VehicleMsg)
        {
            switch (mCodeType)
            {
                case MCodeConsts.CHARGESTARTED: //M01
                    InterfaceService.SendVehicleMessageMCodeChargeStart(msgName, VehicleMsg);
                    break;
                case MCodeConsts.CHARGECOMPLETED: //M02
                    InterfaceService.SendVehicleMessageMCodeChargeComplete(msgName, VehicleMsg);
                    break;
                case MCodeConsts.VEHICLEEMPTY: //M07
                    InterfaceService.SendVehicleMessageMCodeVehicleEmpty(msgName, VehicleMsg);
                    break;
                case MCodeConsts.VEHICLEOCCUPIED: //M08
                    InterfaceService.SendVehicleMessageMCodeVehicleOccupied(msgName, VehicleMsg);
                    break;
                case MCodeConsts.DestPIOConnectError: //M15
                    InterfaceService.SendVehicleMessageMCodeDestPIOConnectError(VehicleMsg);
                    break;
                case MCodeConsts.DestPIORequestError: //M25
                    InterfaceService.SendVehicleMessageMCodeDestPIORequestError(VehicleMsg);
                    break;
                case MCodeConsts.DestPIORunError: //M35
                    InterfaceService.SendVehicleMessageMCodeDestPIORunError(VehicleMsg);
                    break;
                case MCodeConsts.DestPIOPortCheckError: //M05
                    InterfaceService.SendVehicleMessageMCodeDestPIOPortCheckError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIOConnectError: //M16
                    InterfaceService.SendVehicleMessageMCodeSourcePIOConnectError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIORequestError: //M26
                    InterfaceService.SendVehicleMessageMCodeSourcePIORequestError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIOPortCheckError: //M06
                    InterfaceService.SendVehicleMessageMCodeSourcePIOPortCheckError(VehicleMsg);
                    break;
                case MCodeConsts.SourcePIORunError: //M36
                    InterfaceService.SendVehicleMessageMCodeSourcePIORunError(VehicleMsg);
                    break;
                //20230316 M40 CarrierLoaded Block
                /*
                case MCodeConsts.CarrierLoaded: //40
                    InterfaceService.SendVehicleMessageMCodeCarrierLoaded(VehicleMsg);
                    break;
                */
                //
                case MCodeConsts.CarrierRemoved: //50
                    InterfaceService.SendVehicleMessageMCodeCarrierRemoved(VehicleMsg);
                    break;
                case MCodeConsts.PortError: //99
                    InterfaceService.SendVehicleMessageMCodePortError(VehicleMsg);
                    break;
                case MCodeConsts.ConveyorLoadingTimeOut: //70
                    InterfaceService.SendVehicleMessageMCodeConveyorLoadingTimeout(VehicleMsg);
                    break;
                case MCodeConsts.ConveyorUnloadingTimeOut: //60
                    InterfaceService.SendVehicleMessageMCodeConveyorUnloadingTimeout(VehicleMsg);
                    break;
                case MCodeConsts.START: //M03
                    InterfaceService.SendVehicleMessageMCodeStart(VehicleMsg);
                    break;
                case MCodeConsts.STOP:  //M04
                    InterfaceService.SendVehicleMessageMCodeStop(VehicleMsg);
                    break;
                case MCodeConsts.MAINBOARDVERSION:  //M80
                    InterfaceService.SendVehicleMessageMCodeMainboardVersion(VehicleMsg);
                    break;
                case MCodeConsts.PLCVERSION:    //M90
                    InterfaceService.SendVehicleMessageMCodePLCVersion(VehicleMsg);
                    break;
                // KKH PIO ERROR RECOVERY LOGIC LOG 20200807
                case MCodeConsts.AGVCHARGINGFAIL: //M12
                    InterfaceService.SendVehicleMessageMCodeAgvChargingFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERMISSMAGTAG: //M41
                    InterfaceService.SendVehicleMessageMCodeRecoverMissMagTag(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERMISSMAGTAGFAIL: //M42
                    InterfaceService.SendVehicleMessageMCodeRecoverMissMagTagFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERMISSMAGTAGSUCCESS: //M43
                    InterfaceService.SendVehicleMessageMCodeRecoverMissMagTagSuccess(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERAGVOUTRAIL: //M51
                    InterfaceService.SendVehicleMessageMCodeRecoverAgvOutRail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERAGVOUTRAILSUCCESS: //M52
                    InterfaceService.SendVehicleMessageMCodeRecoverAgvOutRailSuccess(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERAGVOUTRAILFAIL: //M53
                    InterfaceService.SendVehicleMessageMCodeRecoverAgvOutRailFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVAUTOSTART: //M61
                    InterfaceService.SendVehicleMessageMCodeAgvAutoStart(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVAUTOSTARTSUCCESS: //M62
                    InterfaceService.SendVehicleMessageMCodeAgvAutoStartSuccess(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVAUTOSTARTFAIL: //M63
                    InterfaceService.SendVehicleMessageMCodeAgvAutoStartFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVTURNPBSOFF: //M71
                    InterfaceService.SendVehicleMessageMCodeAgvTurnPbsOff(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVTURNPBSOFFSUCCESS: //M72
                    InterfaceService.SendVehicleMessageMCodeAgvTurnPbsOffSuccess(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVTURNPBSOFFFAIL: //M73
                    InterfaceService.SendVehicleMessageMCodeAgvTurnPbsOffFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERYAGVBACK: //M81
                    InterfaceService.SendVehicleMessageMCodeRecoveryAgvBack(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERYAGVBACKSUCCESS: //M82
                    InterfaceService.SendVehicleMessageMCodeRecoveryAgvBackSuccess(msgName, VehicleMsg);
                    break;
                case MCodeConsts.RECOVERYAGVBACKFAIL: //M83
                    InterfaceService.SendVehicleMessageMCodeRecoveryAgvBackFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVSENSORSONIC: //M91
                    InterfaceService.SendVehicleMessageMCodeAgvSensorSonic(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVSENSORSONICSUCCESS: //M92
                    InterfaceService.SendVehicleMessageMCodeAgvSensorSonicSuccess(msgName, VehicleMsg);
                    break;
                case MCodeConsts.AGVSENSORSONICFAIL: //M93
                    InterfaceService.SendVehicleMessageMCodeAgvSensorSonicFail(msgName, VehicleMsg);
                    break;
                case MCodeConsts.HMIVERSION: //M94
                    InterfaceService.SendVehicleMessageMCodeHmiVersion(msgName, VehicleMsg);
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
