using System;

namespace ACS.UI.Models;

public class UserDto
{
    public int Seq { get; set; }
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "Viewer";
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginTime { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? EditTime { get; set; }
    public string Creator { get; set; } = "";
    public string Editor { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>생성/리셋 시 평문 비밀번호 전송용 (응답에는 포함되지 않음).</summary>
    public string InitialPassword { get; set; } = "";
}

public class LoginRequest
{
    public string UserId { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResult
{
    public string Token { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "";
    public bool MustChangePassword { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
