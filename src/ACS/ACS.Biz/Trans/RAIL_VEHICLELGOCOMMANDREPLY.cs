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


namespace ACS.Biz.Trans
{
    class RAIL_VEHICLELGOCOMMANDREPLY : BaseBizJob
    {
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
            XmlDocument rail_vehiclelgocommandreply = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehiclelgocommandreply);
            TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
            ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            if(AlarmService.CheckVehicleHaveAlarm(vehicleMsg))
            {
                InterfaceService.ReportAlarmClearReport(vehicleMsg);
                //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                ResourceService.DeleteUIInform(vehicleMsg);
            }
            ResourceService.ChangeVehicleCrossWaitToGOING(vehicleMsg);

            return 0;
        }
    }
}
