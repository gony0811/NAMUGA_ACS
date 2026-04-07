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
    /// HOST_MOVECANCEL 워크플로우.
    ///
    /// Host(MES)로부터 MOVECANCEL을 수신하면 실행되는 워크플로우.
    ///
    /// 처리 흐름:
    ///   1. MOVECANCEL XML 수신 (Input으로 전달됨)
    ///   2. TransportCommand 취소 처리
    ///   3. JOBREPORT(CANCEL) 응답을 Host에 전송 — ErrorCode/ErrorMsg 포함
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "MOVECANCEL" (ElsaWorkflowManagerBridge가 설정)
    ///   - Arguments: object[] { XmlDocument } (MOVECANCEL XML)
    /// </summary>
    public class HostMoveCancelWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "MOVECANCEL";
            builder.Name = "MOVECANCEL";
            builder.Description = "Host MOVECANCEL 수신 → TC 취소 → JOBREPORT(CANCEL) 응답";

            var moveCancelXml = new Variable<XmlDocument> { Name = "MoveCancelXml" };
            var jobReportXml = new Variable<XmlDocument> { Name = "JobReportXml" };
            var jobId = new Variable<string> { Name = "JobId" };
            var errCode = new Variable<string> { Name = "ErrCode" };
            var errMsg = new Variable<string> { Name = "ErrMsg" };
            builder.WithVariable(moveCancelXml);
            builder.WithVariable(jobReportXml);
            builder.WithVariable(jobId);
            builder.WithVariable(errCode);
            builder.WithVariable(errMsg);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input에서 MOVECANCEL XmlDocument 추출
                    new ExtractMoveCmdFromInput
                    {
                        OutputXml = new(moveCancelXml)
                    },

                    // Step 2: TransportCommand 취소 처리
                    new CancelTransportCommandActivity
                    {
                        MoveCancelXml = new(moveCancelXml),
                        JobId = new(jobId),
                        ErrCode = new(errCode),
                        ErrMsg = new(errMsg)
                    },

                    // Step 3: JOBREPORT(CANCEL) 응답 전송 — ErrorCode/ErrorMsg 포함
                    new SendJobReportActivity
                    {
                        MoveCmdXml = new(moveCancelXml),
                        ReportType = new("CANCEL"),
                        ErrCode = new(errCode),
                        ErrMsg = new(errMsg),
                        JobReportXml = new(jobReportXml)
                    },

                    // Step 4: 로그 출력
                    new WriteLine("MOVECANCEL workflow completed: TransportCommand canceled, JOBREPORT(CANCEL) sent")
                }
            };
        }
    }
}
