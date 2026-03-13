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
using ACS.Framework.Transfer.Model;
using ACS.Workflow;

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_CARRIERLOADED : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public TransferServiceEx TransferService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;
        private IWorkflowManager WorkflowManager;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            //2018.10.04 KSG
            // CARRIER LOADED(M40) 로직 처리 
            // VEHICLE에 알수없는 CARRIER가 LOADING 되었을때 M40보고
            // 처리 로직
            // UI INFORM
            // JOB 수행중인 VEHICLE은 TRANSPORTCOMMAND를 QUEUED 상태로 원복
            // JOB 없는 경우는 WAIT POINT로 보냄

            XmlDocument rail_carrierloaded = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_carrierloaded);
            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);

            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            ResourceService.CreateCarrierInform(vehicleMsg);

            if (InterfaceService.CheckTransportCommand(vehicleMsg)) //&& InterfaceService.CheckTransferCommandState(TransferCommandEx)) 
            {
                //20190909 KSB : AGV에서 제품을 가지고 있는데, AGV에서 CarrierLoaded = "40" 보고하면 무시하는 로직
                if (vehicleMsg.Vehicle.TransferState.Equals("ACQUIRE_COMPLETE") && vehicleMsg.Vehicle.FullState.Equals("FULL"))
                {
                    return 0;
                }
                //20190913 KSB
                if (vehicleMsg.Vehicle.BayId.Equals("KIT-MAT"))
                {
                    object aa = "RAIL-VEHICLEDEPOSITCOMPLETED";
                    XmlDocument rail_carrierremove = (XmlDocument)aa;
                    this.WorkflowManager.Execute("RAIL-VEHICLEACQUIRECOMPLETEDKMT", (XmlDocument)rail_carrierremove);
                    return 0;
                }
                else
                {
                    ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                    TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                    TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                    TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                    TransferService.ChangeTransferCommandVehicleIdEmpty(vehicleMsg);
                    TransferService.ChangeTransportCommandStateToQueued(vehicleMsg);

                    //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
                    if (vehicleMsg.Vehicle.Vendor.Equals("RGV"))
                    {
                        InterfaceService.SendGoWaitpoint0000_RGV(vehicleMsg);
                        TransferService.UpdateVehicleAcsDestNodeIdTo0000_RGV(vehicleMsg);
                    }
                }
            }
            else
            {
                //20190913 KSB
                if (vehicleMsg.Vehicle.BayId.Equals("KIT-MAT"))
                {
                    return 0;
                }
                else
                {
                    //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
                    if (vehicleMsg.Vehicle.Vendor.Equals("RGV"))
                    {
                        InterfaceService.SendGoWaitpoint0000_RGV(vehicleMsg);
                        TransferService.UpdateVehicleAcsDestNodeIdTo0000_RGV(vehicleMsg);
                    }
                    else
                    {
                        if (ResourceService.SearchWaitPoint(vehicleMsg))
                        {
                            InterfaceService.SendGoWaitpoint(vehicleMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                        }
                        else
                        {
                            InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                        }
                    }
                }
            }
            return 0;
        }
    }
}
