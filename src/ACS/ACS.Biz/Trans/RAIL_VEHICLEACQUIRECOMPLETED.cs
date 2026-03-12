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
using ACS.Framework.Message.Model.Ui;
using ACS.Workflow;
using log4net;
using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEACQUIRECOMPLETED : BaseBizJob
    {
        protected static ILog logger = LogManager.GetLogger(typeof(RAIL_VEHICLEACQUIRECOMPLETED));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        private IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("TS RAIL_VEHICLEACQUIRECOMPLETED START=====================================");
            XmlDocument rail_vehicleacquirecompleted = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicleacquirecompleted);
            
            if (ResourceService.CheckVehicle(vehicleMsg))
            {
                //if (vehicleMsg.Vehicle.BayId.Equals("S0_CP"))
                //{
                //    this.WorkflowManager.Execute("RAIL-VEHICLEACQUIRECOMPLETEDSO", rail_vehicleacquirecompleted);
                //    return 0;
                //}

                //20190826 KSB
                //KIT-MAT(DLAMI.MTP(UV), CP) 자재 투입은 PLC Call 방식으로 오토콜 사용하지 않음
                if (vehicleMsg.Vehicle.BayId.Equals("KIT-MAT"))
                {
                    this.WorkflowManager.Execute("RAIL-VEHICLEACQUIRECOMPLETEDKMT", rail_vehicleacquirecompleted);
                    return 0;
                }

                logger.Debug("RAIL_VEHICLEACQUIRECOMPLETED Normal Sequence Step.1 CheckVehicle finish(TRUE)");

                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleEvent]= RAIL-VEHICLEACQUIRECOMPLETED");

                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");
                logger.Debug(">>>>add NA_H_VEHICLEHISTORY += RAIL-VEHICLEACQUIRECOMPLETED");

                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE  [EventTime] = $EventTime");

                if (AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
                {
                    InterfaceService.ReportAlarmClearReport(vehicleMsg);
                    logger.Debug("RAIL_VEHICLEACQUIRECOMPLETED Normal Sequence Step.2 ReportAlarmClearReport finish");

                    //AlarmService.DeleteAlarmByVehicle(vehicleMsg);

                    //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                    AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                    logger.Debug(">>>>update NA_R_VEHICLE [AlarmState] = NOALARM");
                    logger.Debug(">>>>add NA_H_VEHICLEHISTORY += RAIL-VEHICLEACQUIRECOMPLETED");

                    ResourceService.DeleteUIInform(vehicleMsg);
                }
                logger.Debug("RAIL_VEHICLEACQUIRECOMPLETED Normal Sequence Step.3 MOVE TO VEHICLE_ACQUIRECOMPLETED");
                this.WorkflowManager.Execute("VEHICLE_ACQUIRECOMPLETED", vehicleMsg);

            }
            else
            {
                logger.Debug("RAIL_VEHICLEACQUIRECOMPLETED Abnormal Case CheckVehicle finish(FALSE)");
                logger.Debug("TS RAIL_VEHICLEACQUIRECOMPLETED End======================================");
                return 0;
            }
            logger.Debug("TS RAIL_VEHICLEACQUIRECOMPLETED End=====================================");
            return 0;
        }
    }
}
