using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.KnowledgeGraph;

public class KgFeedbackGraphWriter : IKgFeedbackGraphWriter
{
    private readonly IEnterpriseKnowledgeGraphRepository _repository;
    private readonly RubricIntelligenceOptions _options;

    public KgFeedbackGraphWriter(
        IEnterpriseKnowledgeGraphRepository repository,
        IOptions<RubricIntelligenceOptions> options)
    {
        _repository = repository;
        _options = options.Value;
    }

    public async Task ApplyFeedbackAsync(
        Guid sessionId,
        AudioCaseRubricFeedbackRequestModel request,
        long feedbackId,
        int doctorUserId,
        DoctorLearningFeedbackContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableEnterpriseKnowledgeGraph) return;

        var conceptName = context.HomeopathicConceptName ?? context.ClinicalConceptName ?? request.SourceConceptName;
        if (string.IsNullOrWhiteSpace(conceptName) || !request.SubSectionId.HasValue)
            return;

        var homeoNode = await _repository.FindOrCreateNodeAsync(
            KgNodeTypes.HomeopathicConcept,
            conceptName,
            conceptName,
            "en",
            confidence: 0.8m,
            cancellationToken: cancellationToken);

        var rubricNode = await _repository.FindOrCreateNodeAsync(
            KgNodeTypes.RepertoryRubric,
            $"subsection:{request.SubSectionId.Value}",
            request.RubricName,
            subSectionId: request.SubSectionId,
            cancellationToken: cancellationToken);

        var edge = await _repository.UpsertEdgeAsync(
            homeoNode.NodeId,
            rubricNode.NodeId,
            KgEdgeTypes.SuggestsRubric,
            1m,
            0.7m,
            isProvisional: true,
            source: "DoctorFeedback",
            sourceSessionId: sessionId,
            cancellationToken: cancellationToken);

        switch (request.FeedbackType.ToUpperInvariant())
        {
            case "ACCEPTED":
            case "APPROVED":
                await _repository.AdjustEdgeWeightAsync(
                    edge.EdgeId, _options.DoctorLearningAcceptBoost, confirmProvisional: true, cancellationToken);
                await _repository.RecordFeedbackMutationAsync(
                    sessionId, feedbackId, "Accepted", "StrengthenEdge", edge.EdgeId, homeoNode.NodeId,
                    request.SubSectionId, null, _options.DoctorLearningAcceptBoost, doctorUserId,
                    cancellationToken: cancellationToken);
                break;

            case "REJECTED":
                await _repository.AdjustEdgeWeightAsync(
                    edge.EdgeId, _options.DoctorLearningRejectPenalty, cancellationToken: cancellationToken);
                await _repository.RecordFeedbackMutationAsync(
                    sessionId, feedbackId, "Rejected", "WeakenEdge", edge.EdgeId, homeoNode.NodeId,
                    request.SubSectionId, null, _options.DoctorLearningRejectPenalty, doctorUserId,
                    cancellationToken: cancellationToken);
                break;

            case "CORRECTED":
            case "EDITED":
                await _repository.AdjustEdgeWeightAsync(
                    edge.EdgeId, _options.DoctorLearningRejectPenalty, cancellationToken: cancellationToken);
                if (request.CorrectedSubSectionId.HasValue)
                {
                    var correctedNode = await _repository.FindOrCreateNodeAsync(
                        KgNodeTypes.RepertoryRubric,
                        $"subsection:{request.CorrectedSubSectionId.Value}",
                        $"Rubric {request.CorrectedSubSectionId.Value}",
                        subSectionId: request.CorrectedSubSectionId,
                        cancellationToken: cancellationToken);

                    var correctedEdge = await _repository.UpsertEdgeAsync(
                        homeoNode.NodeId,
                        correctedNode.NodeId,
                        KgEdgeTypes.SuggestsRubric,
                        1.2m,
                        0.85m,
                        isProvisional: false,
                        source: "DoctorCorrection",
                        sourceSessionId: sessionId,
                        cancellationToken: cancellationToken);

                    await _repository.RecordFeedbackMutationAsync(
                        sessionId, feedbackId, "Corrected", "RedirectEdge", correctedEdge.EdgeId, homeoNode.NodeId,
                        request.SubSectionId, request.CorrectedSubSectionId,
                        _options.DoctorLearningCorrectionBoost, doctorUserId,
                        JsonSerializer.Serialize(new { request.RubricName }), cancellationToken);
                }
                break;
        }
    }
}

public class KgBootstrapImporter : IKgBootstrapImporter
{
    private readonly NIGACentrumContext _context;
    private readonly IEnterpriseKnowledgeGraphRepository _repository;

    public KgBootstrapImporter(NIGACentrumContext context, IEnterpriseKnowledgeGraphRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    public async Task<KgBootstrapImportResult> ImportAsync(CancellationToken cancellationToken = default)
    {
        var result = new KgBootstrapImportResult();
        try
        {
            var mappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var mapping in mappings)
            {
                var homeo = await _repository.FindOrCreateNodeAsync(
                    KgNodeTypes.HomeopathicConcept,
                    mapping.HomeopathicConceptPattern,
                    mapping.HomeopathicConceptPattern,
                    "en",
                    confidence: 0.75m,
                    cancellationToken: cancellationToken);

                var medical = await _repository.FindOrCreateNodeAsync(
                    KgNodeTypes.MedicalConcept,
                    mapping.HomeopathicConceptPattern,
                    mapping.HomeopathicConceptPattern,
                    "en",
                    confidence: 0.7m,
                    cancellationToken: cancellationToken);

                await _repository.UpsertEdgeAsync(
                    medical.NodeId, homeo.NodeId, KgEdgeTypes.MapsHomeopathic,
                    1m, 0.75m, false, "Bootstrap", cancellationToken: cancellationToken);
                result.EdgesCreated++;
                result.NodesCreated += 2;
            }

            var learnings = await _context.AiCaseLearnings.AsNoTracking()
                .Where(x => x.ToRubricSubSectionId.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var learning in learnings.GroupBy(x => new { x.FromConcept, x.ToRubricSubSectionId }))
            {
                var weight = learning.Sum(x => x.WeightDelta);
                var homeo = await _repository.FindOrCreateNodeAsync(
                    KgNodeTypes.HomeopathicConcept,
                    learning.Key.FromConcept,
                    learning.Key.FromConcept,
                    "en",
                    cancellationToken: cancellationToken);

                var rubric = await _repository.FindOrCreateNodeAsync(
                    KgNodeTypes.RepertoryRubric,
                    $"subsection:{learning.Key.ToRubricSubSectionId!.Value}",
                    $"Rubric {learning.Key.ToRubricSubSectionId.Value}",
                    subSectionId: learning.Key.ToRubricSubSectionId,
                    cancellationToken: cancellationToken);

                await _repository.UpsertEdgeAsync(
                    homeo.NodeId, rubric.NodeId, KgEdgeTypes.SuggestsRubric,
                    Math.Max(0.5m, 1m + weight), Math.Clamp(0.5m + weight, 0.1m, 1m), weight < 0,
                    "LearningImport", cancellationToken: cancellationToken);
                result.LearningRecordsImported++;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }
}

public class KgRemedyProjectionSync : IKgRemedyProjectionSync
{
    private readonly NIGACentrumContext _context;

    public KgRemedyProjectionSync(NIGACentrumContext context) => _context = context;

    public async Task<KgRemedySyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        await _context.AiKgRemedyProjections.ExecuteDeleteAsync(cancellationToken);

        var rows = await (
            from rr in _context.RubricRemedyDetails.AsNoTracking()
            join remedy in _context.RemedyMasters.AsNoTracking() on rr.RemedyId equals remedy.RemedyId
            join grade in _context.RemedyGradeMaster.AsNoTracking() on rr.GradeId equals grade.GradeId into grades
            from grade in grades.DefaultIfEmpty()
            where rr.DeletedStatus != true && rr.SubSectionId.HasValue && rr.RemedyId.HasValue
            select new
            {
                SubSectionId = rr.SubSectionId!.Value,
                RemedyId = rr.RemedyId!.Value,
                rr.GradeId,
                RemedyName = remedy.RemedyName,
                GradeValue = grade != null ? grade.GradeNo : (int?)null,
            }).ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            _context.AiKgRemedyProjections.Add(new Master.AiKgRemedyProjection
            {
                SubSectionId = row.SubSectionId,
                RemedyId = row.RemedyId,
                GradeId = row.GradeId,
                RemedyName = row.RemedyName,
                GradeValue = row.GradeValue,
                LastSyncedUtc = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new KgRemedySyncResult
        {
            ProjectionsSynced = rows.Count,
            RubricsCovered = rows.Select(x => x.SubSectionId).Distinct().Count(),
        };
    }

    public async Task<List<KgRemedyProjectionModel>> GetRemediesForRubricAsync(
        int subSectionId,
        CancellationToken cancellationToken = default) =>
        await _context.AiKgRemedyProjections.AsNoTracking()
            .Where(x => x.SubSectionId == subSectionId)
            .OrderByDescending(x => x.GradeValue)
            .Select(x => new KgRemedyProjectionModel
            {
                SubSectionId = x.SubSectionId,
                RemedyId = x.RemedyId,
                RemedyName = x.RemedyName,
                GradeValue = x.GradeValue,
                LastSyncedUtc = x.LastSyncedUtc,
            })
            .ToListAsync(cancellationToken);
}
