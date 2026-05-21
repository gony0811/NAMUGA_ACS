using System.Xml;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using ACS.Elsa.Activities;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// HOST_ACTIONCMD 워크플로우.
    ///
    /// Host(MES)로부터 ACTIONCMD XML을 수신하면:
    ///   1. JOBREPORT(RECEIVE) 응답을 Host에 전송
    ///   2. XML을 ACTIONCMD JSON으로 변환하여 RabbitMQ를 통해 Trans 프로세스로 전송
    ///
    /// 워크플로우 입력:
    ///   - CommandName: "ACTIONCMD"
    ///   - Arguments: object[] { XmlDocument }
    /// </summary>
    public class HostActionCmdWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "ACTIONCMD";
            builder.Name = "ACTIONCMD";
            builder.Description = "Host ACTIONCMD 수신 → JOBREPORT(RECEIVE) 응답 → JSON 변환 → Trans 전송";

            var actionCmdXml = new Variable<XmlDocument> { Name = "ActionCmdXml" };
            builder.WithVariable(actionCmdXml);

            builder.Root = new Sequence
            {
                Activities =
                {
                    // Step 1: Input에서 ACTIONCMD XmlDocument 추출
                    new ExtractMoveCmdFromInput
                    {
                        OutputXml = new(actionCmdXml)
                    },

                    // Step 2: JOBREPORT(RECEIVE) 응답 전송
                    new ReplyActionCmdReceiveActivity
                    {
                        ActionCmdXml = new(actionCmdXml)
                    },

                    // Step 3: XML → JSON 변환 후 RabbitMQ로 Trans에 전송
                    new SendActionCmdJsonToTransActivity
                    {
                        ActionCmdXml = new(actionCmdXml)
                    },

                    new WriteLine("ACTIONCMD workflow completed: JOBREPORT(RECEIVE) sent, JSON forwarded to Trans")
                }
            };
        }
    }
}
