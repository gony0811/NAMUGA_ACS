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
    class RAIL_CARRIERREMOVED : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
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
            XmlDocument rail_carrierremoved = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_carrierremoved);
            iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);

            ResourceService.UpdateVehicleEventTime(vehicleMsg);
            ResourceService.CreateCarrierInform(vehicleMsg);
            if(InterfaceService.CheckTransferCommand(vehicleMsg))
            {
                //20190909 KSB : AGV에서 제품이 없는데, AGV에서  CarrierRemoved = "50" 보고하면 무시하는 로직
                if (vehicleMsg.Vehicle.TransferState.Equals("ASSIGNED") && vehicleMsg.Vehicle.FullState.Equals("EMPTY"))
                {
                    return 0;
                }
                //KIT-MAT(DLAMI.MTP(UV), CP) 자재 투입은 PLC Call 방식으로 오토콜 사용하지 않고, 
                //작업자가 목적지 도착전 제거하면 강제 Complete 처리하는 로직 추가함
                // 20200208 LSJ 현재 사용 Bay 없음, XmlDocument 형변환 Error
                if (vehicleMsg.Vehicle.BayId.Equals("KIT-MAT"))
                {
                    //object aa = "RAIL-VEHICLEDEPOSITCOMPLETED";
                    //XmlDocument rail_carrierremove = (XmlDocument)aa;
                    this.WorkflowManager.Execute("RAIL-VEHICLEDEPOSITCOMPLETEDKMT", rail_carrierremoved);
                    return 0;
                }

                // 20191115
                // 목적지 도착 후 수작업 제거 시 완료 처리 (목적지 도착 전 수작업 제거는 Error 로 유지)

                // 20200208 LSJ Error 발생 주석처리
                //string acsDestNodeId = vehicleMsg.Vehicle.AcsDestNodeId;
                //if (vehicleMsg.Vehicle.CurrentNodeId.Equals(acsDestNodeId))
                //{
                //    object aa = "RAIL-VEHICLEDEPOSITCOMPLETED";
                //    XmlDocument rail_carrierremove = (XmlDocument)aa;
                //    this.WorkflowManager.Execute("RAIL_VEHICLEDEPOSITCOMPLETED", (XmlDocument)rail_carrierremove);
                //    return 0;
                //}

                ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                MaterialService.DeleteCarrier(vehicleMsg);
                TransferService.DeleteTransportCommand(vehicleMsg);
                TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                InterfaceService.requestAGVJobCancel(vehicleMsg);
                if(ResourceService.SearchWaitPoint(vehicleMsg))
                {
                    InterfaceService.SendGoWaitpoint(vehicleMsg);
                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                }
                else
                {
                    InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                    TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                }
                
                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
            }
            else
            {
                if(ResourceService.SearchWaitPoint(vehicleMsg))
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

            return 0;
        }
    }
}
