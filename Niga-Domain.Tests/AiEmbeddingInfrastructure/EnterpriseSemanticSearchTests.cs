using Niga_Domain.DTOs;
using Niga_Domain.Services.AiEmbeddingInfrastructure;
using Xunit;

namespace Niga_Domain.Tests.AiEmbeddingInfrastructure;

public class ConceptSemanticDocumentParserTests
{
    [Fact]
    public void Parse_reads_semantic_fields()
    {
        var text = """
            Clinical Concept: anticipatory fear
            Homeopathic Concept: Fear
            Known Synonyms: anxiety before events; dread
            Meaning: Fear appearing before an anticipated event.
            Linked Rubrics: 101; 202
            """;

        var parsed = ConceptSemanticDocumentParser.Parse(text);

        Assert.Equal("anticipatory fear", parsed.ClinicalConcept);
        Assert.Equal("Fear", parsed.HomeopathicConcept);
        Assert.Equal(2, parsed.KnownSynonyms.Count);
        Assert.Contains(101, parsed.LinkedRubricIds);
        Assert.Contains(202, parsed.LinkedRubricIds);
    }

    [Fact]
    public void Compose_roundtrip_preserves_clinical_concept()
    {
        var text = ConceptSemanticDocumentParser.Compose(
            "restlessness",
            "Restlessness",
            new[] { "cannot sit still" },
            new[] { "Internal unease with motor impulse." },
            new[] { 55 });

        var parsed = ConceptSemanticDocumentParser.Parse(text);
        Assert.Equal("restlessness", parsed.ClinicalConcept);
        Assert.Equal("Restlessness", parsed.HomeopathicConcept);
        Assert.Contains(55, parsed.LinkedRubricIds);
    }
}

public class ClinicalConceptInputGuardTests
{
    [Fact]
    public void Validate_rejects_transcript_like_input()
    {
        var options = new Configuration.AiEmbeddingInfrastructureOptions
        {
            MaxClinicalConceptInputLength = 300,
            TranscriptRejectionMinWords = 25,
        };

        var transcript = string.Join(' ', Enumerable.Repeat("word", 30));
        var (valid, error) = ClinicalConceptInputGuard.Validate(transcript, options);

        Assert.False(valid);
        Assert.Contains("transcript", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_accepts_short_clinical_concept()
    {
        var options = new Configuration.AiEmbeddingInfrastructureOptions
        {
            MaxClinicalConceptInputLength = 300,
            TranscriptRejectionMinWords = 25,
        };

        var (valid, error) = ClinicalConceptInputGuard.Validate("anticipatory fear", options);

        Assert.True(valid);
        Assert.Null(error);
    }
}

public class EnterpriseEmbeddingVectorSearchTests
{
    [Fact]
    public void TopConceptMatches_orders_by_cosine()
    {
        var entries = new List<AiConceptEmbeddingCacheEntry>
        {
            new() { ConceptKey = "low", Vector = new[] { 0f, 1f, 0f } },
            new() { ConceptKey = "high", Vector = new[] { 1f, 0f, 0f } },
        };

        var matches = EnterpriseEmbeddingVectorSearch.TopConceptMatches(
            new[] { 1f, 0f, 0f },
            entries,
            topK: 2,
            minScore: 0m);

        Assert.Equal("high", matches[0].Entry.ConceptKey);
    }
}
