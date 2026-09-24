using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 11: every rubric must have an evidence span; otherwise reject.</summary>
public sealed class EvidenceVerifier : IEciEvidenceVerifier
{
    private readonly ILogger<EvidenceVerifier> _logger;

    public EvidenceVerifier(ILogger<EvidenceVerifier> logger)
    {
        _logger = logger;
    }

    public List<EciCandidateRubric> VerifyAndReject(
        EciValidatedSymptom symptom,
        IReadOnlyList<EciCandidateRubric> rankedCandidates)
    {
        var evidence = symptom.Symptom.Evidence?.Trim();
        var list = new List<EciCandidateRubric>(rankedCandidates.Count);

        foreach (var c in rankedCandidates)
        {
            if (string.IsNullOrWhiteSpace(evidence))
            {
                c.Rejected = true;
                c.RejectReasons.Add("Missing transcript evidence span for symptom.");
            }

            list.Add(c);
        }

        return list;
    }
}

