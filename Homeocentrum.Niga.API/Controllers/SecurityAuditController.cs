using Homeocentrum.Niga.API.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>Admin read and integrity check of the append-only security audit trail (script 09).</summary>
[ApiController]
[Route("api/Admin/SecurityAudit")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
public class SecurityAuditController : ControllerBase
{
    private readonly SecurityAuditLog _audit;
    private readonly IConfiguration _configuration;

    public SecurityAuditController(SecurityAuditLog audit, IConfiguration configuration)
    {
        _audit = audit;
        _configuration = configuration;
    }

    [HttpGet("Verify")]
    public async Task<IActionResult> Verify(CancellationToken cancellationToken)
    {
        var result = await _audit.VerifyAsync(cancellationToken);
        return Ok(new
        {
            success = true,
            intact = result.FirstBrokenId == null,
            rowsChecked = result.RowsChecked,
            firstBrokenId = result.FirstBrokenId,
            problem = result.Problem,
            headHash = result.HeadHash
        });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? eventType, [FromQuery] string? outcome, [FromQuery] long? actorUserId,
        [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        const string sql = @"
SELECT SecurityAuditLogId, OccurredAtUtc, EventType, Outcome, ActorUserId, ActorRole, Subject, SourceApi, ClientIp, CorrelationId, Detail
FROM dbo.SecurityAuditLog
WHERE (@EventType IS NULL OR EventType = @EventType)
  AND (@Outcome IS NULL OR Outcome = @Outcome)
  AND (@ActorUserId IS NULL OR ActorUserId = @ActorUserId)
  AND (@FromUtc IS NULL OR OccurredAtUtc >= @FromUtc)
  AND (@ToUtc IS NULL OR OccurredAtUtc < @ToUtc)
ORDER BY SecurityAuditLogId DESC
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;";

        var rows = new List<object>();
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EventType", (object?)eventType?.Trim().ToUpperInvariant() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Outcome", (object?)outcome?.Trim().ToUpperInvariant() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ActorUserId", (object?)actorUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("@FromUtc", (object?)fromUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("@ToUtc", (object?)toUtc ?? DBNull.Value);
        command.Parameters.AddWithValue("@Skip", (page - 1) * pageSize);
        command.Parameters.AddWithValue("@Take", pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new
            {
                id = reader.GetInt64(0),
                occurredAtUtc = DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                eventType = reader.GetString(2),
                outcome = reader.GetString(3),
                actorUserId = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4),
                actorRole = reader.IsDBNull(5) ? null : reader.GetString(5),
                subject = reader.IsDBNull(6) ? null : reader.GetString(6),
                sourceApi = reader.GetString(7),
                clientIp = reader.IsDBNull(8) ? null : reader.GetString(8),
                correlationId = reader.IsDBNull(9) ? null : reader.GetString(9),
                detail = reader.IsDBNull(10) ? null : reader.GetString(10)
            });
        }
        return Ok(new { success = true, page, pageSize, data = rows });
    }
}
