using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

public class RepertoryMappingRepository : IRepertoryMappingRepository
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<RepertoryMappingRepository> _logger;

    public RepertoryMappingRepository(NIGACentrumContext context, ILogger<RepertoryMappingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<int, List<RepertoryMapModel>>> GetMapsForSubSectionsAsync(
        IEnumerable<int> subSectionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = subSectionIds.Where(x => x > 0).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, List<RepertoryMapModel>>();

        try
        {
            var rows = await (
                from map in _context.RubricRepertoryMaps.AsNoTracking()
                join source in _context.RepertorySources.AsNoTracking()
                    on map.RepertorySourceId equals source.RepertorySourceId
                where map.IsActive && source.IsActive && ids.Contains(map.SubSectionId)
                select new RepertoryMapModel
                {
                    SubSectionId = map.SubSectionId,
                    SourceCode = source.SourceCode,
                    SourceName = source.SourceName,
                    SourceRubricKey = map.SourceRubricKey,
                    SourceRubricPath = map.SourceRubricPath,
                    MappingConfidence = map.MappingConfidence,
                    IsPrimarySource = map.IsPrimarySource,
                    PriorityOrder = source.PriorityOrder,
                }).ToListAsync(cancellationToken);

            return rows
                .GroupBy(x => x.SubSectionId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.PriorityOrder).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Repertory map lookup failed — tables may not exist yet.");
            return new Dictionary<int, List<RepertoryMapModel>>();
        }
    }

    public async Task<RepertoryMappingStatusModel> GetMappingStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sources = await _context.RepertorySources.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.PriorityOrder)
                .Select(x => new RepertorySourceModel
                {
                    RepertorySourceId = x.RepertorySourceId,
                    SourceCode = x.SourceCode,
                    SourceName = x.SourceName,
                    PriorityOrder = x.PriorityOrder,
                    IsActive = x.IsActive,
                })
                .ToListAsync(cancellationToken);

            var mappedCount = await _context.RubricRepertoryMaps.AsNoTracking()
                .CountAsync(x => x.IsActive, cancellationToken);

            var kentId = sources.FirstOrDefault(x => x.SourceCode == "KENT")?.RepertorySourceId;
            var completeId = sources.FirstOrDefault(x => x.SourceCode == "COMPLETE")?.RepertorySourceId;

            var kentCount = kentId.HasValue
                ? await _context.RubricRepertoryMaps.AsNoTracking().CountAsync(x => x.IsActive && x.RepertorySourceId == kentId.Value, cancellationToken)
                : 0;

            var completeCount = completeId.HasValue
                ? await _context.RubricRepertoryMaps.AsNoTracking().CountAsync(x => x.IsActive && x.RepertorySourceId == completeId.Value, cancellationToken)
                : 0;

            return new RepertoryMappingStatusModel
            {
                ActiveSourceCount = sources.Count,
                MappedRubricCount = mappedCount,
                KentMappedCount = kentCount,
                CompleteMappedCount = completeCount,
                Sources = sources,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Repertory mapping status query failed.");
            return new RepertoryMappingStatusModel();
        }
    }
}
