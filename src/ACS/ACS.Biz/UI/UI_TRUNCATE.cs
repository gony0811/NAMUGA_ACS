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
    public class UI_TRUNCATE : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public HistoryServiceEx HistoryService;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument document = (XmlDocument)args[0];
            UiTruncateMessageEx uiTruncateMsg;

            InterfaceService = (InterfaceServiceEx)ApplicationContext.GetObject("InterfaceService");
            HistoryService = (HistoryServiceEx)ApplicationContext.GetObject("HistoryService");

            uiTruncateMsg = InterfaceService.CreateUiTruncateMessage(document);
            HistoryService.TruncatePartitionTable(uiTruncateMsg);
            InterfaceService.ReplyMessageToUi(uiTruncateMsg);

            return 0;
        }
    }
}
