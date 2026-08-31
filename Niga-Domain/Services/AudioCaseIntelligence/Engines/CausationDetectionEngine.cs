using System.Text.RegularExpressions;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class CausationDetectionEngine : ICausationDetectionEngine
{
    private static readonly (Regex Pattern, string LinkType)[] CausationPatterns =
    {
        (new Regex(@"\b(?:after|following|since|because of|due to|from)\s+(.{2,80}?)\s+(?:came|developed|started|got|had|began|appeared|occurred|happened|triggered|led to|resulted in|caused)\s+(.{2,80}?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "CauseEffect"),
        (new Regex(@"\b(.{2,60}?)\s+(?:led to|resulted in|caused|triggered|brought on)\s+(.{2,80}?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "CauseEffect"),
        (new Regex(@"\b(?:after|since)\s+(.{2,80}?)\s*,?\s*(.{2,80}?)\s+(?:started|began|appeared|worsened)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "CauseEffect"),
    };

    public CausationDetectionResult Detect(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string transcript)
    {
        var enrichedConcepts = concepts.Select(Clone).ToList();
        var links = new List<CausationLinkModel>();
        var sequence = 0;

        // Only infer causation from explicit causal language in the transcript — never link two
        // temporally-adjacent particulars/concomitants without a stated cause-effect relationship.
        if (!string.IsNullOrWhiteSpace(transcript))
        {
            foreach (var (pattern, linkType) in CausationPatterns)
            {
                foreach (Match match in pattern.Matches(transcript))
                {
                    if (!match.Success || match.Groups.Count < 3) continue;

                    var causeText = match.Groups[1].Value.Trim().TrimEnd(',', '.');
                    var effectText = match.Groups[2].Value.Trim().TrimEnd(',', '.');
                    if (causeText.Length < 3 || effectText.Length < 3) continue;

                    var causeConcept = FindBestConceptMatch(enrichedConcepts, causeText);
                    var effectConcept = FindBestConceptMatch(enrichedConcepts, effectText);
                    if (causeConcept == null || effectConcept == null) continue;
                    if (causeConcept.ConceptId == effectConcept.ConceptId) continue;

                    links.Add(new CausationLinkModel
                    {
                        LinkId = Guid.NewGuid(),
                        CauseConceptId = causeConcept.ConceptId,
                        EffectConceptId = effectConcept.ConceptId,
                        CauseText = causeText,
                        EffectText = effectText,
                        LinkType = linkType,
                        Confidence = Math.Min(0.90m, Math.Max(causeConcept.Confidence, effectConcept.Confidence)),
                        SequenceOrder = ++sequence,
                    });
                }
            }
        }

        links = links
            .GroupBy(l => $"{l.CauseText}|{l.EffectText}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Confidence).First())
            .OrderBy(l => l.SequenceOrder)
            .ToList();

        for (var i = 0; i < links.Count; i++)
        {
            links[i].SequenceOrder = i + 1;
        }

        return new CausationDetectionResult
        {
            Concepts = enrichedConcepts,
            Links = links,
        };
    }

    private static ClinicalConceptModel? FindBestConceptMatch(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string phrase)
    {
        var normalized = IntelligenceTextNormalizer.Normalize(phrase);
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        return concepts
            .Select(c => new
            {
                Concept = c,
                Score = ScoreConceptMatch(c, normalized),
            })
            .Where(x => x.Score > 0.4m)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Concept)
            .FirstOrDefault();
    }

    private static decimal ScoreConceptMatch(ClinicalConceptModel concept, string normalizedPhrase)
    {
        var candidates = new[]
        {
            IntelligenceTextNormalizer.Normalize(concept.RawStatement),
            IntelligenceTextNormalizer.Normalize(concept.ClinicalMeaning ?? string.Empty),
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        foreach (var candidate in candidates)
        {
            if (candidate.Contains(normalizedPhrase, StringComparison.Ordinal)
                || normalizedPhrase.Contains(candidate, StringComparison.Ordinal))
            {
                return 0.9m;
            }

            var overlap = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Count(token => normalizedPhrase.Contains(token, StringComparison.Ordinal));
            if (overlap >= 2) return 0.75m;
            if (overlap == 1) return 0.5m;
        }

        return 0m;
    }

    private static ClinicalConceptModel Clone(ClinicalConceptModel source) => new()
    {
        ConceptId = source.ConceptId,
        RawStatement = source.RawStatement,
        ClinicalMeaning = source.ClinicalMeaning,
        HomeopathicMeaning = source.HomeopathicMeaning,
        Category = source.Category,
        IsSRP = source.IsSRP,
        Modalities = source.Modalities.ToList(),
        Concomitants = source.Concomitants.ToList(),
        SearchTerms = source.SearchTerms.ToList(),
        Confidence = source.Confidence,
        SourceLanguage = source.SourceLanguage,
        HomeopathicWeight = source.HomeopathicWeight,
        SequenceOrder = source.SequenceOrder,
    };
}
