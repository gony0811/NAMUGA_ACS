namespace ACS.UI.Models;

/// <summary>
/// SignalR HostCommHub의 "Log" 이벤트로 전달되는 Host TCP 통신 로그 항목.
/// 서버 HostCommSubscriber가 RabbitMQ HOSTCOMM 메시지에서 변환해 발행한다.
/// </summary>
public class HostCommLogDto
{
    /// <summary>"Send" 또는 "Receive"</summary>
    public string Direction { get; set; }

    /// <summary>Host 메시지 이름 (MOVECMD, JOBREPORT 등)</summary>
    public string MessageName { get; set; }

    /// <summary>원격 종단점 (IP:Port)</summary>
    public string RemoteEndPoint { get; set; }

    /// <summary>메시지 본문 길이 (bytes)</summary>
    public int Length { get; set; }

    /// <summary>메시지 본문 (8KB까지 잘림)</summary>
    public string Body { get; set; }

    /// <summary>송신 성공 여부 (Direction=Send 일 때만 의미 있음)</summary>
    public bool? Success { get; set; }

    /// <summary>송신 실패 시 오류 메시지</summary>
    public string Error { get; set; }

    /// <summary>이벤트 발생 시각 (UTC)</summary>
    public DateTime EventTime { get; set; }
}

/// <summary>
/// SignalR HostCommHub의 "Connection" 이벤트로 전달되는 Host TCP 연결 상태 변경.
/// </summary>
public class HostCommConnectionDto
{
    public bool Connected { get; set; }
    public string RemoteEndPoint { get; set; }
    public DateTime EventTime { get; set; }
}
