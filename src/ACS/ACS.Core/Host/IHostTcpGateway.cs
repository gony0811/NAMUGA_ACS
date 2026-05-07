using System;

namespace ACS.Core.Host
{
    /// <summary>
    /// Host(MES)와의 TCP/IP 통신 게이트웨이 인터페이스.
    /// HS01_P 프로세스에서 사용하며, Host TCP/IP ↔ RabbitMQ 브릿지의 TCP 측을 담당.
    /// </summary>
    public interface IHostTcpGateway
    {
        /// <summary>TCP 리스너 시작 (Host 연결 수신 대기)</summary>
        void Start();

        /// <summary>TCP 리스너 중지 및 연결 해제</summary>
        void Stop();

        /// <summary>Host와 연결/리스닝 상태 여부</summary>
        bool IsConnected { get; }

        /// <summary>
        /// Host로 메시지 전송 (TRSJOBREPORT, TRSSTATEREPORT 등).
        /// Trans에서 RabbitMQ로 수신한 리포트를 Host TCP/IP로 포워딩할 때 사용.
        /// </summary>
        void SendToHost(string messageName, string messageBody);

        /// <summary>
        /// Host로부터 TCP/IP로 메시지 수신 시 발생하는 이벤트.
        /// HostBridgeService가 구독하여 RabbitMQ로 포워딩.
        /// </summary>
        event EventHandler<HostTcpMessageEventArgs> MessageReceived;

        /// <summary>Host가 ListenPort로 접속했을 때 발생.</summary>
        event EventHandler<HostTcpConnectionEventArgs> Connected;

        /// <summary>Host와의 연결이 끊겼을 때 발생.</summary>
        event EventHandler<HostTcpConnectionEventArgs> Disconnected;

        /// <summary>SendToHost 호출 결과(성공/실패)를 알리는 이벤트.</summary>
        event EventHandler<HostTcpMessageSentEventArgs> MessageSent;
    }

    /// <summary>
    /// Host TCP/IP에서 수신한 메시지 이벤트 인자.
    /// </summary>
    public class HostTcpMessageEventArgs : EventArgs
    {
        /// <summary>메시지 이름 (MOVECMD, MOVECANCEL, MOVEUPDATE 등)</summary>
        public string MessageName { get; set; }

        /// <summary>메시지 본문 (XML 문자열)</summary>
        public string MessageBody { get; set; }

        /// <summary>원격 종단점 (IP:Port). 알 수 없으면 빈 문자열.</summary>
        public string RemoteEndPoint { get; set; }
    }

    /// <summary>
    /// Host 연결/단절 이벤트 인자.
    /// </summary>
    public class HostTcpConnectionEventArgs : EventArgs
    {
        public string RemoteEndPoint { get; set; }
        public bool Connected { get; set; }
    }

    /// <summary>
    /// Host로 메시지 송신 결과 이벤트 인자.
    /// </summary>
    public class HostTcpMessageSentEventArgs : EventArgs
    {
        public string MessageName { get; set; }
        public string MessageBody { get; set; }
        public string RemoteEndPoint { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
