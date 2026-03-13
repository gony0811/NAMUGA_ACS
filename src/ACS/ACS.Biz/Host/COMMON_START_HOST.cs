using System;
using Autofac;
using ACS.Framework.Logging;

namespace ACS.Biz.Host
{
    /// <summary>
    /// Host 프로세스(HS01_P) 시작 워크플로우.
    /// ApplicationInitializer에서 InvokeStartWorkflow("COMMON-START-HOST")로 호출됨.
    /// </summary>
    public class COMMON_START_HOST : BaseBizJob
    {
        public COMMON_START_HOST()
        {
        }

        public override int ExecuteJob(object[] args)
        {
            Logger.Info("HS START : Host bridge process initialized.");
            return 0;
        }
    }
}
