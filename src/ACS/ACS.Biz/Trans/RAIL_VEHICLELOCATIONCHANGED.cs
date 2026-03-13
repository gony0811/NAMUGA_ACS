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

namespace ACS.Biz.Trans
{
    class RAIL_VEHICLELOCATIONCHANGED : BaseBizJob
    {
        protected static Logger logger = Logger.GetLogger("OCODELog");
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public HistoryServiceEx HistoryService;
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
            try
            {
                XmlDocument rail_vehiclelocationchanged = (XmlDocument)args[0];
                VehicleMessageEx vehicleMsg;
                int iCount;
                bool sendReport = false;
                bool isOCodeBay = false;
                InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
                ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
                MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
                TransferService = LifetimeScope.Resolve<TransferServiceEx>();
                HistoryService = LifetimeScope.Resolve<HistoryServiceEx>();
                AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
                WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

                vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehiclelocationchanged);
                if (ResourceService.CheckVehicle(vehicleMsg))
                {
                    TransferService.UpdateTransportCommandVehicleEvent(vehicleMsg);
                    ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                    ResourceService.UpdateVehicleEventTime(vehicleMsg);

                    if (ResourceService.CheckSpecialMismatchAndFly(vehicleMsg))
                    {
                        //Vehicle Mismatch and Fly check
                        ResourceService.CheckVehicleNodeMismatchAndFly(vehicleMsg);
                    }

                    //20200808 KSG : CheckVehicleHaveAlarm으로 변경
                    //check that vehicle has alarm by VehicleId
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

                    //Search ORDER BAY configuration
                    isOCodeBay = ResourceService.CheckSpecialOrderBay(vehicleMsg);

                    if (ResourceService.IsRegistedNode(vehicleMsg))
                    {
                        if (ResourceService.IsCrossStartNode(vehicleMsg))   // Cross start Node 이면 true
                        {
                            if (ResourceService.CheckVehicleCrossWaitByStartNodeId(vehicleMsg))  // Cross Node에 가고 있는 AGV가 있으면 True, 없으면 False
                            {
                                if (ResourceService.IsAlreadyGoingVehicleCrossWait(vehicleMsg)) // Cross Node에 가고 있는 AGV가 내 자신이면 True, 아니면 False.
                                {

                                    this.WorkflowManager.Execute("COMMON_SENDPERMIT", vehicleMsg);
                                    return 0;
                                }
                                else
                                {
                                    ResourceService.AddVehicleCrossWaitToWAIT(vehicleMsg);
                                    return 0;
                                }
                            }
                            else
                            {
                                ResourceService.AddVehicleCrossWaitToWAIT(vehicleMsg);  // Cross Node에 이미 진행중인 AGV가 있으므로 해당 AGV는 대기
                                return 0;
                            }
                        }
                        else
                        {
                            if (ResourceService.IsCrossEndNode(vehicleMsg)) // Cross End Node 이면 True
                            {
                                if (ResourceService.CheckSpecialCrossWaitHistory(vehicleMsg))
                                {
                                    HistoryService.CreateVehicleCrossWaitHistory(vehicleMsg);
                                }

                                iCount = ResourceService.DeleteVehicleCrossWaitByVehicleId(vehicleMsg);
                                return 0;
                            }
                            else
                            {
                                if (vehicleMsg.Vehicle.BayId.Equals("S0_CP"))
                                {
                                    ResourceService.ChangeVehicleLocation(vehicleMsg);
                                    return 0;
                                }

                                ResourceService.ChangeVehicleLocation(vehicleMsg);
                                logger.Debug("TS RAIL_VEHICLELOCATIONCHANGED Current Node : " + vehicleMsg.NodeId);


                                //Check Current Vehicle O-CODE Bay or Not
                                if (isOCodeBay)
                                {              
                                    // tttttttttttttttttttttttt
                                    VehicleMessageEx orderMsg = TransferService.CheckPathAndReCalculate(vehicleMsg);

                                    if (orderMsg != null)
                                    {
                                        InterfaceService.SendVehicleOrderMessage(orderMsg);
                                    }
                                }

                                ResourceService.ChangeVehicleProcessingState(vehicleMsg);
                                if (ResourceService.CheckVehicleBayByNodeId(vehicleMsg))
                                {
                                    if (TransferService.CheckAndUpdateLoadArrivedTimeOrUnloadArrivedTime(vehicleMsg))
                                    {
                                        if (ResourceService.CheckVehicleCurrentNodeIsSource(vehicleMsg))
                                        {
                                            logger.Debug("==========================================================================");
                                            logger.Debug("TS RAIL_VEHICLELOCATIONCHANGED Start");

                                            logger.Debug("RAIL_VEHICLELOCATIONCHANGED CheckVehicleCurrentNodeIsSource finish(True)");

                                            InterfaceService.ReportAGVArrivedSourcePort(vehicleMsg);
                                            logger.Debug("RAIL_VEHICLELOCATIONCHANGED Report AGVArrivedSourcePort(TYPE_SOURCE_ARRIVED) to HOST finish(True)");

                                            logger.Debug("TS RAIL_VEHICLELOCATIONCHANGED End");
                                            logger.Debug("==========================================================================");
                                        }
                                    }
                                    else
                                    {
                                        if (ResourceService.CheckVehicleCurrentNodeIsDest(vehicleMsg))
                                        {
                                            logger.Debug("==========================================================================");
                                            logger.Debug("TS RAIL_VEHICLELOCATIONCHANGED Start");

                                            logger.Debug("RAIL_VEHICLELOCATIONCHANGED CheckVehicleCurrentNodeIsDest finish(True)");

                                            InterfaceService.ReportAGVArrivedDestPort(vehicleMsg);
                                            logger.Debug("RAIL_VEHICLELOCATIONCHANGED Report AGVArrivedDestPort(TYPE_DEST_ARRIVED) to HOST finish");

                                            logger.Debug("TS RAIL_VEHICLELOCATIONCHANGED End");
                                            logger.Debug("==========================================================================");


                                        }
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
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }

            }
            catch (Exception e)
            {
                string msg = e.StackTrace + ", " + e.ToString();
                Logger.Error(msg);
            }

            return 0;
        }       
    }
}
