using System;
using System.Collections.Concurrent;

namespace ACS.App.Web.Auth
{
    /// <summary>
    /// 메모리 기반 세션 저장소. 토큰=GUID, 12시간 슬라이딩 만료.
    /// 산업용 내부망 환경 — JWT/Redis 대신 단순 토큰으로 충분.
    /// 백엔드 재시작 시 모든 세션 무효화 (UI는 401 응답 → 재로그인).
    /// </summary>
    public class SessionStore
    {
        public class SessionInfo
        {
            public string UserId { get; set; }
            public string Role { get; set; }
            public DateTime IssuedAt { get; set; }
            public DateTime LastAccess { get; set; }
        }

        private static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(12);
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

        public string Issue(string userId, string role)
        {
            var token = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            _sessions[token] = new SessionInfo
            {
                UserId = userId,
                Role = role,
                IssuedAt = now,
                LastAccess = now
            };
            return token;
        }

        /// <summary>
        /// 토큰 유효성 검사 + LastAccess 갱신. 유효하지 않거나 만료된 경우 null.
        /// </summary>
        public SessionInfo Touch(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            if (!_sessions.TryGetValue(token, out var info)) return null;

            var now = DateTime.UtcNow;
            if (now - info.LastAccess > SlidingExpiration)
            {
                _sessions.TryRemove(token, out _);
                return null;
            }
            info.LastAccess = now;
            return info;
        }

        public void Revoke(string token)
        {
            if (!string.IsNullOrEmpty(token))
                _sessions.TryRemove(token, out _);
        }

        /// <summary>특정 사용자의 모든 세션 무효화 (비활성 처리/역할 변경 시 호출).</summary>
        public void RevokeAllForUser(string userId)
        {
            foreach (var kvp in _sessions)
            {
                if (kvp.Value.UserId == userId)
                    _sessions.TryRemove(kvp.Key, out _);
            }
        }
    }
}
