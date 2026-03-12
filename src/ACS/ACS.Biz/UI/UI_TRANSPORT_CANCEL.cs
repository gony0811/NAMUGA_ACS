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
    public class UI_TRANSPORT_CANCEL : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
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
            UiTransportCancelMessageEx uiTransportCancelMsg;
            TransferMessageEx transferMsg;
            int resultCode_timeOut = 99;
            Boolean result;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");
            MaterialService = (MaterialServiceEx)ApplicationContext.GetObject("MaterialService");

            uiTransportCancelMsg = InterfaceService.CreateUiTransportCancelMessage(document);
            transferMsg = InterfaceService.CreateTransferCancelMessage(uiTransportCancelMsg);
            result = InterfaceService.CheckAndPopulateTransportCommand(transferMsg);
            MaterialService.DeleteCarrierByCommand(transferMsg);
            TransferService.DeleteTransportCommand(transferMsg);
            InterfaceService.ReplyMoveCmdByResultCode(transferMsg, resultCode_timeOut);
            InterfaceService.ReplyMessageToUi(uiTransportCancelMsg);
            

            return 0;

        }
    }
}
