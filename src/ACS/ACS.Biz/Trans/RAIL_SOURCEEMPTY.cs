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
    class RAIL_SOURCEEMPTY : BaseBizJob
    {
        public Logger logger = Logger.GetLogger(typeof(RAIL_SOURCEEMPTY));
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        private IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            logger.Debug("TS RAIL_SOURCEEMPTY START========================================================");
            
            XmlDocument rail_sourceempty = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;
            Boolean iConnectionState;
            Boolean result;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_sourceempty);
            if(ResourceService.CheckVehicle(vehicleMsg))
            {
                logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.01 CheckVehicle (TRUE)");

                iConnectionState = ResourceService.ChangeVehicleConnectionStateToConnect(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [ConnectionState] = CONNECT");

                ResourceService.UpdateVehicleEventTime(vehicleMsg);
                logger.Debug(">>>>update NA_R_VEHICLE [EventTime] update");

                if(InterfaceService.CheckTransferCommand(vehicleMsg))
                {
                    logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.02 CheckTransferCommand (TRUE)");
                  
                    if(ResourceService.CheckTransportCommandSourceNodeByVehicleCurrentNode(vehicleMsg)) //Source 위치에 있으면
                    {
                        logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.03 CheckTransportCommandSourceNodeByVehicleCurrentNode (TRUE)");
                        
                        ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [TransferState] = NOTASSIGNED");

                        MaterialService.DeleteCarrier(vehicleMsg);
                        logger.Debug(">>>>delete NA_M_CARRIER");

                        TransferService.DeleteTransportCommand(vehicleMsg);
                        logger.Debug(">>>>delete NA_T_TRANSPORTCMD");

                        TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = ' '");

                        TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [TransportCommandId] = ' '");

                        TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [Path] = ' '");

                        InterfaceService.requestAGVJobCancel(vehicleMsg);
                        logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.04 requestAGVJobCancel");
                    

                        if(ResourceService.SearchWaitPoint(vehicleMsg))
                        {
                            logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.05 SearchWaitPoint (TRUE)");

                            InterfaceService.SendGoWaitpoint(vehicleMsg);
                            logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.06 SendGoWaitpoint");

                            TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(vehicleMsg);
                            logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = Wait_P");
                        }
                        else
                        {
                            logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.05 SearchWaitPoint (FALSE)");
                            InterfaceService.SendGoWaitpoint0000(vehicleMsg);
                            logger.Debug("RAIL_SOURCEEMPTY Normal Sequence Step.06 SendGoWaitpoint0000");

                            TransferService.UpdateVehicleAcsDestNodeIdTo0000(vehicleMsg);
                            logger.Debug(">>>>update NA_R_VEHICLE [AcsDestNodeId] = 9000");
                        }
                        ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                        logger.Debug(">>>>update NA_R_VEHICLE [ProcessState] = IDLE");
                    }
                    else
                    {
                        logger.Debug("RAIL_SOURCEEMPTY Abnormal Case CheckTransportCommandSourceNodeByVehicleCurrentNode (FALSE)");
                        
                        if(ResourceService.IsCurrentNodeDest(vehicleMsg)) //Dest 위치에 있으면
                        {
                            if(ResourceService.CheckVehicleFullStateEmpty(vehicleMsg)) //비어있으면 True
                            {
                                result = ResourceService.ChangeVehicleTransferStateToNotAssigned(vehicleMsg);
                                MaterialService.DeleteCarrier(vehicleMsg);
                                TransferService.DeleteTransportCommand(vehicleMsg);
                                TransferService.UpdateVehicleAcsDestNodeIdEmpty(vehicleMsg);
                                TransferService.UpdateVehicleTransportCommandEmpty(vehicleMsg);
                                TransferService.UpdateVehiclePathEmpty(vehicleMsg);
                                InterfaceService.requestAGVJobCancel(vehicleMsg);

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
                                ResourceService.ChangeVehicleProcessStateToIdle(vehicleMsg);
                            }
                            else //vehicle이 Full 상태이면
                            {
                                //expression 메세지 표시하는거 같은데
                                this.WorkflowManager.Execute("RAIL_DESTOCCUPIED", rail_sourceempty);
                            }
                        }
                        else
                        {
                            logger.Debug("TS RAIL_SOURCEEMPTY End========================================================");
                            return 0;
                        }
                    }
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
            }
            else
            {
                logger.Debug("TS RAIL_SOURCEEMPTY End========================================================");
                return 0;
            }
            logger.Debug("TS RAIL_SOURCEEMPTY End========================================================");
            return 0;
        }
    }
}
