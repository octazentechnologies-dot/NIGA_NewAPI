using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Conversation;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Validation;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class EciV8ConversationParserTests
{
    [Fact]
    public async Task ParseAsync_RemovesDuplicateLines_AndStripsPrefixes()
    {
        var parser = new EciConversationParser(NullLogger<EciConversationParser>.Instance);
        var input = """
Doctor: How are you?
Patient: I feel fear before every fit.
Patient: I feel fear before every fit.
""";

        var result = await parser.ParseAsync(input);

        Assert.True(result.Success);
        Assert.Contains("How are you?", result.CleanTranscript);
        Assert.Equal(2, result.Turns.Count);
        Assert.Equal(EciSpeakerRole.Doctor, result.Turns[0].Speaker);
        Assert.Equal(EciSpeakerRole.Patient, result.Turns[1].Speaker);
    }
}

public class EciV8ClinicalValidatorTests
{
    [Fact]
    public void Validate_RejectsMissingEvidence_AndDuplicates()
    {
        var validator = new EciClinicalValidator(NullLogger<EciClinicalValidator>.Instance);
        var extracted = new List<EciStructuredSymptom>
        {
            new()
            {
                Symptom = "Fear before fit",
                Evidence = "I become afraid before every fit.",
                Confidence = 0.90m,
            },
            new()
            {
                Symptom = "Fear before fit",
                Evidence = "I become afraid before every fit.",
                Confidence = 0.90m,
            },
            new()
            {
                Symptom = "Thirst",
                Evidence = "",
                Confidence = 0.80m,
            },
        };

        var result = validator.Validate(extracted, "I become afraid before every fit.");

        Assert.Equal(3, result.Validated.Count);
        Assert.Equal(1, result.Accepted.Count);
        Assert.Equal(2, result.Rejected.Count);
        Assert.Contains(result.Rejected, r => r.RejectReasons.Any(x => x.Contains("Missing transcript evidence", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(result.Rejected, r => r.RejectReasons.Any(x => x.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)));
    }
}

