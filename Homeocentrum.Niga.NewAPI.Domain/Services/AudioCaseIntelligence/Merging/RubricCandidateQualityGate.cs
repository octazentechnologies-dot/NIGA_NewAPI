using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Shared post-processing applied after ANY discovery path produces candidates,
/// before ranking/display. Prevents path-by-path fix regressions (Bugs A–D).
/// </summary>
public static class RubricCandidateQualityGate
{
    private static readonly string[] HitchhikerMarkers =
    {
        "ACCOMPANIED BY",
        "ACCOMPANIED WITH",
        ", WITH",
        "-WITH-",
        " THIRST, WITH",
        " THIRST AND ",
    };

    /// <summary>
    /// Run all shared correctness gates on the merged candidate set.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> Apply(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel>? concepts = null)
    {
        if (rubrics.Count == 0)
            return new List<AudioCaseSuggestedRubricModel>();

        var working = rubrics.Select(CloneShallow).ToList();

        working = FilterSubstringCollisions(working);
        working = RescoreAndFilterHitchhikers(working, concepts);
        working = LockCitationsToSourceConcept(working, concepts);
        working = DeduplicateAiConceptsWhenDbMatched(working);

        return working;
    }

    /// <summary>
    /// Drop repertory hits whose search term only substring-collides with the rubric name
    /// (e.g. drop→dropsy) unless a longer phrase from MatchedFrom still word-matches.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> FilterSubstringCollisions(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics)
    {
        return rubrics.Where(r =>
        {
            if (!EnterpriseRubricPresentationHelper.IsAuthoritativeRepertory(r)
                && !string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindRepertory, StringComparison.OrdinalIgnoreCase)
                && r.SubSectionId <= 0)
            {
                return true; // keep AI concepts for Bug D gate
            }

            var name = r.SubSectionName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Prefer MatchedFrom / WhySuggested search term; fall back to short tokens from MatchedFrom.
            var probes = BuildMatchProbes(r);
            if (probes.Count == 0)
                return true; // no probe — leave for other gates

            // Accept if ANY probe whole-word matches; reject if the only hits are substring collisions.
            if (probes.Any(p => WordBoundaryMatcher.Matches(name, p)))
                return true;

            // Soft keep when probe is long phrase (≥12 chars) and Contained — already checked Matches.
            // Hard reject short-token collisions (drop/dropsy, fit/fitness-style false friends).
            if (probes.Any(p => p.Length <= 6 && WordBoundaryMatcher.IsSubstringCollision(name, p)))
                return false;

            // No whole-word probe matched — reject high-confidence false friends from hotspot LIKE.
            return probes.All(p => p.Length > 6) && probes.Any(p =>
                name.Contains(p, StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }

    /// <summary>
    /// Re-apply section-anchor / hitchhiker scoring uniformly; drop candidates that fail threshold.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> RescoreAndFilterHitchhikers(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel>? concepts)
    {
        var conceptList = concepts ?? Array.Empty<ClinicalConceptModel>();
        var output = new List<AudioCaseSuggestedRubricModel>();

        foreach (var rubric in rubrics)
        {
            if (string.Equals(rubric.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase)
                || rubric.SubSectionId <= 0)
            {
                output.Add(rubric);
                continue;
            }

            var concept = ResolveConcept(rubric, conceptList);
            var domain = concept != null
                ? ConceptSearchTermBuilder.ResolveDomain(concept)
                : InferDomainFromMatchedFrom(rubric.MatchedFrom);
            var searchTerm = ExtractPrimarySearchTerm(rubric, concept);

            var score = ConceptSearchTermBuilder.ScoreCandidate(
                concept ?? new ClinicalConceptModel
                {
                    ClinicalMeaning = rubric.MatchedFrom,
                    RawStatement = rubric.MatchedFrom ?? string.Empty,
                    Category = domain,
                },
                domain,
                searchTerm,
                rubric.SubSectionName);

            // Extra hitchhiker pass for markers ScoreCandidate may miss (e.g. "thirst, with" on BLADDER).
            if (IsHitchhikerRubric(rubric.SubSectionName, domain))
                score = Math.Min(score, 0.25m);

            if (score < 0.40m)
                continue;

            // Never let a hitchhiker keep a 99% badge — clamp to rescored value.
            rubric.MatchScore = Math.Min(rubric.MatchScore, score);
            if (rubric.ConfidenceScore.HasValue)
                rubric.ConfidenceScore = Math.Min(rubric.ConfidenceScore.Value, score);
            if (rubric.Scores != null)
                rubric.Scores.FinalHybridScore = Math.Min(rubric.Scores.FinalHybridScore ?? score, score);

            output.Add(rubric);
        }

        return output;
    }

    /// <summary>
    /// Citations must come from the rubric's own SourceConceptId / MatchedFrom — never fuzzy-steal another concept.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> LockCitationsToSourceConcept(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel>? concepts)
    {
        var conceptList = concepts ?? Array.Empty<ClinicalConceptModel>();

        foreach (var rubric in rubrics)
        {
            ClinicalConceptModel? concept = null;
            if (rubric.SourceConceptId.HasValue)
                concept = conceptList.FirstOrDefault(c => c.ConceptId == rubric.SourceConceptId.Value);

            // Exact MatchedFrom alignment only — no fuzzy cross-concept scoring.
            if (concept == null && !string.IsNullOrWhiteSpace(rubric.MatchedFrom))
            {
                concept = conceptList.FirstOrDefault(c =>
                    string.Equals(c.RawStatement, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.ClinicalMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.HomeopathicMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase));
            }

            if (concept == null)
            {
                // Strip contaminated explainability labels that don't match MatchedFrom.
                if (rubric.Explainability != null
                    && !string.IsNullOrWhiteSpace(rubric.MatchedFrom)
                    && !string.IsNullOrWhiteSpace(rubric.Explainability.PatientStatement)
                    && !string.Equals(rubric.Explainability.PatientStatement, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                    && !(rubric.MatchedFrom?.Contains(rubric.Explainability.PatientStatement, StringComparison.OrdinalIgnoreCase) ?? false)
                    && !rubric.Explainability.PatientStatement.Contains(rubric.MatchedFrom!, StringComparison.OrdinalIgnoreCase))
                {
                    rubric.Explainability.PatientStatement = rubric.MatchedFrom;
                    rubric.Explainability.ClinicalMeaning = rubric.MatchedFrom;
                    rubric.Explainability.HomeopathicMeaning = rubric.MatchedFrom;
                }

                continue;
            }

            rubric.SourceConceptId ??= concept.ConceptId;
            rubric.MatchedFrom ??= concept.RawStatement ?? concept.ClinicalMeaning;
            rubric.Explainability ??= new RubricExplainabilityModel();
            rubric.Explainability.SourceConceptId = concept.ConceptId;
            rubric.Explainability.PatientStatement = concept.RawStatement ?? concept.ClinicalMeaning;
            rubric.Explainability.ClinicalMeaning = concept.ClinicalMeaning ?? concept.RawStatement;
            rubric.Explainability.HomeopathicMeaning = concept.HomeopathicMeaning ?? concept.ClinicalMeaning;
            rubric.MatchedFromDetail ??= new RubricMatchedFromModel();
            rubric.MatchedFromDetail.PatientStatement = concept.RawStatement;
            rubric.MatchedFromDetail.NormalizedMeaning = concept.ClinicalMeaning;
            rubric.MatchedFromDetail.ClinicalConcept = concept.ClinicalMeaning;
            rubric.MatchedFromDetail.HomeopathicConcept = concept.HomeopathicMeaning;
        }

        return rubrics.ToList();
    }

    /// <summary>
    /// Section B (AI Clinical Concept) only when no Section A DB match exists for that concept.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> DeduplicateAiConceptsWhenDbMatched(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics)
    {
        var dbMatches = rubrics
            .Where(r => r.SubSectionId > 0
                && !string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (dbMatches.Count == 0)
            return rubrics.ToList();

        var coveredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var db in dbMatches)
        {
            if (db.SourceConceptId.HasValue)
                coveredKeys.Add($"id:{db.SourceConceptId.Value}");
            foreach (var key in ConceptKeys(db.MatchedFrom, db.Explainability?.ClinicalMeaning, db.Explainability?.HomeopathicMeaning, db.Explainability?.PatientStatement))
                coveredKeys.Add(key);
        }

        return rubrics.Where(r =>
        {
            if (!string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase)
                && !(r.IsAiSuggested && r.SubSectionId <= 0))
            {
                return true;
            }

            if (r.SourceConceptId.HasValue && coveredKeys.Contains($"id:{r.SourceConceptId.Value}"))
                return false;

            foreach (var key in ConceptKeys(r.SubSectionName, r.MatchedFrom, r.Explainability?.ClinicalMeaning, r.Explainability?.HomeopathicMeaning))
            {
                if (coveredKeys.Contains(key))
                    return false;

                // Fuzzy: AI concept name appears in any DB MatchedFrom / why text.
                if (dbMatches.Any(db => ConceptTextOverlaps(key, db)))
                    return false;
            }

            return true;
        }).ToList();
    }

    public static bool IsHitchhikerRubric(string? rubricName, string domain)
    {
        if (string.IsNullOrWhiteSpace(rubricName))
            return false;

        var upper = rubricName.ToUpperInvariant();
        var hasHitchMarker = HitchhikerMarkers.Any(m => upper.Contains(m, StringComparison.Ordinal));

        if (domain == "thirst")
        {
            if (upper.StartsWith("STOMACH") && WordBoundaryMatcher.ContainsWord(upper, "THIRST"))
                return false;
            if (hasHitchMarker && WordBoundaryMatcher.ContainsWord(upper, "THIRST"))
                return true;
            // BLADDER/ABDOMEN/... with thirst as concomitant, not primary stomach thirst.
            if (WordBoundaryMatcher.ContainsWord(upper, "THIRST")
                && (upper.StartsWith("BLADDER") || upper.StartsWith("ABDOMEN") || upper.StartsWith("RECTUM")
                    || upper.StartsWith("KIDNEY") || upper.StartsWith("URETHRA")))
                return true;
        }

        if (domain is "salt" or "sexual" or "awkward" or "sleep-talk" or "height-fear" or "fear-fit")
        {
            if (hasHitchMarker)
                return true;
        }

        if (domain == "sleep-talk")
        {
            if (upper.StartsWith("ABDOMEN") || upper.StartsWith("BLADDER") || upper.StartsWith("RECTUM")
                || upper.StartsWith("CHEST") || upper.StartsWith("STOMACH"))
                return true;
            if (WordBoundaryMatcher.ContainsWord(upper, "SLEEP")
                && !WordBoundaryMatcher.ContainsWord(upper, "TALK")
                && !WordBoundaryMatcher.ContainsWord(upper, "TALKING"))
                return true;
        }

        return hasHitchMarker && !upper.StartsWith("STOMACH") && !upper.StartsWith("MIND") && !upper.StartsWith("GENERAL");
    }

    private static List<string> BuildMatchProbes(AudioCaseSuggestedRubricModel rubric)
    {
        var probes = new List<string>();
        void Add(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            var t = s.Trim();
            if (t.Length < 3) return;
            if (!probes.Contains(t, StringComparer.OrdinalIgnoreCase))
                probes.Add(t);
        }

        Add(rubric.MatchedFrom);
        if (!string.IsNullOrWhiteSpace(rubric.WhySuggested))
        {
            // "Per-concept keyword hit via 'drops things' (domain=awkward...)"
            var via = rubric.WhySuggested;
            var start = via.IndexOf('\'');
            var end = via.LastIndexOf('\'');
            if (start >= 0 && end > start)
                Add(via[(start + 1)..end]);
        }

        foreach (var token in WordBoundaryMatcher.Tokenize(rubric.MatchedFrom ?? string.Empty))
        {
            if (token.Length >= 4)
                Add(token);
        }

        return probes;
    }

    private static ClinicalConceptModel? ResolveConcept(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts)
    {
        if (rubric.SourceConceptId.HasValue)
        {
            var linked = concepts.FirstOrDefault(c => c.ConceptId == rubric.SourceConceptId.Value);
            if (linked != null) return linked;
        }

        if (string.IsNullOrWhiteSpace(rubric.MatchedFrom))
            return null;

        return concepts.FirstOrDefault(c =>
            string.Equals(c.RawStatement, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.ClinicalMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtractPrimarySearchTerm(AudioCaseSuggestedRubricModel rubric, ClinicalConceptModel? concept)
    {
        if (concept?.SearchTerms.Count > 0)
            return concept.SearchTerms[0];
        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
            return rubric.MatchedFrom!;
        return rubric.SubSectionName ?? string.Empty;
    }

    private static string InferDomainFromMatchedFrom(string? matchedFrom)
    {
        if (string.IsNullOrWhiteSpace(matchedFrom))
            return "general";
        return ConceptSearchTermBuilder.ResolveDomain(new ClinicalConceptModel
        {
            ClinicalMeaning = matchedFrom,
            RawStatement = matchedFrom,
        });
    }

    private static IEnumerable<string> ConceptKeys(params string?[] parts)
    {
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            var norm = NormalizeKey(part);
            if (norm.Length >= 3)
                yield return norm;
        }
    }

    private static string NormalizeKey(string text) =>
        string.Join(' ', WordBoundaryMatcher.Tokenize(text));

    private static bool ConceptTextOverlaps(string aiKey, AudioCaseSuggestedRubricModel db)
    {
        var hay = NormalizeKey($"{db.MatchedFrom} {db.Explainability?.PatientStatement} {db.Explainability?.ClinicalMeaning} {db.SubSectionName} {db.WhySuggested}");
        if (string.IsNullOrWhiteSpace(hay) || string.IsNullOrWhiteSpace(aiKey))
            return false;

        // Require substantial overlap: AI key tokens mostly present in DB citation/rubric.
        var aiTokens = aiKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (aiTokens.Length == 0) return false;
        var hits = aiTokens.Count(t => hay.Contains(t, StringComparison.OrdinalIgnoreCase));
        return hits >= Math.Max(1, (aiTokens.Length + 1) / 2);
    }

    private static AudioCaseSuggestedRubricModel CloneShallow(AudioCaseSuggestedRubricModel r) => r;
}
