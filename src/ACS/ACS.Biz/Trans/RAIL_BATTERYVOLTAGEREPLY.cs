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

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_BATTERYVOLTAGEREPLY : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
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
            XmlDocument rail_batteryvoltagereply = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            AlarmService = (AlarmServiceEx)ApplicationContext.GetObject("AlarmService");
            WorkflowManager = (IWorkflowManager)ApplicationContext.GetObject("WorkflowManager");
            
            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_batteryvoltagereply);
            TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
            ResourceService.UpdateVehicleEventTime(vehicleMsg);

            if(ResourceService.UpdateVehicleVoltage(vehicleMsg))
            {
                AlarmService.ClearLowBatteryAlarm(vehicleMsg);

                return 0;
                //return밑에 JAVA에 로직이 있음
                //if(ResourceService.CheckVehicleBatteryVoltage(vehicleMsg))
                //{
                //    return 0;
                //}
                //else
                //{
                //    if(TransferService.CheckTransportCommandsByVehicle(vehicleMsg))
                //    {
                //        return 0;
                //    }
                //    else
                //    {
                //        this.WorkflowManager.Execute("VEHICLE_CHANGE", vehicleMsg);
                //    }
                //}
            }
            else
            {
                return 0;
            }

            return 0;
        }
    }
}
