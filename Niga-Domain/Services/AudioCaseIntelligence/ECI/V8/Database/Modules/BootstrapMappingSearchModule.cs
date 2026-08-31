using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 7: bootstrap mapping search (supplemental only).</summary>
public sealed class BootstrapMappingSearchModule : IEciBootstrapMappingSearchModule
{
    private readonly NIGACentrumContext _context;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<BootstrapMappingSearchModule> _logger;

    public BootstrapMappingSearchModule(
        NIGACentrumContext context,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<BootstrapMappingSearchModule> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        CancellationToken cancellationToken = default)
    {
        var text = symptom.Symptom.Symptom?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<EciCandidateRubric>();
        }

        var mappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(cancellationToken);

        var map = new Dictionary<int, EciCandidateRubric>();

        foreach (var mapping in mappings.Where(m =>
            ConceptGraphTierHelper.ConceptNamesMatch(text, m.HomeopathicConceptPattern)))
        {
            var likePattern = mapping.SubSectionNamePattern.Replace('*', '%');
            if (!likePattern.Contains('%'))
            {
                likePattern = $"%{likePattern}%";
            }

            var rubrics = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null
                    && EF.Functions.Like(s.SubSectionName, likePattern))
                .Take(_options.MaxRubricsPerPattern)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var r in rubrics)
            {
                if (string.IsNullOrWhiteSpace(r.SubSectionName))
                {
                    continue;
                }

                if (!map.TryGetValue(r.SubSectionId, out var candidate))
                {
                    candidate = new EciCandidateRubric { SubSectionId = r.SubSectionId, SubSectionName = r.SubSectionName! };
                    map[r.SubSectionId] = candidate;
                }

                candidate.Provenance.Add(new EciCandidateProvenance
                {
                    Source = EciCandidateSources.Bootstrap,
                    MatchPath = $"Bootstrap: {mapping.HomeopathicConceptPattern} → {mapping.SubSectionNamePattern}",
                    Confidence = 0.55m,
                });
            }
        }

        return map.Values.ToList();
    }
}

