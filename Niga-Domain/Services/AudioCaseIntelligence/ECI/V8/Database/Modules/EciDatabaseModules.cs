using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

public interface IEciExactSqlSearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(EciValidatedSymptom symptom, CancellationToken cancellationToken = default);
}

public interface IEciSynonymSearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> curatedSynonyms,
        CancellationToken cancellationToken = default);
}

public interface IEciOntologySearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> ontologyTerms,
        CancellationToken cancellationToken = default);
}

public interface IEciFullTextSearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken = default);
}

public interface IEciEmbeddingSearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken = default);
}

public interface IEciHierarchySearchModule
{
    Task<List<EciCandidateRubric>> ExpandAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<EciCandidateRubric> seedCandidates,
        CancellationToken cancellationToken = default);
}

public interface IEciBootstrapMappingSearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        CancellationToken cancellationToken = default);
}

public interface IEciHistoricalMappingSearchModule
{
    Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        CancellationToken cancellationToken = default);
}

public interface IEciCandidateMerger
{
    List<EciCandidateRubric> Merge(params IEnumerable<EciCandidateRubric>[] lists);

    Task ResolveNamesAsync(List<EciCandidateRubric> candidates, CancellationToken cancellationToken = default);
}

public interface IEciRubricRanker
{
    List<EciCandidateRubric> Rank(
        EciValidatedSymptom symptom,
        IReadOnlyList<EciCandidateRubric> candidates,
        string cleanTranscript);
}

public interface IEciEvidenceVerifier
{
    List<EciCandidateRubric> VerifyAndReject(
        EciValidatedSymptom symptom,
        IReadOnlyList<EciCandidateRubric> rankedCandidates);
}

public interface IEciExplainabilityGenerator
{
    void ApplyExplainability(
        AudioCaseSuggestedRubricModel rubric,
        EciValidatedSymptom symptom,
        EciCandidateRubric candidate);
}

