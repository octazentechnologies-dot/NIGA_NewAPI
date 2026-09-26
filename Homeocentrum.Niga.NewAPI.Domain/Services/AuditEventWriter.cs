using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    /// <summary>SEC-08.02 — Helper to write AuditEvent rows from mutating endpoints.</summary>
    public interface IAuditEventWriter
    {
        Task WriteAsync(
            long? actorUserId,
            string? role,
            string action,
            string entity,
            string? oldJson = null,
            string? newJson = null,
            string? correlationId = null);
    }

    public class AuditEventWriter : IAuditEventWriter
    {
        private readonly NIGACentrumContext _context;

        public AuditEventWriter(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task WriteAsync(
            long? actorUserId,
            string? role,
            string action,
            string entity,
            string? oldJson = null,
            string? newJson = null,
            string? correlationId = null)
        {
            _context.AuditEvents.Add(new AuditEvent
            {
                ActorUserId = actorUserId,
                Role = role,
                Action = action,
                Entity = entity,
                OldJson = oldJson,
                NewJson = newJson,
                At = DateTime.UtcNow,
                CorrelationId = correlationId
            });
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>SEC-03.01 — In-memory JWT jti denylist until token expiry (optional; no JwtDenylist table).</summary>
    public interface IJwtDenylistService
    {
        void Deny(string jti, DateTime expiresAtUtc);
        bool IsDenied(string? jti);
    }

    public class JwtDenylistService : IJwtDenylistService
    {
        private readonly ConcurrentDictionary<string, DateTime> _denied = new();

        public void Deny(string jti, DateTime expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return;
            _denied[jti] = expiresAtUtc;
            PurgeExpired();
        }

        public bool IsDenied(string? jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return false;
            PurgeExpired();
            return _denied.ContainsKey(jti);
        }

        private void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _denied)
            {
                if (kv.Value <= now)
                    _denied.TryRemove(kv.Key, out _);
            }
        }
    }

    /// <summary>SHA-256 hex for reset tokens / OTP codes (not passwords).</summary>
    public static class SecurityTokenHash
    {
        public static string Sha256Hex(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}
