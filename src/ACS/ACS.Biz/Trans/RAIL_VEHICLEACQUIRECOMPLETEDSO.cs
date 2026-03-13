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
using ACS.Framework.Logging;
using System.Xml;


using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLEACQUIRECOMPLETEDSO : BaseBizJob
    {
        protected static Logger logger = Logger.GetLogger(typeof(RAIL_VEHICLEACQUIRECOMPLETEDSO));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        private IWorkflowManager WorkflowManager;
        private IResourceManagerEx ResourceManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("TS RAIL_VEHICLEACQUIRECOMPLETEDSO START=====================================");
            XmlDocument rail_vehicleacquirecompletedso = (XmlDocument)args[0];
            TransferMessageEx transferMessage = new TransferMessageEx();
            
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            int temp = 1;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();
            ResourceManager = LifetimeScope.Resolve<IResourceManagerEx>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicleacquirecompletedso);

            //동일 Vehicle로 생성된 Job이 있으면 DB에서 삭제. (Cancel 보고는 하지 않음)
            TransferService.DeleteTransportCommandsameVehicleID(vehicleMsg);


            LocationEx sourceLocation = ResourceManager.GetLocationByStationId(vehicleMsg.NodeId);
            string[] port = sourceLocation.PortId.Split(':');
            string createTCMD = string.Format("S0_{0}_{1}_D", vehicleMsg.Vehicle.Id, DateTime.Now.ToString("yyyyMMddHHmmssfffff"));
            vehicleMsg.TransportCommandId = createTCMD;

            //Create Transfer cmd
            {
                transferMessage.MessageName = "MOVECMD";
                transferMessage.TransportCommandId = createTCMD;
                transferMessage.CarrierId = createTCMD;
                transferMessage.CarrierType = "TRAY";
                transferMessage.Description = "";
                transferMessage.BayId = "S0";
                transferMessage.SourceMachine = port[0];
                transferMessage.SourceUnit = port[1];
                transferMessage.DestMachine = "";
                transferMessage.DestUnit = "";
                transferMessage.VehicleId = vehicleMsg.VehicleId;
                transferMessage.Time = DateTime.Now.ToString();
                 int.TryParse(vehicleMsg.Priority, out temp );
                transferMessage.Priority = temp;
                TransferService.CreateTransportCommandAndVehicleAssign(transferMessage);
                
                logger.Debug("RAIL_VEHICLEACQUIRECOMPLETEDSO Normal Sequence Step.1 Create Transfercommand success");
            }

            ResourceService.ChangeVehicleTransferStateToAssigned(vehicleMsg);
            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = ASSIGNED in S0 area");

            //ResourceService.ChangeVehicleTransportCommandId(vehicleMsg);
            TransferService.UpdateVehicleTransportCommand(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [TransportcommandId] in S0 area");

            TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
            logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleEvent]= RAIL-RAIL_VEHICLEACQUIRECOMPLETEDSO");

            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");
            logger.Debug(">>>>add NA_H_VEHICLEHISTORY += RAIL-VEHICLEACQUIRECOMPLETED");

            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE  [EventTime] = $EventTime");

            if (AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
            {
                InterfaceService.ReportAlarmClearReport(vehicleMsg);
                logger.Debug("RAIL_VEHICLEACQUIRECOMPLETEDSO Normal Sequence Step.2 ReportAlarmClearReport finish");

                //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [AlarmState] = NOALARM");
                logger.Debug(">>>>add NA_H_VEHICLEHISTORY += RAIL-VEHICLEACQUIRECOMPLETED");

                ResourceService.DeleteUIInform(vehicleMsg);
            }
            //logger.Debug("RAIL_VEHICLEACQUIRECOMPLETEDSO Normal Sequence Step.3 MOVE TO VEHICLE_ACQUIRECOMPLETED");
            //this.WorkflowManager.Execute("VEHICLE_ACQUIRECOMPLETED", vehicleMsg);

            TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] =''");
            logger.Debug(">>>>update NA_H_VEHICLEHISTORY");

            ResourceService.ChangeVehicleTransferStateToAcquireComplete(vehicleMsg);
            logger.Debug(">>>>update NA_R_VEHICLE [TransferState] ='ACQUIRE_COMPLETE'");
            logger.Debug(">>>>update NA_H_VEHICLEHISTORY");

            TransferService.ChangeTransportCommandStateToTransferingDest(vehicleMsg);
            logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] ='TRANSFERRING_DEST'");
            logger.Debug(">>>>update NA_T_TRANSPORTCMD [LoadedTime] ='LoadedTime'");


            if (InterfaceService.CheckTransferCommand(vehicleMsg))
            {
                logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.2 CheckTransferCommand finish(True)");

                InterfaceService.ReportAGVloadComplete(vehicleMsg);
                logger.Debug("VEHICLEACQUIRECOMPLETED Normal Sequence Step.3 ReportAGVloadComplete(PICKUP) to HOST finish");
            }


                logger.Debug("TS RAIL_VEHICLEACQUIRECOMPLETEDSO End=====================================");
            return 0;
        }
    }
}
