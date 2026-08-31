namespace Niga_Domain.DTOs;

public static class EciV8StageNames
{
    public const string ConversationParser = "ConversationParser";
    public const string StructuredSymptomExtraction = "StructuredSymptomExtraction";
    public const string ClinicalValidation = "ClinicalValidation";
    public const string ConceptGraphEnrichment = "ConceptGraphEnrichment";
    public const string ClinicalOntology = "ClinicalOntology";
    public const string MedicalSynonyms = "MedicalSynonyms";
    public const string DatabaseIntelligence = "DatabaseIntelligence";
    public const string RubricRanking = "RubricRanking";
    public const string EvidenceVerification = "EvidenceVerification";
    public const string FinalSelection = "FinalSelection";
}

public enum EciSpeakerRole
{
    Unknown = 0,
    Doctor = 1,
    Patient = 2,
    System = 3,
    Noise = 4,
    Ignore = 5,
}

public sealed class EciDialogueTurn
{
    public EciSpeakerRole Speaker { get; set; } = EciSpeakerRole.Unknown;

    public string Text { get; set; } = string.Empty;

    public int TurnIndex { get; set; }

    public decimal Confidence { get; set; } = 0.90m;
}

public sealed class EciConversationParseResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<EciDialogueTurn> Turns { get; set; } = new();

    public string CleanTranscript { get; set; } = string.Empty;
}

public sealed class EciStructuredSymptom
{
    public string Symptom { get; set; } = string.Empty;

    public string Evidence { get; set; } = string.Empty;

    public EciSpeakerRole Speaker { get; set; } = EciSpeakerRole.Patient;

    public decimal Confidence { get; set; }

    public string? Time { get; set; }

    public string? Location { get; set; }

    public string? Modality { get; set; }

    public string? Sensation { get; set; }

    public string? Emotion { get; set; }

    public string? Trigger { get; set; }

    public string? Amelioration { get; set; }

    public string? Aggravation { get; set; }

    public string? Intensity { get; set; }

    public string? Duration { get; set; }

    public string? Frequency { get; set; }
}

public sealed class EciStructuredSymptomExtractionResult
{
    public List<EciStructuredSymptom> Symptoms { get; set; } = new();
}

public sealed class EciValidatedSymptom
{
    public EciStructuredSymptom Symptom { get; set; } = new();

    public bool Accepted { get; set; }

    public List<string> RejectReasons { get; set; } = new();
}

public sealed class EciClinicalValidationResult
{
    public List<EciValidatedSymptom> Validated { get; set; } = new();

    public List<EciValidatedSymptom> Rejected => Validated.Where(v => !v.Accepted).ToList();

    public List<EciValidatedSymptom> Accepted => Validated.Where(v => v.Accepted).ToList();
}

public sealed class EciV8StageMetric
{
    public string Stage { get; set; } = string.Empty;

    public int DurationMs { get; set; }

    public Dictionary<string, object> Counters { get; set; } = new();
}

public sealed class EciV8Diagnostics
{
    public List<EciV8StageMetric> Stages { get; set; } = new();

    public void Add(string stage, int durationMs, object? counters = null)
    {
        var dict = counters == null
            ? new Dictionary<string, object>()
            : counters.GetType()
                .GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(counters) ?? string.Empty);

        Stages.Add(new EciV8StageMetric
        {
            Stage = stage,
            DurationMs = durationMs,
            Counters = dict,
        });
    }
}

public sealed class EciV8Request
{
    public Guid SessionId { get; set; }

    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// V3 concept graph (enrichment only). Must NOT select rubrics.
    /// </summary>
    public ConceptGraphFullModel? Graph { get; set; }
}

public sealed class EciV8Result
{
    public bool Success { get; set; }

    public string EngineVersion { get; set; } = "v8.0";

    public string? Error { get; set; }

    public string CleanTranscript { get; set; } = string.Empty;

    public List<EciStructuredSymptom> ExtractedSymptoms { get; set; } = new();

    public List<EciValidatedSymptom> ValidatedSymptoms { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> FinalDatabaseRubrics { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> AiConcepts { get; set; } = new();

    public EciV8Diagnostics Diagnostics { get; set; } = new();
}

