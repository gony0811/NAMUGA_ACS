using System;

namespace ACS.Communication.Http.Models
{
    public class UserDto
    {
        public int Seq { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; }
        public bool MustChangePassword { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? EditTime { get; set; }
        public string Creator { get; set; }
        public string Editor { get; set; }
        public string Description { get; set; }

        /// <summary>생성/리셋 시에만 사용되는 평문 비밀번호. 응답에는 절대 포함하지 않음.</summary>
        public string InitialPassword { get; set; }
    }

    public class LoginRequestDto
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
