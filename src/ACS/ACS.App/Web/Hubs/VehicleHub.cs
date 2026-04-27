using Microsoft.AspNetCore.SignalR;

namespace ACS.App.Web.Hubs
{
    /// <summary>
    /// UI 클라이언트가 차량 상태 업데이트(특히 POSE 텔레메트리)를 구독하는 SignalR Hub.
    /// 서버 → 클라이언트 단방향(브로드캐스트). 클라이언트에서 호출할 메서드는 없음.
    /// PoseTelemetrySubscriber가 RabbitMQ에서 RAIL-VEHICLEUPDATE를 받아
    /// IHubContext&lt;VehicleHub&gt;를 통해 "PoseUpdate" 이벤트를 모든 연결에 발행한다.
    /// </summary>
    public class VehicleHub : Hub
    {
    }
}
