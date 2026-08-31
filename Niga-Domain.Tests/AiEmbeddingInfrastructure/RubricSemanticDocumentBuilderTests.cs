using Niga_Domain.DTOs;
using Niga_Domain.Services.AiEmbeddingInfrastructure;
using Xunit;

namespace Niga_Domain.Tests.AiEmbeddingInfrastructure;

public class RubricSemanticDocumentBuilderTests
{
    [Fact]
    public void Build_includes_all_required_sections()
    {
        var item = new RepertoryRubricCatalogItem
        {
            RubricId = 101,
            RubricName = "Mind - FEAR - death, of",
            SectionName = "Mind",
            ClinicalConcepts = { "Anticipatory dread" },
            HomeopathicConcepts = { "Fear of death" },
            KnownSynonyms = { "mortality anxiety" },
            Meanings = { "Dread about impending death." },
            SymptomExamples = { "I feel like I am going to die." },
        };

        var document = RubricSemanticDocumentBuilder.Build(item);

        Assert.Contains("Section:", document.SourceText);
        Assert.Contains("Subsection:", document.SourceText);
        Assert.Contains("Rubric:", document.SourceText);
        Assert.Contains("Clinical Concept:", document.SourceText);
        Assert.Contains("Homeopathic Concept:", document.SourceText);
        Assert.Contains("Known Synonyms:", document.SourceText);
        Assert.Contains("Meaning:", document.SourceText);
        Assert.Contains("Symptom Examples:", document.SourceText);
    }

    [Fact]
    public void Build_is_never_title_only_even_without_enrichment()
    {
        var item = new RepertoryRubricCatalogItem
        {
            RubricId = 202,
            RubricName = "Mind - ANGER - trifles, about",
            SectionName = "Mind",
        };

        var document = RubricSemanticDocumentBuilder.Build(item);

        Assert.False(RubricSemanticDocumentBuilder.IsTitleOnly(document));
        Assert.NotEqual(item.RubricName.Trim(), document.SourceText.Trim());
        Assert.True(document.SemanticFieldCount >= 3);
        Assert.False(string.IsNullOrWhiteSpace(document.TextHash));
    }

    [Fact]
    public void Build_derives_subsection_from_rubric_path()
    {
        var item = new RepertoryRubricCatalogItem
        {
            RubricId = 303,
            RubricName = "Mind - FEAR - death, of",
            SectionName = "Mind",
        };

        var document = RubricSemanticDocumentBuilder.Build(item);

        Assert.Equal("FEAR", document.Subsection);
        Assert.Equal("Mind - FEAR - death, of", document.Rubric);
    }

    [Fact]
    public void Build_produces_stable_hash_for_same_input()
    {
        var item = new RepertoryRubricCatalogItem
        {
            RubricId = 404,
            RubricName = "Generals - WEATHER - cold, agg.",
            SectionName = "Generals",
            Meanings = { "Aggravation from cold weather." },
        };

        var first = RubricSemanticDocumentBuilder.Build(item);
        var second = RubricSemanticDocumentBuilder.Build(item);

        Assert.Equal(first.TextHash, second.TextHash);
        Assert.Equal(first.SourceText, second.SourceText);
    }
}
