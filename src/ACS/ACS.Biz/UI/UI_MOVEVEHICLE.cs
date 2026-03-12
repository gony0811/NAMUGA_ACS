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
    public class UI_MOVEVEHICLE : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public TransferServiceEx TransferService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            UiMoveVehicleMessageEx uiMoveVehicleMeg;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            TransferService = (TransferServiceEx)ApplicationContext.GetObject("TransferService");

            uiMoveVehicleMeg = InterfaceService.CreateUiMoveVehicleMessage(document);
            InterfaceService.SendMoveMessageTarget(uiMoveVehicleMeg);
            TransferService.UpdateVehicleAcsDestNodeId(uiMoveVehicleMeg);
            //InterfaceService.ReplyMessageToUi(uiMoveVehicleMeg);

            return 0;
        }
    }
}
