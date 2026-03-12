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

using System.Xml;

namespace ACS.Biz.UI
{
    public class UI_TRANSPORT : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public TransferServiceEx TransferService;
        public MaterialServiceEx MaterialService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            UiTransportMessageEx uiTransportMsg;
            TransferMessageEx transferMsg;
            int resultCode_timeOut = 99;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");

            uiTransportMsg = InterfaceService.CreateUiTransportMessage(document);
            transferMsg = InterfaceService.CreateTransferCommandMessage(uiTransportMsg);

            if(InterfaceService.CheckTransferMessage(transferMsg))
            {
                if(InterfaceService.CheckTransferCommandSourceDest(transferMsg))
                {
                    if(InterfaceService.CheckTransportCommand(transferMsg))
                    {
                        if(MaterialService.CreateCarrier(transferMsg))
                        {
                            if(TransferService.SearchPaths(transferMsg))
                            {
                                if(TransferService.CreateUITransportCommand(transferMsg))
                                {
                                    if(ResourceService.SearchSuitableVehicle(transferMsg))
                                    {
                                        //InterfaceService.ReplyMessageToUi(uiTransportMsg);
                                        TransferService.ChangeTransportCommandStateToAssigned(transferMsg);
                                        TransferService.UpdateVehicleTransportCommand(transferMsg);
                                        TransferService.CreateTransportCommandRequest(transferMsg);
                                        InterfaceService.SendTransportMessageSource(transferMsg);
                                        TransferService.UpdateTransportCommandPath(transferMsg);
                                        TransferService.UpdateVehicleAcsDestNodeId(transferMsg);
                                        //TransferService.UpdateVehiclePath(transferMsg);
                                        ResourceService.ChangeVehicleProcessStateToRun(transferMsg);
                                        
                                        Thread.Sleep(5000);

                                        if(TransferService.IsTransportCommandRequestReplied(transferMsg))
                                        {
                                            return 0;
                                        }
                                        else
                                        {
                                            TransferService.CreateTransportCommandRequest(transferMsg);
                                            InterfaceService.SendTransportMessageSource(transferMsg);
                                            
                                            Thread.Sleep(5000);

                                            if(TransferService.IsTransportCommandRequestReplied(transferMsg))
                                            {
                                                return 0;
                                            }
                                            else
                                            {
                                                TransferService.CreateTransportCommandRequest(transferMsg);
                                                InterfaceService.SendTransportMessageSource(transferMsg);
                                                
                                                Thread.Sleep(5000);
                                                
                                                if(TransferService.IsTransportCommandRequestReplied(transferMsg))
                                                {
                                                    return 0;
                                                }
                                                else
                                                {
                                                    TransferService.CreateTransportCommandRequest(transferMsg);
                                                    InterfaceService.SendTransportMessageSource(transferMsg);

                                                    Thread.Sleep(5000);

                                                    if(TransferService.IsTransportCommandRequestReplied(transferMsg))
                                                    {
                                                        return 0;
                                                    }
                                                    else
                                                    {
                                                        TransferService.CreateTransportCommandRequest(transferMsg);
                                                        InterfaceService.SendTransportMessageSource(transferMsg);

                                                        Thread.Sleep(5000);

                                                        if (TransferService.IsTransportCommandRequestReplied(transferMsg))
                                                        {
                                                            return 0;
                                                        }
                                                        else
                                                        {
                                                            ResourceService.ChangeVehicleTransferStateToNotAssigned(transferMsg);
                                                            MaterialService.DeleteCarrier(transferMsg);
                                                            TransferService.DeleteTransportCommand(transferMsg);
                                                            TransferService.UpdateVehicleTransportCommandEmpty(transferMsg);
                                                            TransferService.UpdateVehiclePathEmpty(transferMsg);
                                                            TransferService.UpdateVehicleAcsDestNodeIdEmpty(transferMsg);
                                                            TransferService.UpdateTransportCommandPathEmpty(transferMsg);
                                                            ResourceService.ChangeVehicleProcessStateToIdle(transferMsg);
                                                            ResourceService.ChangeVehicleConnectionStateToDisconnect(transferMsg);
                                                            InterfaceService.ReplyMoveCmdByResultCode(transferMsg, resultCode_timeOut);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        TransferService.ChangeTransportCommandStateToQueued(transferMsg);
                                        //InterfaceService.ReplyMessageToUi(uiTransportMsg);
                                    }
                                }
                                else
                                {
                                    MaterialService.DeleteCarrier(transferMsg);
                                    TransferService.DeleteTransportCommand(transferMsg);
                                    //InterfaceService.ReplyTransportJobCreateNGToUi(transferMsg);
                                }
                            }
                            else
                            {
                                MaterialService.DeleteCarrier(transferMsg);
                                //InterfaceService.ReplyTransportPathValidationNGToUi(transferMsg);
                            }

                        }
                        else
                        {
                            //InterfaceService.ReplyTransportCarrierCreateNGToUi(transferMsg);
                        }
                    }
                    else
                    {
                        //InterfaceService.ReplyTransportExistJobNGToUi(transferMsg);
                    }
                }
                else
                {
                    //InterfaceService.ReplyTransportStationValidationNGToUi(transferMsg);
                }
            }
            else
            {
                //InterfaceService.ReplyTransportMessageValidationNGToUi(transferMsg);
            }

            return 0;
        }
    }
}
