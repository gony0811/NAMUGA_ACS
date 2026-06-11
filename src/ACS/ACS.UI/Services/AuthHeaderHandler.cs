using System.Net.Http;
using System.Net.Http.Headers;

namespace ACS.UI.Services;

/// <summary>
/// 매 HTTP 요청에 UserSession.Token 을 Authorization: Bearer 헤더로 부착한다.
/// 로그인 엔드포인트는 토큰이 없을 때 호출되므로 빈 토큰일 때는 헤더를 생략한다.
/// </summary>
internal class AuthHeaderHandler : DelegatingHandler
{
    private readonly UserSession _session;

    public AuthHeaderHandler(UserSession session, HttpMessageHandler inner) : base(inner)
    {
        _session = session;
    }

    protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_session?.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
