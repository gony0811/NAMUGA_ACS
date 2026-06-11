using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ACS.App.Web.Auth
{
    /// <summary>
    /// REST 엔드포인트 보호. Authorization: Bearer &lt;token&gt; 헤더 검증 후
    /// Role 파라미터(콤마 구분)와 매칭.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AcsAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string Role { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var sessionStore = context.HttpContext.RequestServices.GetService<SessionStore>();
            if (sessionStore == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var auth = context.HttpContext.Request.Headers["Authorization"].ToString();
            const string bearerPrefix = "Bearer ";
            if (string.IsNullOrEmpty(auth) || !auth.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            var token = auth.Substring(bearerPrefix.Length).Trim();

            var session = sessionStore.Touch(token);
            if (session == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!string.IsNullOrEmpty(Role))
            {
                var allowed = Role.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                if (!allowed.Any(r => string.Equals(r, session.Role, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            context.HttpContext.Items["AcsSession"] = session;
        }
    }
}
