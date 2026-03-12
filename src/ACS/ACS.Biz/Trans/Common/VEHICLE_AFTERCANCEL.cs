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
using System.Xml;

namespace ACS.Biz.Trans.Common
{
    public class VEHICLE_AFTERCANCEL : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;


        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            VehicleMessageEx vehicleMessage = (VehicleMessageEx)args[0];
            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            VehicleInterfaceService = (VehicleInterfaceServiceEx)ApplicationContext.GetObject("VehicleInterfaceService");
            DataHandlingService = (DataHandlingServiceEx)ApplicationContext.GetObject("DataHandlingService");

            if(ResourceService.GetQueuedTransportCommandbyVehicle(vehicleMessage))
            {
                TransferService.ChangeTransferCommandVehicleId(vehicleMessage);
                ResourceService.ChangeVehicleTransportCommandId(vehicleMessage);
                TransferService.ChangeTransportCommandStateToAssigned(vehicleMessage);
                ResourceService.ChangeVehicleProcessStateToRun(vehicleMessage);
                TransferService.CreateTransportCommandRequest(vehicleMessage);

                //sendToAGV C CODE SOURCE로 날리자
                InterfaceService.SendTransportMessageSource(vehicleMessage);
                System.Threading.Thread.Sleep(5000);

                if(TransferService.IsTransportCommandRequestReplied(vehicleMessage))
                {
                    TransferService.UpdateVehiclePath(vehicleMessage);
                    TransferService.UpdateTransportCommandPath(vehicleMessage);
                    TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);
                }
                else
                {
                    FALSE_IsTransportCommandRequestReplied1(vehicleMessage);
                }


            }
            else
            {
                if(ResourceService.SearchWaitPoint(vehicleMessage))
                {
                    InterfaceService.SendGoWaitpoint(vehicleMessage);
                    TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMessage);
                }
                else
                {
                    InterfaceService.SendGoWaitpoint0000(vehicleMessage);
                    TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMessage);
                }

            }
        
            return 0;

        }

        public void FALSE_IsTransportCommandRequestReplied1(VehicleMessageEx vehicleMessage)
        {
            TransferService.CreateTransportCommandRequest(vehicleMessage);
            InterfaceService.SendTransportMessageSource(vehicleMessage);

            System.Threading.Thread.Sleep(5000);

            if(TransferService.IsTransportCommandRequestReplied(vehicleMessage))
            {
                TransferService.UpdateVehiclePath(vehicleMessage);
                TransferService.UpdateTransportCommandPath(vehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);
            }
            else
            {
                FALSE_IsTransportCommandRequestReplied2(vehicleMessage);
            }
        }

        public void FALSE_IsTransportCommandRequestReplied2(VehicleMessageEx vehicleMessage)
        {
            TransferService.CreateTransportCommandRequest(vehicleMessage);
            InterfaceService.SendTransportMessageSource(vehicleMessage);

            System.Threading.Thread.Sleep(5000);

            if(TransferService.IsTransportCommandRequestReplied(vehicleMessage))
            {
                TransferService.UpdateVehiclePath(vehicleMessage);
                TransferService.UpdateTransportCommandPath(vehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);
            }
            else
            {
                FALSE_IsTransportCommandRequestReplied3(vehicleMessage);
            }

        }

        public void FALSE_IsTransportCommandRequestReplied3(VehicleMessageEx vehicleMessage)
        {
            TransferService.CreateTransportCommandRequest(vehicleMessage);
            InterfaceService.SendTransportMessageSource(vehicleMessage);

            System.Threading.Thread.Sleep(5000);
            if (TransferService.IsTransportCommandRequestReplied(vehicleMessage))
            {
                TransferService.UpdateVehiclePath(vehicleMessage);
                TransferService.UpdateTransportCommandPath(vehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);
            }
            else
            {
                FALSE_IsTransportCommandRequestReplied4(vehicleMessage);
            }
        }

        public void FALSE_IsTransportCommandRequestReplied4(VehicleMessageEx vehicleMessage)
        {
            TransferService.ChangeTransferCommandVehicleIdEmpty(vehicleMessage);
            ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
            TransferService.ChangeTransportCommandStateToQueued(vehicleMessage);
            ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
            ResourceService.ChangeVehicleConnectionStateToDisconnect(vehicleMessage);
        }
    }
}
