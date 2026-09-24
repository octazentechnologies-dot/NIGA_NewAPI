using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Accuracy pack gates for the fast path (Phases 15–17, 22, 38):
/// speaker/negation, gender, hierarchy specificity, hallucination hard-drop, evidence contract.
/// </summary>
public static class FastClinicalEvidenceGate
{
    private static readonly string[] NegationMarkers =
    {
        "no", "not", "never", "don't", "dont", "doesn't", "doesnt", "didn't", "didnt",
        "nope", "none", "without", "denied", "denies", "negative",
    };

    /// <summary>
    /// Drop / dampen symptoms that only appear as doctor questions and are patient-negated.
    /// </summary>
    public static List<AudioCaseSymptomModel> FilterSpeakerNegatedSymptoms(
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        IReadOnlyList<AudioCaseMessageModel>? messages)
    {
        if (symptoms.Count == 0 || messages == null || messages.Count == 0)
            return symptoms.ToList();

        var doctorText = string.Join('\n', messages
            .Where(m => IsDoctor(m.Role))
            .Select(m => m.Text ?? string.Empty));
        var patientText = string.Join('\n', messages
            .Where(m => IsPatient(m.Role))
            .Select(m => m.Text ?? string.Empty));

        if (string.IsNullOrWhiteSpace(doctorText) && string.IsNullOrWhiteSpace(patientText))
            return symptoms.ToList();

        return symptoms.Where(s =>
        {
            var phrase = (s.Phrase ?? string.Empty).Trim();
            if (phrase.Length < 3)
                return false;

            var inPatient = ContainsLoose(patientText, phrase);
            var inDoctor = ContainsLoose(doctorText, phrase);

            // Affirmed by patient → keep
            if (inPatient && !IsNegatedNear(patientText, phrase))
                return true;

            // Only in doctor speech and patient negated nearby → drop
            if (inDoctor && !inPatient && IsNegatedNear(patientText, phrase))
                return false;

            // Doctor-only probe with any patient negation of key tokens → drop
            if (inDoctor && !inPatient)
            {
                var keyTokens = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length >= 4)
                    .Take(3)
                    .ToList();
                if (keyTokens.Count > 0 && keyTokens.All(t => IsNegatedNear(patientText, t)))
                    return false;
            }

            return true;
        }).ToList();
    }

    public static List<ClinicalConceptModel> FilterSpeakerNegatedConcepts(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseMessageModel>? messages)
    {
        if (concepts.Count == 0 || messages == null || messages.Count == 0)
            return concepts.ToList();

        var asSymptoms = concepts
            .Select(c => new AudioCaseSymptomModel { Phrase = c.RawStatement, SearchTerms = c.SearchTerms })
            .ToList();
        var kept = FilterSpeakerNegatedSymptoms(asSymptoms, messages);
        var keptKeys = new HashSet<string>(
            kept.Select(s => (s.Phrase ?? string.Empty).Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        return concepts
            .Where(c => keptKeys.Contains((c.RawStatement ?? string.Empty).Trim().ToLowerInvariant()))
            .ToList();
    }

    /// <summary>Hard gender mismatch reject (reuses GenderRubricValidator).</summary>
    public static List<AudioCaseSuggestedRubricModel> ApplyGenderGate(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        PatientClinicalContext? patient)
    {
        if (patient?.Gender is not (0 or 1))
            return rubrics.ToList();

        return rubrics.Where(r =>
        {
            var issue = GenderRubricValidator.Validate(r, patient);
            if (issue?.IsHardReject == true)
            {
                r.Validation ??= new RubricValidationSummaryModel();
                r.Validation.Gender = patient.Gender == 0 ? "Male" : "Female";
                r.ValidationFlags.Add("GenderMismatch");
                return false;
            }

            return true;
        }).ToList();
    }

    /// <summary>
    /// Reject overly specific hierarchy leaves when patient evidence lacks those tokens
    /// (e.g. "… - Left - Index finger" without "left"/"index"/"finger" in evidence).
    /// Prefer parent when child fails specificity.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> ApplyHierarchySpecificityGate(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts,
        decimal maxUnsupportedRatio = 0.5m)
    {
        var evidenceHay = string.Join(' ',
            concepts.SelectMany(c => new[] { c.RawStatement, c.ClinicalMeaning ?? "", c.HomeopathicMeaning ?? "" }
                .Concat(c.SearchTerms ?? new List<string>()))).ToLowerInvariant();

        return rubrics.Where(r =>
        {
            var path = FastClinicalHierarchyParser.Parse(r.SubSectionName);
            r.HierarchyPath = path.Joined;
            r.HierarchyDepth = path.Depth;

            var specifics = FastClinicalHierarchyParser.SpecificityTokens(path);
            if (specifics.Count == 0)
                return true;

            var unsupported = specifics.Count(t => !evidenceHay.Contains(t, StringComparison.Ordinal));
            var ratio = unsupported / (decimal)specifics.Count;
            if (ratio > maxUnsupportedRatio)
            {
                r.ValidationFlags.Add("HierarchyOverSpecific");
                return false;
            }

            return true;
        }).ToList();
    }

    /// <summary>
    /// Phase 38: hard gate — DB-backed only, minimum evidence overlap, mark hallucination.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> ApplyHallucinationHardGate(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts,
        decimal minEvidenceScore = 0.15m)
    {
        var evidenceHay = string.Join(' ',
            concepts.SelectMany(c => new[] { c.RawStatement, c.ClinicalMeaning ?? "", c.HomeopathicMeaning ?? "" }
                .Concat(c.SearchTerms ?? new List<string>())));

        var kept = new List<AudioCaseSuggestedRubricModel>();
        foreach (var r in rubrics)
        {
            r.IsDbBacked = r.SubSectionId > 0;
            if (r.SubSectionId <= 0)
            {
                r.Validation ??= new RubricValidationSummaryModel { Hallucination = true };
                r.Validation.Hallucination = true;
                r.ValidationFlags.Add("NotDbBacked");
                continue;
            }

            var evidenceScore = ComputeEvidenceScore(r.SubSectionName, evidenceHay, r.MatchedFrom);
            r.EvidenceScore = evidenceScore;

            if (evidenceScore < minEvidenceScore)
            {
                r.Validation ??= new RubricValidationSummaryModel { Hallucination = true };
                r.Validation.Hallucination = true;
                r.ValidationFlags.Add("InsufficientEvidence");
                continue;
            }

            r.Validation ??= new RubricValidationSummaryModel { Hallucination = false };
            r.Validation.Hallucination = false;
            kept.Add(r);
        }

        return kept;
    }

    /// <summary>Phase 22: attach doctor-facing evidence contract fields on finals.</summary>
    public static void AttachEvidenceContract(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts)
    {
        foreach (var r in rubrics)
        {
            r.IsDbBacked = r.SubSectionId > 0;
            r.DiscoveryMethod ??= r.MatchSource ?? r.MatchLayer ?? "FastClinical";
            r.CanonicalScore ??= r.ConfidenceScore;

            var concept = ResolveConcept(r, concepts);
            var patientEvidence = concept?.RawStatement
                ?? r.MatchedFrom
                ?? r.Explainability?.PatientStatement
                ?? r.MatchedFromDetail?.PatientStatement;

            r.PatientEvidence = patientEvidence;
            r.EvidenceType = InferEvidenceType(r.MatchSource, concept);

            if (r.EvidenceScore == null)
            {
                var hay = string.Join(' ',
                    concepts.SelectMany(c => new[] { c.RawStatement, c.ClinicalMeaning ?? "" }
                        .Concat(c.SearchTerms ?? new List<string>())));
                r.EvidenceScore = ComputeEvidenceScore(r.SubSectionName, hay, r.MatchedFrom);
            }

            r.EvidenceChainComplete = !string.IsNullOrWhiteSpace(r.PatientEvidence)
                && r.SubSectionId > 0
                && (r.EvidenceScore ?? 0) >= 0.15m;

            r.MatchedFromDetail ??= new RubricMatchedFromModel();
            r.MatchedFromDetail.PatientStatement ??= r.PatientEvidence;
            r.MatchedFromDetail.ClinicalConcept ??= concept?.ClinicalMeaning;
            r.MatchedFromDetail.HomeopathicConcept ??= concept?.HomeopathicMeaning;

            if (string.IsNullOrWhiteSpace(r.HierarchyPath))
            {
                var path = FastClinicalHierarchyParser.Parse(r.SubSectionName);
                r.HierarchyPath = path.Joined;
                r.HierarchyDepth = path.Depth;
            }
        }
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

        if (string.IsNullOrWhiteSpace(rubric.MatchedFrom) && string.IsNullOrWhiteSpace(rubric.PatientEvidence))
            return null;

        var probe = rubric.MatchedFrom ?? rubric.PatientEvidence;
        return concepts.FirstOrDefault(c =>
            string.Equals(c.RawStatement, probe, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.ClinicalMeaning, probe, StringComparison.OrdinalIgnoreCase)
            || (c.SearchTerms?.Any(t => string.Equals(t, probe, StringComparison.OrdinalIgnoreCase)) ?? false));
    }

    private static string InferEvidenceType(string? matchSource, ClinicalConceptModel? concept)
    {
        var src = (matchSource ?? string.Empty).ToLowerInvariant();
        if (src.Contains("embed")) return "Semantic";
        if (src.Contains("alias")) return "Alias";
        if (src.Contains("keyword") || src.Contains("fts")) return "Lexical";
        if (src.Contains("exact") || src.Contains("database") || src.Contains("v1")) return "Exact";
        if (concept != null) return "Concept";
        return "Lexical";
    }

    internal static decimal ComputeEvidenceScore(string? rubricName, string conceptHay, string? matchedFrom)
    {
        if (string.IsNullOrWhiteSpace(conceptHay) && string.IsNullOrWhiteSpace(matchedFrom))
            return 0;

        var tokens = (rubricName ?? string.Empty)
            .Split(new[] { ' ', '-', ',', ';', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length >= 3 && t is not ("the" or "and" or "with" or "from" or "for"))
            .Distinct()
            .ToList();

        if (tokens.Count == 0)
            return string.IsNullOrWhiteSpace(matchedFrom) ? 0 : 0.4m;

        var hay = (conceptHay + " " + (matchedFrom ?? "")).ToLowerInvariant();
        var hits = tokens.Count(t => hay.Contains(t, StringComparison.Ordinal));
        var score = hits / (decimal)tokens.Count;
        return score < 0 ? 0 : score > 1 ? 1 : score;
    }

    private static bool IsDoctor(string? role) =>
        !string.IsNullOrWhiteSpace(role)
        && (role.Contains("doctor", StringComparison.OrdinalIgnoreCase)
            || role.Contains("physician", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "clinician", StringComparison.OrdinalIgnoreCase));

    private static bool IsPatient(string? role) =>
        !string.IsNullOrWhiteSpace(role)
        && (role.Contains("patient", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "user", StringComparison.OrdinalIgnoreCase));

    private static bool ContainsLoose(string hay, string needle)
    {
        if (string.IsNullOrWhiteSpace(hay) || string.IsNullOrWhiteSpace(needle))
            return false;
        return hay.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || needle.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Where(t => t.Length >= 4)
                   .Any(t => hay.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNegatedNear(string patientText, string phrase)
    {
        if (string.IsNullOrWhiteSpace(patientText) || string.IsNullOrWhiteSpace(phrase))
            return false;

        var lower = patientText.ToLowerInvariant();
        var probe = phrase.Trim().ToLowerInvariant();
        var idx = lower.IndexOf(probe, StringComparison.Ordinal);
        if (idx < 0)
        {
            // Try first meaningful token
            var token = probe.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(t => t.Length >= 4);
            if (token == null) return false;
            idx = lower.IndexOf(token, StringComparison.Ordinal);
            if (idx < 0) return false;
            probe = token;
        }

        var start = Math.Max(0, idx - 40);
        var len = Math.Min(lower.Length - start, probe.Length + 80);
        var window = lower.Substring(start, len);
        return NegationMarkers.Any(n =>
            window.Contains($" {n} ", StringComparison.Ordinal)
            || window.StartsWith($"{n} ", StringComparison.Ordinal)
            || window.Contains($"{n} {probe}", StringComparison.Ordinal));
    }
}
