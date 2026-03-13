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
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Transfer;
using ACS.Framework.Material.Model;
using ACS.Framework.Path;
using ACS.Framework.Path.Model;
using ACS.Framework.Alarm;
using ACS.Framework.History;
using ACS.Framework.Resource;
using ACS.Framework.Application;
using ACS.Framework.Material;
using ACS.Framework.Message;
using ACS.Framework.Message.Model.Control;
using System.Xml;

namespace ACS.Biz
{
    public class CONTROL_UNLOADNIOCONTROLLABLE : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ControlService ControlService_;

        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            ControlMessageEx controlMsg;
            Boolean result;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ControlService_ = LifetimeScope.Resolve<ControlService>();

            controlMsg = InterfaceService.CreateControlMessage(document);
            result = ControlService_.Control(controlMsg);

            //200622 Change NIO Logic About ES.exe does not restart
            //InterfaceService.ReplyMessage(controlMsg);
            //

            return 0;
        }
    }
}
