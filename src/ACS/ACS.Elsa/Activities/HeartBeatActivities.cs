using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Elsa.Bridge;
using ACS.Core.Application;
using ACS.Core.Application.Model;
using ACS.Core.History;
using ACS.Communication.Msb;
using ACS.Core.Logging;

namespace ACS.Elsa.Activities
{
    /// <summary>
    /// Primary 서버의 하드웨어 타입에 해당하는 Application 이름 목록을 조회.
    /// IApplicationManager.GetApplicationNamesByRunningHardware() 호출.
    /// </summary>
    [Activity("ACS.Control", "Get Applications By Hardware",
        "지정된 하드웨어 타입으로 실행 중인 Application 목록을 조회합니다.")]
    public class GetApplicationsByHardwareActivity : CodeActivity<List<string>>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_HEARTBEAT");

        [Input(Description = "하드웨어 타입 (PRIMARY / SECONDARY)")]
        public Input<string> HardwareType { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var hardwareType = HardwareType?.Get(context) ?? "PRIMARY";
            var result = new List<string>();

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var appManager = accessor.Resolve<IApplicationManager>();

                if (appManager == null)
                {
                    logger.Error("GetApplicationsByHardwareActivity: IApplicationManager를 resolve할 수 없습니다.");
                    context.Set(Result, result);
                    return;
                }

                IList names = appManager.GetApplicationNamesByRunningHardware(hardwareType);
                if (names != null)
                {
                    foreach (var name in names)
                    {
                        if (name != null)
                            result.Add(name.ToString());
                    }
                }

                logger.Info($"GetApplicationsByHardwareActivity: {hardwareType} 서버의 앱 {result.Count}개 조회됨.");
            }
            catch (Exception ex)
            {
                logger.Error($"GetApplicationsByHardwareActivity 실패: {ex.Message}", ex);
            }

            context.Set(Result, result);
        }
    }

    /// <summary>
    /// 지정된 Application에 CONTROL-HEARTBEAT 메시지를 동기 전송하고 응답 여부를 반환.
    /// ISynchronousMessageAgent.Request()를 timeout과 함께 호출.
    /// </summary>
    [Activity("ACS.Control", "Send HeartBeat",
        "대상 Application에 CONTROL-HEARTBEAT 메시지를 전송하고 응답 여부를 반환합니다.")]
    public class SendHeartBeatActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_HEARTBEAT");

        [Input(Description = "대상 Application 이름")]
        public Input<string> ApplicationName { get; set; }

        [Input(Description = "HeartBeat 메시지 XmlDocument")]
        public Input<XmlDocument> HeartBeatDocument { get; set; }

        [Input(Description = "응답 대기 타임아웃 (밀리초)")]
        public Input<long> Timeout { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var appName = ApplicationName?.Get(context);
            var document = HeartBeatDocument?.Get(context);
            var timeout = Timeout?.Get(context) ?? 5000L;

            if (string.IsNullOrEmpty(appName) || document == null)
            {
                logger.Error("SendHeartBeatActivity: ApplicationName 또는 Document가 null입니다.");
                context.Set(Result, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var syncAgent = accessor.Resolve<ISynchronousMessageAgent>();
                var appManager = accessor.Resolve<IApplicationManager>();

                if (syncAgent == null || appManager == null)
                {
                    logger.Error("SendHeartBeatActivity: 서비스를 resolve할 수 없습니다.");
                    context.Set(Result, false);
                    return;
                }

                // 대상 Application의 destination name 획득을 위해 ControlServerManager 사용
                var controlManager = accessor.Resolve<ACS.Control.IControlServerManager>();
                string destinationName = controlManager?.GetDestinationName(appName);

                XmlDocument reply = syncAgent.Request(document, destinationName, timeout);

                if (reply != null)
                {
                    logger.Info($"SendHeartBeatActivity: {appName} 응답 수신 성공.");
                    context.Set(Result, true);
                }
                else
                {
                    logger.Warn($"SendHeartBeatActivity: {appName} 응답 없음 (타임아웃).");
                    context.Set(Result, false);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"SendHeartBeatActivity 실패 ({appName}): {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }

    /// <summary>
    /// Application 상태를 업데이트. 응답 성공 시 active, 실패 시 inactive/hang 등으로 변경.
    /// </summary>
    [Activity("ACS.Control", "Update Application State",
        "Application의 상태를 업데이트합니다.")]
    public class UpdateApplicationStateActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_HEARTBEAT");

        [Input(Description = "Application 이름")]
        public Input<string> ApplicationName { get; set; }

        [Input(Description = "변경할 상태 (active, inactive, hang 등)")]
        public Input<string> NewState { get; set; }

        [Input(Description = "CheckTime도 함께 업데이트할지 여부")]
        public Input<bool> UpdateCheckTime { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var appName = ApplicationName?.Get(context);
            var newState = NewState?.Get(context);
            var updateCheckTime = UpdateCheckTime?.Get(context) ?? false;

            if (string.IsNullOrEmpty(appName))
            {
                context.Set(Result, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var appManager = accessor.Resolve<IApplicationManager>();
                var now = DateTime.Now;

                if (!string.IsNullOrEmpty(newState))
                {
                    appManager.UpdateApplicationState(appName, newState, now);
                    logger.Info($"UpdateApplicationStateActivity: {appName} → {newState}");
                }

                if (updateCheckTime)
                {
                    appManager.UpdateApplicationCheckTime(appName, now);
                }

                context.Set(Result, true);
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateApplicationStateActivity 실패 ({appName}): {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }

    /// <summary>
    /// HeartBeat 실패 이력을 기록.
    /// IHistoryManagerEx.CreateHeartBeatFailHistory() 호출.
    /// </summary>
    [Activity("ACS.Control", "Record HeartBeat Fail",
        "HeartBeat 실패 이력을 DB에 기록합니다.")]
    public class RecordHeartBeatFailActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_HEARTBEAT");

        [Input(Description = "실패한 Application 이름")]
        public Input<string> ApplicationName { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            var appName = ApplicationName?.Get(context);

            if (string.IsNullOrEmpty(appName))
            {
                context.Set(Result, false);
                return;
            }

            try
            {
                var accessor = context.GetService<AutofacContainerAccessor>();
                var appManager = accessor.Resolve<IApplicationManager>();
                var historyManager = accessor.Resolve<IHistoryManagerEx>();

                var application = appManager.GetApplication(appName);
                if (application != null)
                {
                    historyManager.CreateHeartBeatFailHistory(application);
                    logger.Info($"RecordHeartBeatFailActivity: {appName} 실패 이력 기록됨.");
                }

                context.Set(Result, true);
            }
            catch (Exception ex)
            {
                logger.Error($"RecordHeartBeatFailActivity 실패 ({appName}): {ex.Message}", ex);
                context.Set(Result, false);
            }
        }
    }
}
