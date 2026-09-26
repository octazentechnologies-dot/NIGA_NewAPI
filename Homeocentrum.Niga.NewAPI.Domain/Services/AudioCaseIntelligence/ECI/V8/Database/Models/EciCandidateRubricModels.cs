namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

public static class EciCandidateSources
{
    public const string ExactSql = "ExactSql";
    public const string Synonym = "Synonym";
    public const string Ontology = "Ontology";
    public const string FullText = "FullText";
    public const string Embedding = "Embedding";
    public const string Hierarchy = "Hierarchy";
    public const string Bootstrap = "Bootstrap";
    public const string Historical = "Historical";
}

public sealed class EciCandidateProvenance
{
    public string Source { get; set; } = string.Empty;

    public string MatchPath { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public decimal? EmbeddingScore { get; set; }
}

public sealed class EciCandidateRubric
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public List<EciCandidateProvenance> Provenance { get; set; } = new();

    public decimal ClinicalMatchScore { get; set; }

    public decimal EvidenceScore { get; set; }

    public decimal OntologyScore { get; set; }

    public decimal EmbeddingScore { get; set; }

    public decimal HierarchyScore { get; set; }

    public decimal SqlExactScore { get; set; }

    public decimal HistoricalScore { get; set; }

    public decimal ExpertRulesScore { get; set; }

    public decimal PenaltyScore { get; set; }

    public decimal FinalScore { get; set; }

    public bool Rejected { get; set; }

    public List<string> RejectReasons { get; set; } = new();
}

