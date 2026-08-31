using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Repositories;
using Niga_Domain.Services.AudioCaseIntelligence;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class RubricAliasEngine : IRubricAliasEngine
{
    private readonly IRubricIntelligenceAdminService _adminService;

    public RubricAliasEngine(IRubricIntelligenceAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<RubricAliasSearchResult> SearchRubricsAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        string? language,
        CancellationToken cancellationToken = default)
    {
        var terms = concepts
            .SelectMany(c => c.SearchTerms.Concat(new[] { c.RawStatement, c.ClinicalMeaning ?? string.Empty }))
            .Concat(symptoms.SelectMany(s => s.SearchTerms.Concat(new[] { s.Phrase })))
            .Select(IntelligenceTextNormalizer.Normalize)
            .Where(t => t.Length >= 3)
            .Distinct()
            .ToList();

        var hits = await _adminService.SearchActiveAliasesAsync(terms, language, cancellationToken);
        var rubrics = new Dictionary<int, AudioCaseSuggestedRubricModel>();

        foreach (var (alias, subSectionName) in hits)
        {
            if (rubrics.ContainsKey(alias.SubSectionId)) continue;

            rubrics[alias.SubSectionId] = new AudioCaseSuggestedRubricModel
            {
                SubSectionId = alias.SubSectionId,
                SubSectionName = subSectionName,
                MatchScore = alias.Weight * 0.9m,
                ConfidenceScore = alias.Weight,
                SuggestedIntensityNo = 2,
                MatchedFrom = alias.AliasText,
                MatchSource = "Alias",
                WhySuggested = $"Matched rubric alias '{alias.AliasText}'",
                EngineVersion = "v2",
                RequiresManualApproval = true,
            };
        }

        foreach (var concept in concepts)
        {
            // Also match metaphors that resolved to rubric names via SubSection lookup is handled in alias search
        }

        return new RubricAliasSearchResult
        {
            Rubrics = rubrics.Values.OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore).Take(10).ToList(),
            AliasesMatched = hits.Count,
        };
    }
}
