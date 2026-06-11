using System;
using ACS.Core.Base;

namespace ACS.Core.User.Model
{
    /// <summary>
    /// ACS 사용자 계정 (NA_X_USER 테이블). 로그인 ID/BCrypt 해시/역할 기반 권한.
    /// </summary>
    public class User : TimedEntity
    {
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_OPERATOR = "Operator";
        public const string ROLE_VIEWER = "Viewer";

        public virtual int Seq { get; set; }
        public virtual string UserId { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string Role { get; set; } = ROLE_VIEWER;
        public virtual bool MustChangePassword { get; set; }
        public virtual bool IsActive { get; set; } = true;
        public virtual DateTime? LastLoginTime { get; set; }
    }
}
