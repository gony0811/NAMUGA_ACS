using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using ACS.Framework.Base;
using ACS.Service;
using ACS.Framework.Message.Model;
using ACS.Framework.Transfer.Model;
using Autofac;
using ACS.Biz.Trans.Common;
using ACS.Framework.Logging;

namespace ACS.Biz.Trans.Daemon
{
    public class SCHEDULE_QUEUEJOB : BaseBizJob
    {
        //protected static ILog logger = LogManager.GetLogger(typeof(SCHEDULE_QUEUEJOB));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public VehicleInterfaceServiceEx VehicleInterfaceService;
        public DataHandlingServiceEx DataHandlingService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        bool RETURNS = false;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }
        public override int ExecuteJob(object[] args)
        {
            Logger.Debug("TS SCHEDULE_QUEUEJOB START========================================================");

            XmlDocument SCHEDULE_QUEUEJOB = (XmlDocument)args[0];

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            VehicleInterfaceService = LifetimeScope.Resolve<VehicleInterfaceServiceEx>();
            DataHandlingService = LifetimeScope.Resolve<DataHandlingServiceEx>();
            Logger.logManager = LifetimeScope.Resolve<ILogManager>();
            VehicleMessageEx vehicleMessage = InterfaceService.CreateVehicleMessageFromDaemon(SCHEDULE_QUEUEJOB);




            if (ResourceService.GetQueuedTransportCommandbyBayId(vehicleMessage))
            {
                Logger.Debug("SCHEDULE_QUEUEJOB Normal Sequence Step.1 GetQueuedTransportCommandbyBayId finish(true)");
                TransferService.ChangeTransferCommandVehicleId(vehicleMessage);
                Logger.Debug(">>>>update NA_T_TRANSPORTCMD [VehicleId] update ");

                ResourceService.ChangeVehicleTransportCommandId(vehicleMessage);
                Logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId] update");

                //KSB
                //PREASSIGN 추가
                //TransferService.ChangeTransportCommandStateToAssigned(vehicleMessage);
                //Logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = ASSIGNED");
                TransferService.ChangeTransportCommandStateToPreAssigned(vehicleMessage);
                Logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = PREASSIGNED");

                ResourceService.ChangeVehicleProcessStateToRun(vehicleMessage);
                Logger.Debug(">>>>update NA_R_VEHICLE [processingState] = RUN ");

                TransferService.CreateTransportCommandRequest(vehicleMessage);
                Logger.Debug(">>>>update NA_T_TRANSPORTCMD [JobID] = TransCommandID");

                InterfaceService.SendTransportMessageSource(vehicleMessage); //RAIL_CARRIERTRANSFER
                Logger.Debug("SCHEDULE_QUEUEJOB Normal Sequence Step.2 send a Source MSG 'RAIL_CARRIERTRANSFER' to ES finish");

                TransferService.UpdateVehicleAcsDestNodeId(vehicleMessage);
                Logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId]");

                // DateTime startTime = DateTime.Now;

                int count = 0;

                while (!TransferService.IsTransportCommandRequestReplied(vehicleMessage))
                {
                    //TimeSpan ts = DateTime.Now - startTime;
                    FALSE_IsTransportCommandRequestReplied1(vehicleMessage);

                    //if (ts.Seconds > 5)
                    if (count > 2)
                    {
                        Logger.Debug("SCHEDULE_QUEUEJOB Abnormal Case Retry > 3 : " + vehicleMessage.VehicleId);
                        TransferService.ChangeTransferCommandVehicleIdEmpty(vehicleMessage);
                        TransferService.ChangeTransportCommandStateToQueued(vehicleMessage);
                        ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);

                        //1023 LSJ
                        ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);

                        TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMessage);
                        ResourceService.ChangeVehicleConnectionStateToDisconnect(vehicleMessage);
                        Logger.Debug("TS SCHEDULE_QUEUEJOB End========================================================");

                        //1023 LSJ
                        TransferService.DeleteTransportCommandRequest(vehicleMessage);
                        return 0;
                    }
                    count++;
                    //System.Threading.Thread.Sleep(1000);
                }
                //KSB
                //AGV C-CODE REPLY 받으면 ASSIGN 하기 위함(AGV 는 QUEUE에서는 PREASSIGN 상태)
                TransferService.ChangeTransportCommandStateToAssigned(vehicleMessage);
                Logger.Debug(">>>>update NA_T_TRANSPORTCMD [State] = ASSIGNED");

                //20200818 KSG
                //SpecialConfig에서 Order 적용  Bay인지 확인
                if(ResourceService.CheckSpecialOrderBay(vehicleMessage))
                {
                    TransferService.UpdateJobExpectedTimeWhenQueue(vehicleMessage);
                }
                else
                {
                    TransferService.UpdateTransportCommandPath(vehicleMessage);
                    TransferService.UpdateVehiclePath(vehicleMessage);
                }               
            }
            else
            {
                Logger.Debug("SCHEDULE_QUEUEJOB Abnormal Case GetQueuedTransportCommandbyBayId finish(False)");
            }
            Logger.Debug("TS SCHEDULE_QUEUEJOB End========================================================");
            return 0;
        }

        public void FALSE_IsTransportCommandRequestReplied1(VehicleMessageEx vehicleMessage)
        {
            if (TransferService.IsSameTransportCommandVehicleId(vehicleMessage))
            {
                TransferService.CreateTransportCommandRequest(vehicleMessage);

                InterfaceService.SendTransportMessageSource(vehicleMessage);

                System.Threading.Thread.Sleep(5000);

                //if(TransferService.IsTransportCommandRequestReplied(vehicleMessage))
                //{
                //    TransferService.UpdateTransportCommandPath(vehicleMessage);
                //    TransferService.UpdateVehiclePath(vehicleMessage);
                //}
                //else
                //{
                //    ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);

                //    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
                //    //Terminate 코드 추가 필요
                //}

            }
            else
            {
                FALSE_IsSameTransportCommandVehicleId1(vehicleMessage);

            }
        }

        public void FALSE_IsSameTransportCommandVehicleId1(VehicleMessageEx vehicleMessage)
        {
            ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
            ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);

            //if(TransferService.IsSameTransportCommandVehicleId(vehicleMessage))
            //{
            //    TransferService.CreateTransportCommandRequest(vehicleMessage);
            //    InterfaceService.SendTransportMessageSource(vehicleMessage);

            //    System.Threading.Thread.Sleep(5000);

            //    if(TransferService.IsTransportCommandRequestReplied(vehicleMessage))
            //    {
            //        TransferService.UpdateTransportCommandPath(vehicleMessage);
            //        TransferService.UpdateVehiclePath(vehicleMessage);
            //    }
            //    else
            //    {
            //        if(TransferService.IsSameTransportCommandVehicleId(vehicleMessage))
            //        {
            //            TransferService.CreateTransportCommandRequest(vehicleMessage);
            //            InterfaceService.SendTransportMessageSource(vehicleMessage);

            //            System.Threading.Thread.Sleep(5000);

            //            if (TransferService.IsTransportCommandRequestReplied(vehicleMessage))
            //            {
            //                TransferService.UpdateTransportCommandPath(vehicleMessage);
            //                TransferService.UpdateVehiclePath(vehicleMessage);
            //            }
            //            else
            //            {
            //                FALSE_IsTransportCommandRequestReplied2(vehicleMessage);
            //            }
            //        }
            //        else
            //        {
            //            ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
            //            ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
            //            //Terminate 코드 추가필요
            //        }
            //    }
            //}
            //else
            //{
            //    ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
            //    ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
            //    //Terminate 코드 추가 필요
            //}
        }

        public void FALSE_IsTransportCommandRequestReplied2(VehicleMessageEx vehicleMessage)
        {
            if (TransferService.IsSameTransportCommandVehicleId(vehicleMessage))
            {
                ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
                if (TransferService.IsSameTransportCommandVehicleId(vehicleMessage))
                {
                    TransferService.ChangeTransferCommandVehicleIdEmpty(vehicleMessage);
                    TransferService.ChangeTransportCommandStateToQueued(vehicleMessage);
                }
                else
                { }

                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMessage);
                ResourceService.ChangeVehicleConnectionStateToDisconnect(vehicleMessage);
            }
            else
            {
                ResourceService.ChangeVehicleTransportCommandIdEmpty(vehicleMessage);
                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMessage);
                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMessage);
                //Terminate 코드 추가 필요
            }
        }

    }
}
