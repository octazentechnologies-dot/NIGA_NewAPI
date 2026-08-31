namespace Niga_Domain.DTOs;

public class RubricCandidateModel
{
    public int Rank { get; set; }

    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public long? HomeopathicConceptId { get; set; }

    public int ClinicalConceptIndex { get; set; } = -1;

    public string SourceConceptName { get; set; } = string.Empty;

    public string? SourceCategory { get; set; }

    public string RubricTier { get; set; } = "Tier3";

    public decimal SimilarityScore { get; set; }

    public decimal ClinicalRelevanceScore { get; set; }

    public decimal EvidenceScore { get; set; }

    public decimal DoctorAcceptanceScore { get; set; }

    public decimal CompositeScore { get; set; }

    public string MatchMethod { get; set; } = string.Empty;

    public string? MatchReason { get; set; }

    public string? MappedFromConceptKey { get; set; }

    public RubricEvidenceChainV3Model? EvidenceChain { get; set; }

    public EnterpriseSemanticRubricValidationModel? Validation { get; set; }
}

public class RubricCandidateEngineResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string VersionCode { get; set; } = string.Empty;

    public List<RubricCandidateModel> Candidates { get; set; } = new();

    public List<RubricCandidateModel> Tier1Rubrics { get; set; } = new();

    public List<RubricCandidateModel> Tier2Rubrics { get; set; } = new();

    public List<RubricCandidateModel> Tier3Rubrics { get; set; } = new();

    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();
}

public class RubricCandidateEngineRequest
{
    public Guid? EmbeddingVersionId { get; set; }

    public int? MaxCandidatesPerConcept { get; set; }

    public int? MaxTotalCandidates { get; set; }

    public bool IncludeValidation { get; set; } = true;
}

public class RubricCandidateGraphRequest
{
    public Guid? SessionId { get; set; }

    public ConceptGraphFullModel? Graph { get; set; }

    public Guid? EmbeddingVersionId { get; set; }

    public int? MaxCandidatesPerConcept { get; set; }

    public int? MaxTotalCandidates { get; set; }

    public bool IncludeValidation { get; set; } = true;
}
