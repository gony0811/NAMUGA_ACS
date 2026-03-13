using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Autofac;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Workflow;

using System.Xml;
using ACS.Framework.Logging;

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEDEPOSITCOMPLETED : BaseBizJob
    {
        protected static Logger logger = Logger.GetLogger(typeof(RAIL_VEHICLEDEPOSITCOMPLETED));
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
            logger.Debug("==========================================================================");
            logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETED Start");
            XmlDocument rail_vehicledepositcompleted = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicledepositcompleted);

            if (ResourceService.CheckVehicle(vehicleMsg))
            {
                //if (vehicleMsg.Vehicle.BayId.Equals("S0_CP"))
                //{
                //    this.WorkflowManager.Execute("RAIL-VEHICLEDEPOSITCOMPLETEDSO", rail_vehicledepositcompleted);
                //    return 0;
                //}

                //20190826 KSB
                //KIT-MAT(DLAMI.MTP(UV), CP) 자재 투입은 PLC Call 방식으로 오토콜 사용하지 않음
                if (vehicleMsg.Vehicle.BayId.Equals("KIT-MAT"))
                {
                    this.WorkflowManager.Execute("RAIL-VEHICLEDEPOSITCOMPLETEDKMT", rail_vehicledepositcompleted);
                    return 0;
                }

                logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETED Normal Sequence Step.01 CheckVehicle (TRUE)");
                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleEvent] = vehicleEvent;");

                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");

                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [EventTime]");

                if (AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
                {
                    logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETED Normal Sequence Step.02 CheckAlarmClearMessage (TRUE)");
                    InterfaceService.ReportAlarmClearReport(vehicleMsg);
                    //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                    //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                    AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                    ResourceService.DeleteUIInform(vehicleMsg);
                }

                if (!ResourceService.CheckVehicleStateIsTransferDest(vehicleMsg))  return 0;

                this.WorkflowManager.Execute("VEHICLE_DEPOSITCOMPLETED", vehicleMsg);
                logger.Debug("RAIL_VEHICLEDEPOSITCOMPLETED Normal Sequence Step.03 MOVE to VEHICLE_DEPOSITCOMPLETED");
                //VEHICLE_DEPOSITCOMPLETED로 로직 이관
                //if(this.WorkflowManager.Execute("VEHICLE_DEPOSITCOMPLETED", vehicleMsg))
                //{
                //    return 0;
                //}
                //else
                //{
                //    if(ResourceService.SearchWaitPoint(vehicleMsg))
                //    {
                //        InterfaceService.SendGoWaitpoint(vehicleMsg);
                //        TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                //    }
                //    else
                //    {
                //        InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                //        TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                //    }

                //    Thread.Sleep(1000);

                //    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                //}
            }
            else
            {
                logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETED End");
                logger.Debug("==========================================================================");
                return 0;
            }
            logger.Debug("TS RAIL_VEHICLEDEPOSITCOMPLETED End");
            logger.Debug("==========================================================================");
            return 0;
        }
    }
}
