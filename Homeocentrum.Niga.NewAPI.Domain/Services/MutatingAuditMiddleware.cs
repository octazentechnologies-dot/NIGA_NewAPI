using System.Security.Claims;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    /// <summary>
    /// SEC-08.02 — Write AuditEvent on mutating money / Rx / approval / PII endpoints.
    /// </summary>
    public class MutatingAuditMiddleware
    {
        private static readonly PathPrefix[] Prefixes =
        {
            new("/api/package", "Package"),
            new("/api/Prescription", "Prescription"),
            new("/api/Account", "Account"),
            new("/api/patient", "Patient"),
            new("/api/PatientLab", "PatientLab"),
            new("/api/Caregiver", "Caregiver"),
            new("/api/Family", "Family"),
            new("/api/Consent", "Consent"),
            new("/api/SecureDocument", "SecureDocument"),
            new("/api/Device", "Device"),
            new("/api/PatientProfile", "PatientProfile"),
        };

        private static readonly string[] SkipSuffixes =
        {
            "/Login", "/LoginWithOtp", "/ForgotPassword", "/ResetPassword",
            "/RequestOtp", "/VerifyOtp", "/SubscriptionStatus"
        };

        private readonly RequestDelegate _next;

        public MutatingAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuditEventWriter writer)
        {
            await _next(context);

            try
            {
                var method = context.Request.Method;
                if (method is not ("POST" or "PUT" or "PATCH" or "DELETE"))
                    return;

                var path = context.Request.Path.Value ?? "";
                if (SkipSuffixes.Any(s => path.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                    return;

                var match = Prefixes.FirstOrDefault(p =>
                    path.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase));
                if (match.Prefix == null)
                    return;

                if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 400)
                    return;

                long? userId = null;
                try { userId = context.User.GetUserId(); }
                catch { /* anonymous */ }

                await writer.WriteAsync(
                    userId,
                    AdminAuthorizationPolicies.GetRoleName(context.User)
                        ?? context.User.FindFirst(ClaimTypes.Role)?.Value,
                    $"{method} {path}",
                    match.Entity,
                    correlationId: context.TraceIdentifier);
                Homeocentrum.Niga.NewAPI.Domain.Logging.AppFileLog.Audit($"{method} {path}", match.Entity, userId,
                    AdminAuthorizationPolicies.GetRoleName(context.User));
            }
            catch
            {
                // Audit must not fail the request.
            }
        }

        private readonly struct PathPrefix
        {
            public PathPrefix(string prefix, string entity)
            {
                Prefix = prefix;
                Entity = entity;
            }

            public string Prefix { get; }
            public string Entity { get; }
        }
    }
}
