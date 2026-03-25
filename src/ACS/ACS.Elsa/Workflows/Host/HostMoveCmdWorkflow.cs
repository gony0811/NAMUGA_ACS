using System;
using System.Xml;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// HOST_MOVECMD 워크플로우.
    ///
    /// Host(MES)로부터 MOVECMD를 수신하면 실행되는 워크플로우.
    ///
    /// 처리 흐름:
    ///   1. MOVECMD XML 수신 (Input으로 전달됨)
    ///   2. JOBREPORT(RECEIVE) 응답을 Host에 전송 — 작업 접수 확인
    ///   3. (향후 확장) 차량 배정, 경로 계산, 이동 명령 등
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "MOVECMD" (ElsaWorkflowManagerBridge가 설정)
    ///   - Arguments: object[] { XmlDocument } (MOVECMD XML)
    /// </summary>
    public class HostMoveCmdWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            // 워크플로우 메타데이터
            // DefinitionId = DispatchWorkflowDefinitionRequest에서 찾는 키
            builder.DefinitionId = "MOVECMD";
            builder.Name = "MOVECMD";
            builder.Description = "Host MOVECMD 수신 → JOBREPORT(RECEIVE) 응답 → 작업 처리";

            // 워크플로우 변수: MOVECMD XML을 저장
            var moveCmdXml = new Variable<XmlDocument> { Name = "MoveCmdXml" };
            var jobReportXml = new Variable<XmlDocument> { Name = "JobReportXml" };
            var transportCommandId = new Variable<string> { Name = "TransportCommandId" };
            var errCode = new Variable<string> { Name = "ErrCode" };
            var errMsg = new Variable<string> { Name = "ErrMsg" };
            builder.WithVariable(moveCmdXml);
            builder.WithVariable(jobReportXml);
            builder.WithVariable(transportCommandId);
            builder.WithVariable(errCode);
            builder.WithVariable(errMsg);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input에서 MOVECMD XmlDocument 추출
                    new ExtractMoveCmdFromInput
                    {
                        OutputXml = new(moveCmdXml)
                    },

                    // Step 2: TransportCommand 생성 시도 (BayId 검증 포함)
                    new CreateTransportCommandActivity
                    {
                        MoveCmdXml = new(moveCmdXml),
                        TransportCommandId = new(transportCommandId),
                        ErrCode = new(errCode),
                        ErrMsg = new(errMsg)
                    },

                    // Step 3: JOBREPORT(RECEIVE) 응답 전송 — 에러 정보 포함
                    new SendJobReportActivity
                    {
                        MoveCmdXml = new(moveCmdXml),
                        ReportType = new("RECEIVE"),
                        ErrCode = new(errCode),
                        ErrMsg = new(errMsg),
                        JobReportXml = new(jobReportXml)
                    },

                    // Step 4: 로그 출력
                    new WriteLine("MOVECMD workflow completed: TransportCommand processed, JOBREPORT(RECEIVE) sent")

                    // TODO: 향후 추가 Step
                    // - 차량 배정 로직
                    // - 차량에 이동 명령 전송
                    // - JOBREPORT(ASSIGNED) 전송
                }
            };
        }
    }

    /// <summary>
    /// 워크플로우 Input(Arguments)에서 MOVECMD XmlDocument를 추출하는 Activity.
    ///
    /// ElsaWorkflowManagerBridge가 전달하는 Input 형식:
    ///   { "CommandName": "MOVECMD", "Arguments": object[] { XmlDocument } }
    /// </summary>
    [Activity("ACS.Host", "Extract MoveCmd XML",
        "워크플로우 입력에서 MOVECMD XmlDocument를 추출합니다.")]
    public class ExtractMoveCmdFromInput : CodeActivity
    {
        [Output(Description = "추출된 MOVECMD XmlDocument")]
        public Output<XmlDocument> OutputXml { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            XmlDocument result = null;

            // 방법 1: Input dictionary에서 Arguments 추출
            var input = context.WorkflowExecutionContext.Input;
            if (input != null && input.TryGetValue("Arguments", out var args))
            {
                if (args is object[] argsArray && argsArray.Length > 0)
                {
                    if (argsArray[0] is XmlDocument xmlDoc)
                    {
                        result = xmlDoc;
                    }
                    else if (argsArray[0] is string xmlString)
                    {
                        result = new XmlDocument();
                        result.LoadXml(xmlString);
                    }
                }
                else if (args is XmlDocument singleDoc)
                {
                    result = singleDoc;
                }
            }

            if (result == null)
            {
                // 빈 문서 생성 (워크플로우 계속 진행)
                result = new XmlDocument();
                result.LoadXml("<Msg><Command>MOVECMD</Command><Header/><DataLayer/></Msg>");
            }

            context.Set(OutputXml, result);
        }
    }
}
