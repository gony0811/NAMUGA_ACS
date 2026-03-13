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

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_ALARMREPORT : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_alarmreport = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
           
            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_alarmreport);

            if(ResourceService.CheckVehicle(vehicleMsg))
            {
                TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                if(AlarmService.CreateAlarm(vehicleMsg))
                {
                    if(AlarmService.CheckHeavyAlarm(vehicleMsg))
                    {
                        ResourceService.CreateHeavyAlarmInform(vehicleMsg);
                    }

                    ResourceService.UpdateVehicleAlarmStateToAlarm(vehicleMsg);
                    InterfaceService.ReportAlarmReport(vehicleMsg);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }

            return 0;
        }
    }
}
