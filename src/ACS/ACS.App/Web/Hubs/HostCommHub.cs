using Microsoft.AspNetCore.SignalR;

namespace ACS.App.Web.Hubs
{
    /// <summary>
    /// UI 클라이언트가 Host(MES) TCP 통신 로그를 구독하는 SignalR Hub.
    /// 서버 → 클라이언트 단방향 브로드캐스트.
    /// HostCommSubscriber가 RabbitMQ fanout(/UI/HOSTCOMM)에서 받은 이벤트를
    /// IHubContext&lt;HostCommHub&gt;를 통해 "Log", "Connection", "MessageSent" 이벤트로 발행한다.
    /// </summary>
    public class HostCommHub : Hub
    {
    }
}
