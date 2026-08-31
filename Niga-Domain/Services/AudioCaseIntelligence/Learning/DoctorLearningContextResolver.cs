using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Learning;

public class DoctorLearningContextResolver
{
    private readonly NIGACentrumContext _context;

    public DoctorLearningContextResolver(NIGACentrumContext context) => _context = context;

    public async Task<DoctorLearningFeedbackContext> ResolveAsync(
        Guid sessionId,
        AudioCaseRubricFeedbackRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var context = new DoctorLearningFeedbackContext
        {
            HomeopathicConceptName = request.SourceConceptName?.Trim(),
            ClinicalConceptName = request.ClinicalConceptName?.Trim(),
            SubSectionId = request.SubSectionId,
            CorrectedSubSectionId = request.CorrectedSubSectionId,
        };

        if (!string.IsNullOrWhiteSpace(context.ResolvedConceptName))
            return context;

        if (!request.SubSectionId.HasValue)
            return context;

        var discovery = await _context.AiRubricDiscoveries.AsNoTracking()
            .Where(d => d.AudioCaseSessionId == sessionId && d.SubSectionId == request.SubSectionId.Value)
            .OrderByDescending(d => d.Confidence)
            .ThenByDescending(d => d.EnteredDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (discovery == null)
            return context;

        var homeo = await _context.AiHomeopathicConcepts.AsNoTracking()
            .FirstOrDefaultAsync(h => h.HomeopathicConceptId == discovery.HomeopathicConceptId, cancellationToken);

        if (homeo == null)
            return context;

        context.HomeopathicConceptName = homeo.ConceptName;

        var clinical = await _context.AiClinicalConceptsV3.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClinicalConceptId == homeo.ClinicalConceptId, cancellationToken);

        context.ClinicalConceptName = clinical?.ConceptName;
        return context;
    }
}
