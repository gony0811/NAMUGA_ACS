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
    class RAIL_VEHICLESTART : BaseBizJob
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
            XmlDocument rail_vehiclestart = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            int iCount;
            bool sendReport = false;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehiclestart);
            //KSB
            //RGV 아닐경우 빠져나감
            if (!vehicleMsg.Vehicle.Vendor.Equals("RGV")) return 0;

            if (ResourceService.CheckVehicle(vehicleMsg))
            {
                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                ResourceService.UpdateVehicleEventTime(vehicleMsg);

                if (AlarmService.CheckVehicleHaveAlarm(vehicleMsg)) //항상 True일듯..
                {
                    if (AlarmService.CheckAlarmClearMessage(vehicleMsg)) //if(sendReport) 가 맞음. 다시 확인 필요
                    {
                        InterfaceService.ReportAlarmClearReport(vehicleMsg);
                    }
                    //AlarmService.DeleteAlarmByVehicle(vehicleMsg);
                    AlarmService.ClearAlarmAndSetAlarmTimeHistory(vehicleMsg);
                    //ResourceService.UpdateVehicleAlarmStateToNoAlarm(vehicleMsg);
                    ResourceService.DeleteUIInform(vehicleMsg);
                }
            }
            return 0;
        }
    }
}
