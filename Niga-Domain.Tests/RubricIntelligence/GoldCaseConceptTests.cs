using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class GoldCaseConceptTests
{
    public static IEnumerable<object[]> GoldTranscripts => new List<object[]>
    {
        new object[] { "Epilepsy", "Vibration in hands 10 seconds before every fit at night.", new[] { "aura", "convulsion", "prodrom" } },
        new object[] { "Epilepsy", "Patient drops things from hands before seizure starts.", new[] { "drop", "seizure", "convulsion" } },
        new object[] { "Psychological", "Since father's death she cannot sleep and weeps alone.", new[] { "grief", "weep", "sleep" } },
        new object[] { "Psychological", "Intense fear of darkness since childhood.", new[] { "fear", "dark" } },
        new object[] { "Gastrointestinal", "Burning pain in stomach worse after eating spicy food.", new[] { "burn", "stomach", "eating" } },
        new object[] { "Gastrointestinal", "Craves sweets and salt together.", new[] { "crav", "sweet", "salt" } },
        new object[] { "Pediatric", "Child screams before urination with red face.", new[] { "scream", "urin" } },
        new object[] { "Pediatric", "Baby kicks legs and turns blue during cough.", new[] { "cough", "blue" } },
        new object[] { "Chronic", "Joint pain worse in damp weather, better by warmth.", new[] { "joint", "damp", "warm" } },
        new object[] { "Chronic", "Migraine every Sunday morning with nausea.", new[] { "migraine", "morning", "nausea" } },
    };

    private readonly ClinicalReasoningEngine _clinicalEngine = new();
    private readonly HomeopathicReasoningEngine _homeopathicEngine = new();

    [Theory]
    [MemberData(nameof(GoldTranscripts))]
    public void Phase1Engines_ProduceClinicalConcept_ForGoldTranscript(string category, string transcript, string[] expectedKeywords)
    {
        var concept = new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            RawStatement = transcript,
            Category = category,
            Confidence = 0.8m,
        };

        var enriched = _homeopathicEngine.Enrich(_clinicalEngine.Enrich(new List<ClinicalConceptModel> { concept }));
        var result = enriched[0];

        Assert.False(string.IsNullOrWhiteSpace(result.ClinicalMeaning));
        var combined = $"{result.ClinicalMeaning} {result.HomeopathicMeaning} {result.RawStatement}".ToLowerInvariant();
        Assert.True(
            expectedKeywords.Any(k => combined.Contains(k, StringComparison.Ordinal)),
            $"Expected one of [{string.Join(", ", expectedKeywords)}] in concept output for: {transcript}");
    }
}
