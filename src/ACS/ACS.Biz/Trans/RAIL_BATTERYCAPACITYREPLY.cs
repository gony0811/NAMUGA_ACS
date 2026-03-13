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

using System.Xml;

namespace ACS.Biz.Trans
{
    class RAIL_BATTERYCAPACITYREPLY : BaseBizJob
    {
        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_batterycapacityreply = (XmlDocument)args[0];
            return 0;
        }
    }
}
