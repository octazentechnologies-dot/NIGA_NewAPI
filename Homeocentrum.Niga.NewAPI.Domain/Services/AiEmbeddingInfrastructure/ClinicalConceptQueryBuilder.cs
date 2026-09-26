using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public static class ClinicalConceptQueryBuilder
{
    public static string BuildQueryText(string clinicalConcept) =>
        BuildQueryText(clinicalConcept, null, null, false, null);

    /// <summary>Phase 6: embed clinical + homeopathic meaning, keywords, SRP, synonyms, and context.</summary>
    public static string BuildQueryText(
        string? clinicalConcept,
        string? homeopathicConcept = null,
        PatientMeaningNodeModel? meaning = null,
        bool isSrp = false,
        IEnumerable<string>? extraSynonyms = null)
    {
        var clinical = clinicalConcept?.Trim() ?? string.Empty;
        var homeopathic = homeopathicConcept?.Trim();
        var synonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(clinical))
        {
            synonyms.Add(clinical);
        }

        if (!string.IsNullOrWhiteSpace(homeopathic))
        {
            synonyms.Add(homeopathic);
        }

        if (!string.IsNullOrWhiteSpace(meaning?.NormalizedMeaning))
        {
            synonyms.Add(meaning.NormalizedMeaning.Trim());
        }

        foreach (var token in extraSynonyms ?? [])
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                synonyms.Add(token.Trim());
            }
        }

        var meanings = new List<string>();
        if (!string.IsNullOrWhiteSpace(meaning?.RawStatement))
        {
            meanings.Add(meaning.RawStatement.Trim());
        }

        if (!string.IsNullOrWhiteSpace(meaning?.NormalizedMeaning))
        {
            meanings.Add(meaning.NormalizedMeaning.Trim());
        }

        if (isSrp)
        {
            meanings.Add("strange rare peculiar");
        }

        return ConceptSemanticDocumentParser.Compose(
            clinicalConcept: clinical,
            homeopathicConcept: homeopathic,
            synonyms: synonyms.Take(12).ToList(),
            meanings: meanings.Take(6).ToList(),
            linkedRubricIds: null);
    }

    public static string BuildFromGraphConcept(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        PatientMeaningNodeModel? meaning) =>
        BuildQueryText(
            clinical?.ConceptName ?? homeo.ConceptName,
            homeo.ConceptName,
            meaning,
            homeo.IsSRP,
            null);
}
