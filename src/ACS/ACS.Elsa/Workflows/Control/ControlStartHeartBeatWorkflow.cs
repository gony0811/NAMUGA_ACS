using System;
using System.Collections.Generic;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// CONTROL_STARTHEARTBEAT 워크플로우.
    ///
    /// Primary 서버(CS01_P)에서 실행되며, 동일 하드웨어의 모든 관리 Application
    /// (DS, ES, TS, HS, UI 등)에 CONTROL-HEARTBEAT 메시지를 전송하고,
    /// 응답이 없는 Application의 상태를 업데이트한다.
    ///
    /// 처리 흐름:
    ///   1. 워크플로우 Input에서 하드웨어 타입/타임아웃 등 파라미터 추출
    ///   2. 해당 하드웨어에서 실행 중인 Application 목록 조회
    ///   3. 각 Application에 HeartBeat 메시지 전송
    ///   4. 응답 성공 → active 상태 유지/갱신
    ///   5. 응답 실패 → 상태 업데이트 (inactive) + 실패 이력 기록
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "CONTROL_STARTHEARTBEAT"
    ///   - Arguments: object[] { XmlDocument } (CONTROL-HEARTBEAT XML)
    /// </summary>
    public class ControlStartHeartBeatWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "CONTROL-STARTHEARTBEAT";
            builder.Name = "CONTROL-STARTHEARTBEAT";
            builder.Description = "Primary 서버의 모든 Application에 HeartBeat 전송 및 상태 관리";

            // 워크플로우 변수
            var hardwareType = new Variable<string> { Name = "HardwareType" };
            var heartBeatTimeout = new Variable<long> { Name = "HeartBeatTimeout" };
            var heartBeatMessage = new Variable<string> { Name = "HeartBeatMessage" };
            var applicationNames = new Variable<List<string>> { Name = "ApplicationNames" };
            var currentAppName = new Variable<string> { Name = "CurrentAppName" };
            var heartBeatResult = new Variable<bool> { Name = "HeartBeatResult" };

            builder.WithVariable(hardwareType);
            builder.WithVariable(heartBeatTimeout);
            builder.WithVariable(heartBeatMessage);
            builder.WithVariable(applicationNames);
            builder.WithVariable(currentAppName);
            builder.WithVariable(heartBeatResult);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input에서 파라미터 추출
                    new ExtractHeartBeatInput
                    {
                        OutputMessage = new(heartBeatMessage),
                        OutputHardwareType = new(hardwareType),
                        OutputTimeout = new(heartBeatTimeout)
                    },

                    // Step 2: 해당 하드웨어의 Application 목록 조회
                    new GetApplicationsByHardwareActivity
                    {
                        HardwareType = new(hardwareType),
                        Result = new(applicationNames)
                    },

                    // Step 3: 각 Application에 대해 HeartBeat 전송
                    new ForEach<string>
                    {
                        Items = new(applicationNames),
                        CurrentValue = new(currentAppName),
                        Body = new Sequence
                        {
                            Activities =
                            {
                                // 3a. HeartBeat JSON 메시지 전송
                                new SendHeartBeatActivity
                                {
                                    ApplicationName = new(currentAppName),
                                    HeartBeatMessage = new(heartBeatMessage),
                                    Timeout = new(heartBeatTimeout),
                                    Result = new(heartBeatResult)
                                },

                                // 3b. 응답에 따라 분기 처리
                                new If
                                {
                                    Condition = new(heartBeatResult),

                                    // 응답 성공: active 상태 갱신
                                    Then = new UpdateApplicationStateActivity
                                    {
                                        ApplicationName = new(currentAppName),
                                        NewState = new("active"),
                                        UpdateCheckTime = new(true)
                                    },

                                    // 응답 실패: inactive로 변경 + 실패 이력 기록
                                    Else = new Sequence
                                    {
                                        Activities =
                                        {
                                            new UpdateApplicationStateActivity
                                            {
                                                ApplicationName = new(currentAppName),
                                                NewState = new("inactive"),
                                                UpdateCheckTime = new(true)
                                            },
                                            new RecordHeartBeatFailActivity
                                            {
                                                ApplicationName = new(currentAppName)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },

                    // Step 4: 완료 로그
                    new WriteLine("CONTROL_STARTHEARTBEAT workflow completed.")
                }
            };
        }
    }

    /// <summary>
    /// 워크플로우 Input에서 HeartBeat 관련 파라미터를 추출하는 Activity.
    /// HeartBeat 설정(하드웨어 타입, 타임아웃)은 IControlServerManager에서 조회.
    /// </summary>
    [Activity("ACS.Control", "Extract HeartBeat Input",
        "워크플로우 입력에서 HeartBeat 파라미터를 추출합니다.")]
    public class ExtractHeartBeatInput : CodeActivity
    {
        [Output(Description = "HeartBeat JSON 메시지")]
        public Output<string> OutputMessage { get; set; }

        [Output(Description = "하드웨어 타입 (PRIMARY/SECONDARY)")]
        public Output<string> OutputHardwareType { get; set; }

        [Output(Description = "HeartBeat 타임아웃 (밀리초)")]
        public Output<long> OutputTimeout { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            string hardwareType = "PRIMARY";
            long timeout = 5000L;

            try
            {
                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                var controlManager = accessor?.Resolve<ACS.Control.IControlServerManager>();

                if (controlManager != null)
                {
                    timeout = controlManager.UseSecondAsTimeUnit
                        ? controlManager.HeartBeatTimeout / 1000L
                        : controlManager.HeartBeatTimeout;
                }

                var config = accessor?.Resolve<Microsoft.Extensions.Configuration.IConfiguration>();
                var hw = config?["Acs:Process:HardwareType"];
                if (!string.IsNullOrEmpty(hw))
                    hardwareType = hw;
            }
            catch
            {
                // 기본값 사용
            }

            string jsonMessage = $"{{\"messageName\":\"CONTROL-HEARTBEAT\",\"timestamp\":\"{DateTime.UtcNow:o}\"}}";

            context.Set(OutputMessage, jsonMessage);
            context.Set(OutputHardwareType, hardwareType);
            context.Set(OutputTimeout, timeout);
        }
    }
}
