using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services.AudioCaseIntelligence;

namespace Niga_Domain.Repositories;

public class RubricIntelligenceAdminService : IRubricIntelligenceAdminService
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<RubricIntelligenceAdminService> _logger;

    public RubricIntelligenceAdminService(NIGACentrumContext context, ILogger<RubricIntelligenceAdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>> GetWeightsAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.HomeopathicWeightRules.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.RuleCode)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .ToListAsync(cancellationToken);

        return new RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>
        {
            Items = rows.Select(MapWeight).ToList(),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task<HomeopathicWeightRuleModel?> GetWeightByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.HomeopathicWeightRules.AsNoTracking()
            .FirstOrDefaultAsync(x => x.WeightRuleId == id, cancellationToken);
        return row == null ? null : MapWeight(row);
    }

    public async Task<(bool Success, string Message, HomeopathicWeightRuleModel? Result)> UpdateWeightAsync(
        int adminUserId, int id, HomeopathicWeightRuleUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.HomeopathicWeightRules
            .FirstOrDefaultAsync(x => x.WeightRuleId == id, cancellationToken);
        if (entity == null) return (false, "Weight rule not found.", null);

        var before = CloneWeight(entity);
        entity.WeightValue = model.WeightValue;
        entity.MultiplierValue = model.MultiplierValue;
        entity.Notes = model.Notes?.Trim();
        entity.IsActive = model.IsActive;
        entity.SetByUserId = adminUserId;

        await WriteAuditAsync("WeightRule", id, "Update", before, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Weight rule updated.", await GetWeightByIdAsync(id, cancellationToken));
    }

    public async Task<RubricIntelligenceAdminListModel<RubricMetaphorModel>> GetMetaphorsAsync(
        string? search, string? language, string? approvalStatus, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.RubricMetaphorDictionaries.AsNoTracking().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.PatientExpression.Contains(term)
                    || x.ClinicalMeaning.Contains(term)
                    || x.RubricMeaning.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(x => x.Language == language);

            if (!string.IsNullOrWhiteSpace(approvalStatus))
                query = query.Where(x => x.ApprovalStatus == approvalStatus);

            var total = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderByDescending(x => x.EnteredDate)
                .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
                .Take(Math.Max(pageSize, 1))
                .ToListAsync(cancellationToken);

            return new RubricIntelligenceAdminListModel<RubricMetaphorModel>
            {
                Items = await MapMetaphorsAsync(rows, cancellationToken),
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Metaphor list query failed — table may not exist yet.");
            return new RubricIntelligenceAdminListModel<RubricMetaphorModel> { PageNumber = pageNumber, PageSize = pageSize };
        }
    }

    public async Task<RubricMetaphorModel?> GetMetaphorByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var row = await _context.RubricMetaphorDictionaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MetaphorId == id && x.IsActive, cancellationToken);
        if (row == null) return null;
        return (await MapMetaphorsAsync(new List<RubricMetaphorDictionary> { row }, cancellationToken)).FirstOrDefault();
    }

    public async Task<(bool Success, string Message, RubricMetaphorModel? Result)> CreateMetaphorAsync(
        int adminUserId, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.PatientExpression)) return (false, "Patient expression is required.", null);

        var entity = new RubricMetaphorDictionary
        {
            PatientExpression = model.PatientExpression.Trim(),
            NormalizedExpression = IntelligenceTextNormalizer.Normalize(model.PatientExpression),
            ClinicalMeaning = model.ClinicalMeaning.Trim(),
            RubricMeaning = model.RubricMeaning.Trim(),
            SubSectionId = model.SubSectionId,
            Language = string.IsNullOrWhiteSpace(model.Language) ? "en" : model.Language.Trim(),
            ConfidenceWeight = model.ConfidenceWeight,
            ApprovalStatus = "Pending",
            EnteredBy = adminUserId,
            EnteredDate = DateTime.UtcNow,
            IsActive = true,
            VersionNo = 1,
        };

        _context.RubricMetaphorDictionaries.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("Metaphor", entity.MetaphorId, "Create", null, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return (true, "Metaphor created.", await GetMetaphorByIdAsync(entity.MetaphorId, cancellationToken));
    }

    public async Task<(bool Success, string Message, RubricMetaphorModel? Result)> UpdateMetaphorAsync(
        int adminUserId, long id, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RubricMetaphorDictionaries.FirstOrDefaultAsync(x => x.MetaphorId == id && x.IsActive, cancellationToken);
        if (entity == null) return (false, "Metaphor not found.", null);

        var before = CloneMetaphor(entity);
        entity.PatientExpression = model.PatientExpression.Trim();
        entity.NormalizedExpression = IntelligenceTextNormalizer.Normalize(model.PatientExpression);
        entity.ClinicalMeaning = model.ClinicalMeaning.Trim();
        entity.RubricMeaning = model.RubricMeaning.Trim();
        entity.SubSectionId = model.SubSectionId;
        entity.Language = string.IsNullOrWhiteSpace(model.Language) ? entity.Language : model.Language.Trim();
        entity.ConfidenceWeight = model.ConfidenceWeight;
        entity.VersionNo += 1;

        await WriteAuditAsync("Metaphor", id, "Update", before, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Metaphor updated.", await GetMetaphorByIdAsync(id, cancellationToken));
    }

    public async Task<(bool Success, string Message)> DeleteMetaphorAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RubricMetaphorDictionaries.FirstOrDefaultAsync(x => x.MetaphorId == id && x.IsActive, cancellationToken);
        if (entity == null) return (false, "Metaphor not found.");

        var before = CloneMetaphor(entity);
        entity.IsActive = false;
        entity.VersionNo += 1;
        await WriteAuditAsync("Metaphor", id, "Delete", before, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Metaphor deleted.");
    }

    public async Task<(bool Success, string Message)> ApproveMetaphorAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RubricMetaphorDictionaries.FirstOrDefaultAsync(x => x.MetaphorId == id && x.IsActive, cancellationToken);
        if (entity == null) return (false, "Metaphor not found.");

        entity.ApprovalStatus = "Approved";
        entity.ApprovedBy = adminUserId;
        entity.ApprovedDate = DateTime.UtcNow;
        await WriteAuditAsync("Metaphor", id, "Approve", null, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Metaphor approved.");
    }

    public async Task<(bool Success, string Message)> RejectMetaphorAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RubricMetaphorDictionaries.FirstOrDefaultAsync(x => x.MetaphorId == id && x.IsActive, cancellationToken);
        if (entity == null) return (false, "Metaphor not found.");

        entity.ApprovalStatus = "Rejected";
        await WriteAuditAsync("Metaphor", id, "Reject", null, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Metaphor rejected.");
    }

    public async Task<RubricIntelligenceAdminListModel<RubricAliasModel>> GetAliasesAsync(
        string? search, string? language, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = from alias in _context.RubricAliases.AsNoTracking()
                        join sub in _context.SubSectionMasters.AsNoTracking() on alias.SubSectionId equals sub.SubSectionId
                        where alias.IsActive
                        select new { alias, sub.SubSectionName };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.alias.AliasText.Contains(term) || x.SubSectionName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(x => x.alias.Language == language);

            var total = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderByDescending(x => x.alias.EnteredDate)
                .Skip((Math.Max(pageNumber, 1) - 1) * Math.Max(pageSize, 1))
                .Take(Math.Max(pageSize, 1))
                .ToListAsync(cancellationToken);

            return new RubricIntelligenceAdminListModel<RubricAliasModel>
            {
                Items = rows.Select(x => MapAlias(x.alias, x.SubSectionName)).ToList(),
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alias list query failed — table may not exist yet.");
            return new RubricIntelligenceAdminListModel<RubricAliasModel> { PageNumber = pageNumber, PageSize = pageSize };
        }
    }

    public async Task<RubricAliasModel?> GetAliasByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var row = await (
            from alias in _context.RubricAliases.AsNoTracking()
            join sub in _context.SubSectionMasters.AsNoTracking() on alias.SubSectionId equals sub.SubSectionId
            where alias.RubricAliasId == id && alias.IsActive
            select new { alias, sub.SubSectionName }).FirstOrDefaultAsync(cancellationToken);

        return row == null ? null : MapAlias(row.alias, row.SubSectionName);
    }

    public async Task<(bool Success, string Message, RubricAliasModel? Result)> CreateAliasAsync(
        int adminUserId, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (model.SubSectionId <= 0) return (false, "SubSectionId is required.", null);
        if (string.IsNullOrWhiteSpace(model.AliasText)) return (false, "Alias text is required.", null);

        var exists = await _context.SubSectionMasters.AnyAsync(x => x.SubSectionId == model.SubSectionId, cancellationToken);
        if (!exists) return (false, "SubSection not found.", null);

        var entity = new RubricAlias
        {
            SubSectionId = model.SubSectionId,
            AliasText = model.AliasText.Trim(),
            NormalizedAlias = IntelligenceTextNormalizer.Normalize(model.AliasText),
            Language = string.IsNullOrWhiteSpace(model.Language) ? "en" : model.Language.Trim(),
            AliasType = string.IsNullOrWhiteSpace(model.AliasType) ? "patient_phrase" : model.AliasType.Trim(),
            Weight = model.Weight,
            Source = "manual",
            EnteredBy = adminUserId,
            EnteredDate = DateTime.UtcNow,
            IsActive = true,
            VersionNo = 1,
        };

        _context.RubricAliases.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("Alias", entity.RubricAliasId, "Create", null, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Alias created.", await GetAliasByIdAsync(entity.RubricAliasId, cancellationToken));
    }

    public async Task<(bool Success, string Message, RubricAliasModel? Result)> UpdateAliasAsync(
        int adminUserId, long id, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RubricAliases.FirstOrDefaultAsync(x => x.RubricAliasId == id && x.IsActive, cancellationToken);
        if (entity == null) return (false, "Alias not found.", null);

        var before = CloneAlias(entity);
        entity.SubSectionId = model.SubSectionId;
        entity.AliasText = model.AliasText.Trim();
        entity.NormalizedAlias = IntelligenceTextNormalizer.Normalize(model.AliasText);
        entity.Language = string.IsNullOrWhiteSpace(model.Language) ? entity.Language : model.Language.Trim();
        entity.AliasType = string.IsNullOrWhiteSpace(model.AliasType) ? entity.AliasType : model.AliasType.Trim();
        entity.Weight = model.Weight;
        entity.ChangedBy = adminUserId;
        entity.ChangedDate = DateTime.UtcNow;
        entity.VersionNo += 1;

        await WriteAuditAsync("Alias", id, "Update", before, entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Alias updated.", await GetAliasByIdAsync(id, cancellationToken));
    }

    public async Task<(bool Success, string Message)> DeleteAliasAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RubricAliases.FirstOrDefaultAsync(x => x.RubricAliasId == id && x.IsActive, cancellationToken);
        if (entity == null) return (false, "Alias not found.");

        entity.IsActive = false;
        entity.ChangedBy = adminUserId;
        entity.ChangedDate = DateTime.UtcNow;
        entity.VersionNo += 1;
        await WriteAuditAsync("Alias", id, "Delete", CloneAlias(entity), entity, adminUserId, ipAddress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Alias deleted.");
    }

    public Task<List<RubricMetaphorDictionary>> SearchApprovedMetaphorsAsync(
        IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default)
        => SearchMetaphorsAsync(normalizedTerms, language, cancellationToken);

    public Task<List<(RubricAlias Alias, string SubSectionName)>> SearchActiveAliasesAsync(
        IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default)
        => SearchAliasesAsync(normalizedTerms, language, cancellationToken);

    internal async Task<List<RubricMetaphorDictionary>> SearchMetaphorsAsync(
        IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken)
    {
        try
        {
            var terms = normalizedTerms.Where(t => t.Length >= 3).Distinct().ToList();
            if (terms.Count == 0) return new List<RubricMetaphorDictionary>();

            var query = _context.RubricMetaphorDictionaries.AsNoTracking()
                .Where(x => x.IsActive && x.ApprovalStatus == "Approved");

            if (!string.IsNullOrWhiteSpace(language))
                query = query.Where(x => x.Language == language || x.Language == "en");

            var rows = await query.ToListAsync(cancellationToken);
            return rows
                .Where(row => terms.Any(term =>
                    row.NormalizedExpression.Contains(term, StringComparison.Ordinal)
                    || term.Contains(row.NormalizedExpression, StringComparison.Ordinal)))
                .OrderByDescending(x => x.ConfidenceWeight)
                .Take(20)
                .ToList();
        }
        catch
        {
            return GetBuiltInMetaphors(normalizedTerms);
        }
    }

    internal async Task<List<(RubricAlias Alias, string SubSectionName)>> SearchAliasesAsync(
        IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken)
    {
        try
        {
            var terms = normalizedTerms.Where(t => t.Length >= 3).Distinct().ToList();
            if (terms.Count == 0) return new List<(RubricAlias, string)>();

            var rows = await (
                from alias in _context.RubricAliases.AsNoTracking()
                join sub in _context.SubSectionMasters.AsNoTracking() on alias.SubSectionId equals sub.SubSectionId
                where alias.IsActive && (language == null || alias.Language == language || alias.Language == "en")
                select new { alias, sub.SubSectionName }).ToListAsync(cancellationToken);

            return rows
                .Where(x => terms.Any(term =>
                    x.alias.NormalizedAlias.Contains(term, StringComparison.Ordinal)
                    || term.Contains(x.alias.NormalizedAlias, StringComparison.Ordinal)))
                .OrderByDescending(x => x.alias.Weight)
                .Take(20)
                .Select(x => (x.alias, x.SubSectionName))
                .ToList();
        }
        catch
        {
            return new List<(RubricAlias, string)>();
        }
    }

    private static List<RubricMetaphorDictionary> GetBuiltInMetaphors(IEnumerable<string> normalizedTerms)
    {
        var builtIn = new List<RubricMetaphorDictionary>
        {
            new()
            {
                MetaphorId = -1,
                PatientExpression = "vibration before fit",
                NormalizedExpression = "vibration before fit",
                ClinicalMeaning = "Prodromal aura before convulsive episode",
                RubricMeaning = "GENERALITIES - CONVULSIONS - aura",
                Language = "en",
                ConfidenceWeight = 0.92m,
                ApprovalStatus = "Approved",
                IsActive = true,
            },
        };

        return builtIn.Where(m => normalizedTerms.Any(t =>
            t.Contains(m.NormalizedExpression, StringComparison.Ordinal)
            || m.NormalizedExpression.Contains(t, StringComparison.Ordinal))).ToList();
    }

    private async Task<List<RubricMetaphorModel>> MapMetaphorsAsync(
        List<RubricMetaphorDictionary> rows, CancellationToken cancellationToken)
    {
        var subIds = rows.Where(x => x.SubSectionId.HasValue).Select(x => x.SubSectionId!.Value).Distinct().ToList();
        var names = subIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.SubSectionMasters.AsNoTracking()
                .Where(x => subIds.Contains(x.SubSectionId))
                .ToDictionaryAsync(x => x.SubSectionId, x => x.SubSectionName ?? string.Empty, cancellationToken);

        return rows.Select(row => new RubricMetaphorModel
        {
            MetaphorId = row.MetaphorId,
            PatientExpression = row.PatientExpression,
            ClinicalMeaning = row.ClinicalMeaning,
            RubricMeaning = row.RubricMeaning,
            SubSectionId = row.SubSectionId,
            SubSectionName = row.SubSectionId.HasValue && names.TryGetValue(row.SubSectionId.Value, out var n) ? n : null,
            Language = row.Language,
            ConfidenceWeight = row.ConfidenceWeight,
            ApprovalStatus = row.ApprovalStatus,
            UsageCount = row.UsageCount,
            AcceptanceRate = row.AcceptanceRate,
            VersionNo = row.VersionNo,
            IsActive = row.IsActive,
        }).ToList();
    }

    private static RubricAliasModel MapAlias(RubricAlias alias, string? subSectionName) => new()
    {
        RubricAliasId = alias.RubricAliasId,
        SubSectionId = alias.SubSectionId,
        SubSectionName = subSectionName,
        AliasText = alias.AliasText,
        Language = alias.Language,
        AliasType = alias.AliasType,
        Weight = alias.Weight,
        Source = alias.Source,
        UsageCount = alias.UsageCount,
        AcceptanceRate = alias.AcceptanceRate,
        IsActive = alias.IsActive,
        VersionNo = alias.VersionNo,
    };

    private static HomeopathicWeightRuleModel MapWeight(HomeopathicWeightRule rule) => new()
    {
        WeightRuleId = rule.WeightRuleId,
        RuleCode = rule.RuleCode,
        Category = rule.Category,
        WeightValue = rule.WeightValue,
        MultiplierValue = rule.MultiplierValue,
        Description = rule.Description,
        Notes = rule.Notes,
        SetByUserId = rule.SetByUserId,
        IsActive = rule.IsActive,
    };

    private async Task WriteAuditAsync(
        string entityType, long entityId, string actionType, object? before, object after,
        int adminUserId, string? ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            _context.RubricAdminAuditLogs.Add(new RubricAdminAuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                ActionType = actionType,
                BeforeJson = before == null ? null : System.Text.Json.JsonSerializer.Serialize(before),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(after),
                AdminUserId = adminUserId,
                IpAddress = ipAddress,
                EnteredDate = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log write failed for {EntityType} {ActionType}.", entityType, actionType);
        }

        await Task.CompletedTask;
    }

    private static object CloneMetaphor(RubricMetaphorDictionary e) => new
    {
        e.MetaphorId, e.PatientExpression, e.ClinicalMeaning, e.RubricMeaning, e.SubSectionId, e.Language, e.ApprovalStatus,
    };

    private static object CloneAlias(RubricAlias e) => new
    {
        e.RubricAliasId, e.SubSectionId, e.AliasText, e.Language, e.AliasType, e.Weight,
    };

    private static object CloneWeight(HomeopathicWeightRule e) => new
    {
        e.WeightRuleId, e.RuleCode, e.Category, e.WeightValue, e.MultiplierValue, e.Notes, e.IsActive, e.SetByUserId,
    };
}
