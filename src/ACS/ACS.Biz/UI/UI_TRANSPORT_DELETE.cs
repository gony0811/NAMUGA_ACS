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
    public class UI_TRANSPORT_DELETE : BaseBizJob
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
            UiTransportDeleteMessageEx uiTransportDeleteMsg;
            TransferMessageEx transferMsg;
            int resultCode_timeOut = 99;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            ResourceService = (ResourceServiceEx)ApplicationContext.GetObject("ResourceService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");

            uiTransportDeleteMsg = InterfaceService.CreateUiTransportDeleteMessage(document);
            transferMsg = InterfaceService.CreateTransferDeleteMessage(uiTransportDeleteMsg);

            if (InterfaceService.CheckAndPopulateTransportCommand(transferMsg))
            {
                string[] source = transferMsg.TransportCommand.Source != null ? transferMsg.TransportCommand.Source.Split(':') : null;
                string[] dest = transferMsg.TransportCommand.Dest != null ? transferMsg.TransportCommand.Dest.Split(':') : null;

                //NG
                if (source == null || dest == null || source.Length < 2 || dest.Length < 2)
                {
                    //20190830 KSB
                    if (transferMsg.TransportCommand.Id.StartsWith("K"))
                    {
                        transferMsg.SourceMachine = source[0];
                        transferMsg.SourceUnit = source[1];
                        transferMsg.DestMachine = source[0];
                        transferMsg.DestUnit = source[1];
                    }
                    else if (transferMsg.TransportCommand.Id.StartsWith("R"))
                    {
                        transferMsg.SourceMachine = dest[0];
                        transferMsg.SourceUnit = dest[1];
                        transferMsg.DestMachine = dest[0];
                        transferMsg.DestUnit = dest[1];
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    transferMsg.SourceMachine = source[0];
                    transferMsg.SourceUnit = source[1];
                    transferMsg.DestMachine = source[0];
                    transferMsg.DestUnit = source[1];
                }
                if (TransferService.ExistTransportCommand(transferMsg))
                {
                    if (ResourceService.CheckVehicleByTransportCommand(transferMsg))
                    {
                        TransferService.UpdateVehicleTransportCommandEmpty(transferMsg);
                        TransferService.UpdateVehiclePathEmpty(transferMsg);
                        TransferService.UpdateVehicleAcsDestNodeIdEmpty(transferMsg);
                        ResourceService.ChangeVehicleTransferStateToNotAssigned(transferMsg);
                        MaterialService.DeleteCarrierByCommand(transferMsg);
                        TransferService.DeleteTransportCommand(transferMsg);
                        
                        InterfaceService.ReplyMoveCmdByResultCode(transferMsg, 99);

                        if (ResourceService.SearchWaitPoint(transferMsg))
                        {                          
                            InterfaceService.SendGoWaitpoint(transferMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(transferMsg);
                        }
                        else
                        {
                            InterfaceService.SendGoWaitpoint0000(transferMsg);
                            TransferService.UpdateVehicleAcsDestNodeIdTo0000(transferMsg);
                        }

                        ResourceService.ChangeVehicleProcessStateToIdle(transferMsg);
                    }
                    // 1020 LSJ VEHICLE 할당이 안된 JOB 도 삭제가 되도록 수정
                    //else
                    //{
                    //    return 0;
                    //}
                }
                else
                {
                    return 0;
                    //Terminate Function 추가 필요
                }
            }
            else
            {
                return 0;

                //Terminate Function 추가 필요
            }

            
            MaterialService.DeleteCarrierByCommand(transferMsg);
            TransferService.DeleteTransportCommand(transferMsg);
            //InterfaceService.ReplyMessageToUi(uiTransportDeleteMsg);
            InterfaceService.ReplyMoveCmdByResultCode(transferMsg, resultCode_timeOut);
            
            if(ResourceService.SearchWaitPoint(transferMsg))
            {
                InterfaceService.SendGoWaitpoint(transferMsg);
                TransferService.UpdateVehicleAcsDestNodeIdToWaitPoint(transferMsg);
            }
            else
            {
                InterfaceService.SendGoWaitpoint0000(transferMsg);
                TransferService.UpdateVehicleAcsDestNodeIdTo0000(transferMsg);
            }

            Thread.Sleep(1000);

            ResourceService.ChangeVehicleProcessStateToIdle(transferMsg);

            return 0;
        }
    }
}
