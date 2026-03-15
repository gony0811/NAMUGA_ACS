using System;
using ACS.Core.Host;
using ACS.Core.Logging;

namespace ACS.Communication.Host
{
    /// <summary>
    /// IHostTcpGateway stub 구현.
    /// 실제 TCP/IP 통신 대신 로그만 출력한다.
    /// TCP/IP 구현 시 이 클래스를 실제 구현체로 교체.
    /// </summary>
    public class HostTcpGatewayStub : IHostTcpGateway
    {
        private readonly Logger logger = Logger.GetLogger(typeof(HostTcpGatewayStub));

        public bool IsConnected => false;

        public event EventHandler<HostTcpMessageEventArgs> MessageReceived;

        public void Start()
        {
            logger.Info("[HostTcpGatewayStub] Start called - TCP/IP gateway is stubbed, no actual connection.");
        }

        public void Stop()
        {
            logger.Info("[HostTcpGatewayStub] Stop called.");
        }

        public void SendToHost(string messageName, string messageBody)
        {
            logger.Info($"[HostTcpGatewayStub] SendToHost - messageName={messageName}, body length={messageBody?.Length ?? 0}");
            // 실제 구현 시: TCP 소켓으로 직렬화하여 전송
        }

        /// <summary>
        /// 테스트 헬퍼: Host에서 메시지를 수신한 것처럼 시뮬레이션.
        /// 단위 테스트나 디버그 도구에서 호출.
        /// </summary>
        public void SimulateReceive(string messageName, string messageBody)
        {
            logger.Info($"[HostTcpGatewayStub] SimulateReceive - messageName={messageName}");
            MessageReceived?.Invoke(this, new HostTcpMessageEventArgs
            {
                MessageName = messageName,
                MessageBody = messageBody
            });
        }
    }
}
