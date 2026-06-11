using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ACS.App.Web.Auth;
using ACS.Communication.Http.Models;
using UserEntity = ACS.Core.User.Model.User;

namespace ACS.App.Web.Controllers
{
    /// <summary>
    /// 사용자 관리 (Admin 전용). NA_X_USER CRUD.
    /// 본인 삭제 및 마지막 Admin 삭제를 거부해 잠금 상태(lock-out) 방지.
    /// (ACS.Core.User.Model.User 는 ControllerBase.User 속성과 충돌하므로 별칭 UserEntity 사용)
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [AcsAuthorize(Role = UserEntity.ROLE_ADMIN)]
    public class UsersController : ControllerBase
    {
        private readonly ACS.Database.AcsDbContext _db;
        private readonly SessionStore _sessions;

        public UsersController(ACS.Database.AcsDbContext db, SessionStore sessions)
        {
            _db = db;
            _sessions = sessions;
        }

        // GET /api/users
        [HttpGet]
        public ActionResult<List<UserDto>> Get()
        {
            return _db.Users.AsNoTracking()
                .OrderBy(x => x.Seq)
                .ToList()
                .Select(ToDto)
                .ToList();
        }

        // POST /api/users — 신규 사용자 (InitialPassword 필수, MustChangePassword=true 강제)
        [HttpPost]
        public ActionResult<UserDto> Create([FromBody] UserDto dto)
        {
            if (dto == null) return BadRequest(new { error = "body is required" });
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest(new { error = "UserId is required" });
            if (string.IsNullOrWhiteSpace(dto.InitialPassword))
                return BadRequest(new { error = "InitialPassword is required" });
            if (!IsValidRole(dto.Role))
                return BadRequest(new { error = "Role must be Admin/Operator/Viewer" });

            if (_db.Users.Any(x => x.UserId == dto.UserId))
                return Conflict(new { error = "UserId already exists" });

            var nowUtc = DateTime.UtcNow;
            var session = HttpContext.Items["AcsSession"] as SessionStore.SessionInfo;
            var operatorId = session?.UserId ?? "UI";

            var entity = new UserEntity
            {
                UserId = dto.UserId,
                PasswordHash = PasswordHasher.Hash(dto.InitialPassword),
                Role = dto.Role,
                MustChangePassword = true,
                IsActive = dto.IsActive,
                Description = dto.Description,
                CreateTime = nowUtc,
                EditTime = nowUtc,
                Creator = operatorId,
                Editor = operatorId
            };
            _db.Users.Add(entity);
            _db.SaveChanges();
            return ToDto(entity);
        }

        // PUT /api/users/{seq} — Role/IsActive/Description/Password reset
        [HttpPut("{seq:int}")]
        public ActionResult<UserDto> Update(int seq, [FromBody] UserDto dto)
        {
            if (dto == null) return BadRequest(new { error = "body is required" });

            var entity = _db.Users.FirstOrDefault(x => x.Seq == seq);
            if (entity == null) return NotFound();

            if (!IsValidRole(dto.Role))
                return BadRequest(new { error = "Role must be Admin/Operator/Viewer" });

            // 마지막 Admin을 Admin이 아닌 다른 역할로 강등시키지 못함
            if (entity.Role == UserEntity.ROLE_ADMIN && dto.Role != UserEntity.ROLE_ADMIN)
            {
                var adminCount = _db.Users.Count(x => x.Role == UserEntity.ROLE_ADMIN && x.IsActive);
                if (adminCount <= 1)
                    return BadRequest(new { error = "Cannot demote the last active Admin" });
            }
            // 마지막 Admin을 비활성으로 만들지 못함
            if (entity.Role == UserEntity.ROLE_ADMIN && entity.IsActive && !dto.IsActive)
            {
                var adminCount = _db.Users.Count(x => x.Role == UserEntity.ROLE_ADMIN && x.IsActive);
                if (adminCount <= 1)
                    return BadRequest(new { error = "Cannot deactivate the last active Admin" });
            }

            var roleChanged = entity.Role != dto.Role;
            var deactivated = entity.IsActive && !dto.IsActive;

            entity.Role = dto.Role;
            entity.IsActive = dto.IsActive;
            entity.Description = dto.Description;

            // InitialPassword 가 전달되면 비밀번호 리셋 + MustChangePassword=true
            if (!string.IsNullOrWhiteSpace(dto.InitialPassword))
            {
                entity.PasswordHash = PasswordHasher.Hash(dto.InitialPassword);
                entity.MustChangePassword = true;
            }

            var session = HttpContext.Items["AcsSession"] as SessionStore.SessionInfo;
            entity.EditTime = DateTime.UtcNow;
            entity.Editor = session?.UserId ?? "UI";
            _db.SaveChanges();

            // 역할 변경/비활성화 시 해당 사용자의 기존 세션을 무효화 (다음 호출에서 401 → 재로그인)
            if (roleChanged || deactivated || !string.IsNullOrWhiteSpace(dto.InitialPassword))
                _sessions.RevokeAllForUser(entity.UserId);

            return ToDto(entity);
        }

        // DELETE /api/users/{seq}
        [HttpDelete("{seq:int}")]
        public ActionResult Delete(int seq)
        {
            var entity = _db.Users.FirstOrDefault(x => x.Seq == seq);
            if (entity == null) return NotFound();

            var session = HttpContext.Items["AcsSession"] as SessionStore.SessionInfo;
            if (session != null && string.Equals(session.UserId, entity.UserId, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Cannot delete your own account" });

            if (entity.Role == UserEntity.ROLE_ADMIN)
            {
                var adminCount = _db.Users.Count(x => x.Role == UserEntity.ROLE_ADMIN && x.IsActive);
                if (adminCount <= 1)
                    return BadRequest(new { error = "Cannot delete the last active Admin" });
            }

            _db.Users.Remove(entity);
            _db.SaveChanges();
            _sessions.RevokeAllForUser(entity.UserId);
            return NoContent();
        }

        private static bool IsValidRole(string role)
            => role == UserEntity.ROLE_ADMIN || role == UserEntity.ROLE_OPERATOR || role == UserEntity.ROLE_VIEWER;

        private static UserDto ToDto(UserEntity x) => new UserDto
        {
            Seq = x.Seq,
            UserId = x.UserId,
            Role = x.Role,
            MustChangePassword = x.MustChangePassword,
            IsActive = x.IsActive,
            LastLoginTime = NormalizeUtc(x.LastLoginTime),
            CreateTime = NormalizeUtc(x.CreateTime),
            EditTime = NormalizeUtc(x.EditTime),
            Creator = x.Creator,
            Editor = x.Editor,
            Description = x.Description
            // InitialPassword 는 응답에서 제외 (절대 평문 반환 금지)
        };

        private static DateTime? NormalizeUtc(DateTime? t)
        {
            if (t is not { } v) return null;
            return v.Kind switch
            {
                DateTimeKind.Utc => v,
                DateTimeKind.Local => v.ToUniversalTime(),
                _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            };
        }
    }
}
