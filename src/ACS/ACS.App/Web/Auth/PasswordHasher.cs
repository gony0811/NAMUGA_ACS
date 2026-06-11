namespace ACS.App.Web.Auth
{
    /// <summary>
    /// BCrypt 기반 비밀번호 해싱/검증 헬퍼.
    /// Work factor 11 — 데스크탑 로그인 대기 시간(~150ms)과 무차별 대입 비용의 균형점.
    /// </summary>
    public static class PasswordHasher
    {
        private const int WorkFactor = 11;

        public static string Hash(string plain)
        {
            if (string.IsNullOrEmpty(plain))
                throw new System.ArgumentException("password is empty", nameof(plain));
            return BCrypt.Net.BCrypt.HashPassword(plain, WorkFactor);
        }

        public static bool Verify(string plain, string hash)
        {
            if (string.IsNullOrEmpty(plain) || string.IsNullOrEmpty(hash))
                return false;
            try
            {
                return BCrypt.Net.BCrypt.Verify(plain, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
