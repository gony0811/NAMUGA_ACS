using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ACS.App.Web.Auth;
using ACS.Communication.Http.Models;
using ACS.Core.User.Model;

namespace ACS.App.Web.Controllers
{
    /// <summary>
    /// 로그인/로그아웃/비밀번호 변경 엔드포인트.
    /// 로그인은 익명 호출 가능, 나머지는 Bearer 토큰 필요.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ACS.Database.AcsDbContext _db;
        private readonly SessionStore _sessions;

        public AuthController(ACS.Database.AcsDbContext db, SessionStore sessions)
        {
            _db = db;
            _sessions = sessions;
        }

        // POST /api/auth/login — 익명
        [HttpPost("login")]
        public ActionResult<LoginResponseDto> Login([FromBody] LoginRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "userId and password are required" });

            var user = _db.Users.FirstOrDefault(x => x.UserId == req.UserId);
            if (user == null || !user.IsActive)
                return Unauthorized(new { error = "Invalid credentials" });

            if (!PasswordHasher.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { error = "Invalid credentials" });

            user.LastLoginTime = DateTime.UtcNow;
            _db.SaveChanges();

            var token = _sessions.Issue(user.UserId, user.Role);
            return new LoginResponseDto
            {
                Token = token,
                UserId = user.UserId,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword
            };
        }

        // POST /api/auth/logout — Bearer
        [HttpPost("logout")]
        [AcsAuthorize]
        public ActionResult Logout()
        {
            var auth = Request.Headers["Authorization"].ToString();
            const string bearerPrefix = "Bearer ";
            if (auth.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _sessions.Revoke(auth.Substring(bearerPrefix.Length).Trim());
            }
            return Ok(new { success = true });
        }

        // POST /api/auth/change-password — Bearer
        [HttpPost("change-password")]
        [AcsAuthorize]
        public ActionResult ChangePassword([FromBody] ChangePasswordRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { error = "current/new password are required" });
            if (req.NewPassword.Length < 4)
                return BadRequest(new { error = "new password must be at least 4 characters" });

            var session = HttpContext.Items["AcsSession"] as SessionStore.SessionInfo;
            if (session == null) return Unauthorized();

            var user = _db.Users.FirstOrDefault(x => x.UserId == session.UserId);
            if (user == null) return Unauthorized();

            if (!PasswordHasher.Verify(req.CurrentPassword, user.PasswordHash))
                return BadRequest(new { error = "current password is incorrect" });

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            user.MustChangePassword = false;
            user.EditTime = DateTime.UtcNow;
            user.Editor = user.UserId;
            _db.SaveChanges();

            return Ok(new { success = true });
        }
    }
}
