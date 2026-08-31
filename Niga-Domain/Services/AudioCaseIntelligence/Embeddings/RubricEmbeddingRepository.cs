using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services.AudioCaseIntelligence;

namespace Niga_Domain.Services.AudioCaseIntelligence.Embeddings;

public class RubricEmbeddingRepository : IRubricEmbeddingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly NIGACentrumContext _context;
    private readonly ILogger<RubricEmbeddingRepository> _logger;

    public RubricEmbeddingRepository(
        NIGACentrumContext context,
        ILogger<RubricEmbeddingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public static string ComputeTextHash(string text)
    {
        var normalized = IntelligenceTextNormalizer.Normalize(text);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<List<RubricEmbeddingCacheEntry>> LoadAllAsync(
        string modelName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await (
                from e in _context.RubricEmbeddings.AsNoTracking()
                join s in _context.SubSectionMasters.AsNoTracking() on e.RubricId equals s.SubSectionId
                where e.ModelName == modelName && !s.DeleteStatus
                select new { e, s.SubSectionName })
                .ToListAsync(cancellationToken);

            return rows.Select(row => new RubricEmbeddingCacheEntry
            {
                RubricId = row.e.RubricId,
                SubSectionName = row.SubSectionName ?? string.Empty,
                Vector = DeserializeVector(row.e.EmbeddingJson),
                ModelName = row.e.ModelName,
            }).Where(x => x.Vector.Length > 0).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load rubric embeddings. Ensure Phase 4 SQL script 005 is deployed.");
            return new List<RubricEmbeddingCacheEntry>();
        }
    }

    public async Task<Dictionary<int, string>> GetExistingHashesAsync(
        IEnumerable<int> rubricIds,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var ids = rubricIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();

        try
        {
            return await _context.RubricEmbeddings
                .AsNoTracking()
                .Where(x => x.ModelName == modelName && ids.Contains(x.RubricId))
                .ToDictionaryAsync(x => x.RubricId, x => x.TextHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read embedding hashes.");
            return new Dictionary<int, string>();
        }
    }

    public async Task UpsertAsync(
        int rubricId,
        string subSectionName,
        float[] vector,
        string modelName,
        string textHash,
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(vector, JsonOptions);
        var existing = await _context.RubricEmbeddings
            .FirstOrDefaultAsync(x => x.RubricId == rubricId && x.ModelName == modelName, cancellationToken);

        if (existing == null)
        {
            _context.RubricEmbeddings.Add(new RubricEmbedding
            {
                RubricId = rubricId,
                EmbeddingJson = json,
                ModelName = modelName,
                TextHash = textHash,
                SourceType = sourceType,
                CreatedDate = DateTime.UtcNow,
            });
        }
        else
        {
            existing.EmbeddingJson = json;
            existing.TextHash = textHash;
            existing.SourceType = sourceType;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RubricEmbeddings
                .AsNoTracking()
                .CountAsync(x => x.ModelName == modelName, cancellationToken);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<DateTime?> GetLastUpdatedUtcAsync(string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RubricEmbeddings
                .AsNoTracking()
                .Where(x => x.ModelName == modelName)
                .Select(x => x.UpdatedDate ?? x.CreatedDate)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static float[] DeserializeVector(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<float>();
        return JsonSerializer.Deserialize<float[]>(json, JsonOptions) ?? Array.Empty<float>();
    }
}
