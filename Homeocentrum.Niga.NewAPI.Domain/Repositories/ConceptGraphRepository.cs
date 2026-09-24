using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

public class ConceptGraphRepository : IConceptGraphRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly NIGACentrumContext _context;

    public ConceptGraphRepository(NIGACentrumContext context) => _context = context;

    public async Task ClearSessionV3DataAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _context.AiMissingSymptomCandidates.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiCaseCoverageMetrics.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);

        var clusterIds = await _context.AiConceptClusters.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .Select(x => x.ConceptClusterId)
            .ToListAsync(cancellationToken);
        if (clusterIds.Count > 0)
        {
            await _context.AiConceptClusterMembers
                .Where(x => clusterIds.Contains(x.ConceptClusterId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _context.AiConceptClusters.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiSymptomBlocks.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricConfidences.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricValidationsV3.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricEvidences.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricDiscoveries.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiConceptGraphEdges.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiHomeopathicConcepts.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiClinicalConceptsV3.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiMetaphorResolutions.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiPatientMeanings.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ClearSessionGraphAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await ClearSessionV3DataAsync(sessionId, cancellationToken);
        await _context.AiReasoningAudits.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task SavePatientMeaningsAsync(
        Guid sessionId,
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default)
    {
        await _context.AiPatientMeanings.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);

        foreach (var meaning in meanings)
        {
            _context.AiPatientMeanings.Add(new AiPatientMeaning
            {
                AudioCaseSessionId = sessionId,
                RawStatement = meaning.RawStatement,
                NormalizedMeaning = meaning.NormalizedMeaning,
                LanguageCode = meaning.LanguageCode,
                Confidence = meaning.Confidence,
                SequenceOrder = meaning.SequenceOrder,
                ModelVersion = meaning.ModelVersion,
                EnteredDate = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PatientMeaningNodeModel>> GetPatientMeaningsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        await _context.AiPatientMeanings.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .OrderBy(x => x.SequenceOrder)
            .Select(x => new PatientMeaningNodeModel
            {
                PatientMeaningId = x.PatientMeaningId,
                RawStatement = x.RawStatement,
                NormalizedMeaning = x.NormalizedMeaning,
                LanguageCode = x.LanguageCode,
                Confidence = x.Confidence,
                SequenceOrder = x.SequenceOrder,
                ModelVersion = x.ModelVersion,
            }).ToListAsync(cancellationToken);

    public async Task SaveFullGraphAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        CancellationToken cancellationToken = default)
    {
        await ClearSessionV3DataAsync(sessionId, cancellationToken);

        var blockRows = new List<AiSymptomBlock>(graph.SymptomBlocks.Count);
        foreach (var block in graph.SymptomBlocks)
        {
            var row = new AiSymptomBlock
            {
                AudioCaseSessionId = sessionId,
                BlockOrder = block.BlockOrder,
                TranscriptSpan = block.TranscriptSpan,
                CategoryHint = block.CategoryHint,
                BlockType = block.BlockType,
                Confidence = block.Confidence,
                IsCovered = block.IsCovered,
                ModelVersion = block.ModelVersion,
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiSymptomBlocks.Add(row);
            blockRows.Add(row);
        }

        if (blockRows.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < graph.SymptomBlocks.Count; i++)
            graph.SymptomBlocks[i].SymptomBlockId = blockRows[i].SymptomBlockId;

        var meaningIdMap = new Dictionary<int, long>();
        var meaningRows = new List<AiPatientMeaning>(graph.Meanings.Count);
        for (var i = 0; i < graph.Meanings.Count; i++)
        {
            var m = graph.Meanings[i];
            var row = new AiPatientMeaning
            {
                AudioCaseSessionId = sessionId,
                RawStatement = m.RawStatement,
                NormalizedMeaning = m.NormalizedMeaning,
                LanguageCode = m.LanguageCode,
                Confidence = m.Confidence,
                SequenceOrder = m.SequenceOrder,
                ModelVersion = m.ModelVersion,
                ParentMeaningId = m.ParentMeaningId,
                SymptomBlockId = m.SymptomBlockId,
                SymptomCategory = m.SymptomCategory,
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiPatientMeanings.Add(row);
            meaningRows.Add(row);
        }

        if (meaningRows.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < graph.Meanings.Count; i++)
        {
            meaningIdMap[i] = meaningRows[i].PatientMeaningId;
            graph.Meanings[i].PatientMeaningId = meaningRows[i].PatientMeaningId;
        }

        for (var i = 0; i < graph.Metaphors.Count; i++)
        {
            var meta = graph.Metaphors[i];
            var meaningId = meta.PatientMeaningId ?? meaningIdMap.GetValueOrDefault(i);
            if (meaningId <= 0) continue;

            _context.AiMetaphorResolutions.Add(new AiMetaphorResolution
            {
                PatientMeaningId = meaningId,
                AudioCaseSessionId = sessionId,
                Expression = meta.Expression,
                LiteralMeaning = meta.LiteralMeaning,
                ClinicalMeaning = meta.ClinicalMeaning,
                Confidence = meta.Confidence,
                ModelVersion = meta.ModelVersion,
                IsMetaphor = meta.IsMetaphor,
                GroundedInOntology = meta.GroundedInOntology,
                OntologyId = meta.OntologyId,
                EnteredDate = DateTime.UtcNow,
            });
        }

        var clinicalIdMap = new Dictionary<int, long>();
        var clinicalRows = new List<AiClinicalConceptV3>(graph.ClinicalConcepts.Count);
        for (var i = 0; i < graph.ClinicalConcepts.Count; i++)
        {
            var c = graph.ClinicalConcepts[i];
            var meaningId = c.PatientMeaningId ?? meaningIdMap.GetValueOrDefault(i);
            var row = new AiClinicalConceptV3
            {
                AudioCaseSessionId = sessionId,
                PatientMeaningId = meaningId > 0 ? meaningId : null,
                ConceptName = c.ConceptName,
                Domain = c.Domain,
                Confidence = c.Confidence,
                SymptomCategory = c.SymptomCategory,
                ModelVersion = c.ModelVersion,
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiClinicalConceptsV3.Add(row);
            clinicalRows.Add(row);
        }

        if (graph.Metaphors.Count > 0 || clinicalRows.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < graph.ClinicalConcepts.Count; i++)
        {
            clinicalIdMap[i] = clinicalRows[i].ClinicalConceptId;
            graph.ClinicalConcepts[i].ClinicalConceptId = clinicalRows[i].ClinicalConceptId;
        }

        var homeoIdMap = new Dictionary<int, long>();
        var homeoEntries = new List<(int GraphIndex, AiHomeopathicConcept Row)>();
        for (var i = 0; i < graph.HomeopathicConcepts.Count; i++)
        {
            var h = graph.HomeopathicConcepts[i];
            var clinicalIndex = h.ClinicalConceptIndex >= 0 ? h.ClinicalConceptIndex : i;
            var clinicalId = h.ClinicalConceptId ?? clinicalIdMap.GetValueOrDefault(clinicalIndex);
            if (clinicalId <= 0) continue;

            var row = new AiHomeopathicConcept
            {
                AudioCaseSessionId = sessionId,
                ClinicalConceptId = clinicalId,
                ConceptName = h.ConceptName,
                Importance = h.Importance,
                SymptomClass = h.SymptomClass,
                IsSRP = h.IsSRP,
                Weight = h.Weight,
                Confidence = h.Confidence,
                ModelVersion = h.ModelVersion,
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiHomeopathicConcepts.Add(row);
            homeoEntries.Add((i, row));
        }

        if (homeoEntries.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        foreach (var (graphIndex, row) in homeoEntries)
        {
            homeoIdMap[graphIndex] = row.HomeopathicConceptId;
            graph.HomeopathicConcepts[graphIndex].HomeopathicConceptId = row.HomeopathicConceptId;
        }

        var rank = 1;
        var discoveryRows = new List<(RubricDiscoveryNodeModel Source, AiRubricDiscovery Row)>();
        foreach (var d in graph.Discoveries)
        {
            var homeoId = d.HomeopathicConceptId ?? homeoIdMap.Values.FirstOrDefault();
            if (homeoId <= 0) continue;

            var discovery = new AiRubricDiscovery
            {
                AudioCaseSessionId = sessionId,
                HomeopathicConceptId = homeoId,
                SubSectionId = d.SubSectionId,
                SubSectionName = d.SubSectionName,
                MatchReason = d.MatchReason,
                DiscoveryMethod = d.DiscoveryMethod,
                Confidence = d.Confidence,
                RubricTier = d.RubricTier ?? ConceptGraphTierHelper.ResolveTier(d.Confidence),
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiRubricDiscoveries.Add(discovery);
            discoveryRows.Add((d, discovery));
        }

        if (discoveryRows.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        foreach (var (source, discovery) in discoveryRows)
        {
            source.RubricDiscoveryId = discovery.RubricDiscoveryId;

            if (source.EvidenceChain != null)
            {
                var chainJson = source.EnterpriseEvidenceChain != null
                    ? JsonSerializer.Serialize(source.EnterpriseEvidenceChain, JsonOptions)
                    : JsonSerializer.Serialize(source.EvidenceChain, JsonOptions);

                _context.AiRubricEvidences.Add(new AiRubricEvidence
                {
                    RubricDiscoveryId = discovery.RubricDiscoveryId,
                    AudioCaseSessionId = sessionId,
                    EvidenceChainJson = chainJson,
                    IsComplete = source.EnterpriseEvidenceChain?.IsComplete ?? source.EvidenceChain.IsComplete,
                    CoverageScore = source.EnterpriseEvidenceChain?.Steps
                        .FirstOrDefault(s => s.StepKey == RubricEvidenceChainStepKeys.Confidence)?.Score
                        ?? source.EvidenceChain.Confidence,
                    EnteredDate = DateTime.UtcNow,
                });
            }

            if (!string.IsNullOrWhiteSpace(source.ValidationStatus))
            {
                _context.AiRubricValidationsV3.Add(new AiRubricValidationV3
                {
                    RubricDiscoveryId = discovery.RubricDiscoveryId,
                    AudioCaseSessionId = sessionId,
                    ValidationStatus = source.ValidationStatus,
                    QualityScore = source.QualityScore,
                    ValidationFlagsJson = source.ValidationFlagsJson,
                    ValidatedAt = DateTime.UtcNow,
                });
            }

            _context.AiRubricConfidences.Add(new AiRubricConfidence
            {
                RubricDiscoveryId = discovery.RubricDiscoveryId,
                AudioCaseSessionId = sessionId,
                FinalScore = source.Confidence,
                RankOrder = rank++,
                Tier = source.RubricTier ?? ConceptGraphTierHelper.ResolveRubricTierLabel(source.Confidence),
                EnteredDate = DateTime.UtcNow,
            });
        }

        if (discoveryRows.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        foreach (var edge in graph.ConceptGraphEdges)
        {
            var fromId = ResolveEdgeNodeId(edge.FromNodeType, edge.FromNodeKey, meaningIdMap, clinicalIdMap, homeoIdMap);
            var toId = ResolveEdgeNodeId(edge.ToNodeType, edge.ToNodeKey, meaningIdMap, clinicalIdMap, homeoIdMap);
            if (fromId <= 0 && toId <= 0) continue;

            _context.AiConceptGraphEdges.Add(new AiConceptGraphEdge
            {
                AudioCaseSessionId = sessionId,
                FromNodeType = edge.FromNodeType,
                FromNodeId = fromId,
                ToNodeType = edge.ToNodeType,
                ToNodeId = toId,
                EdgeType = edge.EdgeType,
                Weight = edge.Weight,
                Confidence = edge.Confidence,
                EnteredDate = DateTime.UtcNow,
            });
        }

        if (graph.ConceptGraphEdges.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);
    }

    private static long ResolveEdgeNodeId(
        string nodeType,
        string nodeKey,
        Dictionary<int, long> meaningIdMap,
        Dictionary<int, long> clinicalIdMap,
        Dictionary<int, long> homeoIdMap)
    {
        if (string.IsNullOrWhiteSpace(nodeKey)) return 0;
        var parts = nodeKey.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var index)) return 0;

        return nodeType switch
        {
            "Meaning" => meaningIdMap.GetValueOrDefault(index),
            "Clinical" => clinicalIdMap.GetValueOrDefault(index),
            "Homeopathic" => homeoIdMap.GetValueOrDefault(index),
            _ => 0,
        };
    }

    public async Task<ConceptGraphFullModel> GetFullGraphAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var graph = new ConceptGraphFullModel { SessionId = sessionId };
        graph.Meanings = await GetPatientMeaningsAsync(sessionId, cancellationToken);

        graph.ClinicalConcepts = await _context.AiClinicalConceptsV3.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .Select(x => new ClinicalConceptNodeModel
            {
                ClinicalConceptId = x.ClinicalConceptId,
                PatientMeaningId = x.PatientMeaningId,
                ConceptName = x.ConceptName,
                Domain = x.Domain,
                Confidence = x.Confidence,
                ModelVersion = x.ModelVersion,
            }).ToListAsync(cancellationToken);

        graph.HomeopathicConcepts = await _context.AiHomeopathicConcepts.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .Select(x => new HomeopathicConceptNodeModel
            {
                HomeopathicConceptId = x.HomeopathicConceptId,
                ClinicalConceptId = x.ClinicalConceptId,
                ConceptName = x.ConceptName,
                Importance = x.Importance,
                SymptomClass = x.SymptomClass,
                IsSRP = x.IsSRP,
                Weight = x.Weight,
                Confidence = x.Confidence,
                ModelVersion = x.ModelVersion,
            }).ToListAsync(cancellationToken);

        graph.Discoveries = await _context.AiRubricDiscoveries.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .OrderByDescending(x => x.Confidence)
            .Select(x => new RubricDiscoveryNodeModel
            {
                RubricDiscoveryId = x.RubricDiscoveryId,
                HomeopathicConceptId = x.HomeopathicConceptId,
                SubSectionId = x.SubSectionId,
                SubSectionName = x.SubSectionName,
                MatchReason = x.MatchReason,
                DiscoveryMethod = x.DiscoveryMethod,
                Confidence = x.Confidence,
            }).ToListAsync(cancellationToken);

        var evidenceRows = await _context.AiRubricEvidences.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .ToListAsync(cancellationToken);

        foreach (var evidence in evidenceRows)
        {
            if (string.IsNullOrWhiteSpace(evidence.EvidenceChainJson)) continue;

            try
            {
                var enterprise = JsonSerializer.Deserialize<RubricEnterpriseEvidenceChainModel>(
                    evidence.EvidenceChainJson, JsonOptions);
                if (enterprise != null && enterprise.Steps.Count > 0)
                {
                    graph.EnterpriseEvidenceChains.Add(enterprise);
                    continue;
                }
            }
            catch (JsonException)
            {
                // Fall back to legacy v3 chain format below.
            }

            var legacy = JsonSerializer.Deserialize<RubricEvidenceChainV3Model>(evidence.EvidenceChainJson, JsonOptions);
            if (legacy != null)
            {
                var discovery = graph.Discoveries.FirstOrDefault(d => d.RubricDiscoveryId == evidence.RubricDiscoveryId);
                if (discovery != null)
                    discovery.EvidenceChain = legacy;
            }
        }

        return graph;
    }

    public async Task SaveDisplayedRubricsAsync(
        Guid sessionId,
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        ConceptGraphFullModel? graph = null,
        CancellationToken cancellationToken = default)
    {
        var repertoryRubrics = rubrics
            .Where(r => r.SubSectionId > 0 && !string.IsNullOrWhiteSpace(r.SubSectionName))
            .ToList();
        if (repertoryRubrics.Count == 0)
            return;

        var homeoByName = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var h in graph?.HomeopathicConcepts ?? Enumerable.Empty<HomeopathicConceptNodeModel>())
        {
            if (string.IsNullOrWhiteSpace(h.ConceptName) || h.HomeopathicConceptId is not > 0)
                continue;
            homeoByName[h.ConceptName.Trim()] = h.HomeopathicConceptId!.Value;
        }

        var dbHomeos = await _context.AiHomeopathicConcepts.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .Select(x => new { x.HomeopathicConceptId, x.ConceptName })
            .ToListAsync(cancellationToken);
        foreach (var h in dbHomeos)
        {
            if (string.IsNullOrWhiteSpace(h.ConceptName))
                continue;
            homeoByName.TryAdd(h.ConceptName.Trim(), h.HomeopathicConceptId);
        }

        if (graph != null && graph.HomeopathicConcepts.Count > 0 && homeoByName.Count == 0)
        {
            await EnsureHomeopathicConceptsPersistedAsync(sessionId, graph, homeoByName, cancellationToken);
        }

        var defaultHomeoId = homeoByName.Values.FirstOrDefault();

        await _context.AiRubricEvidences.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricConfidences.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricValidationsV3.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiRubricDiscoveries.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);

        // Gap 3: batch insert discoveries once (was SaveChanges per rubric → multi-minute finalization).
        var discoveryRows = new List<(AiRubricDiscovery Row, AudioCaseSuggestedRubricModel Rubric)>();
        var rank = 1;
        foreach (var rubric in repertoryRubrics)
        {
            var homeoName = rubric.MatchedFromDetail?.HomeopathicConcept
                ?? rubric.Explainability?.HomeopathicMeaning;
            long homeoId = 0;
            if (!string.IsNullOrWhiteSpace(homeoName))
                homeoByName.TryGetValue(homeoName.Trim(), out homeoId);
            if (homeoId <= 0)
                homeoId = defaultHomeoId;
            if (homeoId <= 0)
                continue;

            var confidence = rubric.ConfidenceScore ?? rubric.MatchScore;
            var row = new AiRubricDiscovery
            {
                AudioCaseSessionId = sessionId,
                HomeopathicConceptId = homeoId,
                SubSectionId = rubric.SubSectionId,
                SubSectionName = rubric.SubSectionName,
                MatchReason = rubric.WhySuggested ?? rubric.SelectionReason ?? rubric.MatchSource,
                DiscoveryMethod = rubric.MatchLayer ?? rubric.MatchSource ?? "ConceptGraph",
                Confidence = confidence,
                RubricTier = rubric.RubricTier ?? ConceptGraphTierHelper.ResolveTier(confidence),
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiRubricDiscoveries.Add(row);
            discoveryRows.Add((row, rubric));
            _ = rank; // rank assigned after insert for confidence rows
        }

        if (discoveryRows.Count == 0)
            return;

        await _context.SaveChangesAsync(cancellationToken);

        rank = 1;
        foreach (var (row, rubric) in discoveryRows)
        {
            var confidence = rubric.ConfidenceScore ?? rubric.MatchScore;
            var detail = rubric.MatchedFromDetail;
            var chain = new RubricEvidenceChainV3Model
            {
                TranscriptStatement = detail?.PatientStatement ?? rubric.MatchedFrom,
                PatientMeaning = detail?.NormalizedMeaning ?? rubric.Explainability?.ClinicalMeaning,
                ClinicalConcept = detail?.ClinicalConcept ?? rubric.Explainability?.ClinicalMeaning,
                HomeopathicConcept = detail?.HomeopathicConcept ?? rubric.Explainability?.HomeopathicMeaning,
                RubricName = rubric.SubSectionName,
                Confidence = confidence,
                IsComplete = rubric.EvidenceChainComplete == true,
            };

            if (rubric.EnterpriseEvidenceChain != null)
            {
                _context.AiRubricEvidences.Add(new AiRubricEvidence
                {
                    RubricDiscoveryId = row.RubricDiscoveryId,
                    AudioCaseSessionId = sessionId,
                    EvidenceChainJson = JsonSerializer.Serialize(rubric.EnterpriseEvidenceChain, JsonOptions),
                    IsComplete = rubric.EnterpriseEvidenceChain.IsComplete,
                    CoverageScore = confidence,
                    EnteredDate = DateTime.UtcNow,
                });
            }
            else if (rubric.EvidenceChain != null || detail != null)
            {
                _context.AiRubricEvidences.Add(new AiRubricEvidence
                {
                    RubricDiscoveryId = row.RubricDiscoveryId,
                    AudioCaseSessionId = sessionId,
                    EvidenceChainJson = JsonSerializer.Serialize(chain, JsonOptions),
                    IsComplete = chain.IsComplete,
                    CoverageScore = confidence,
                    EnteredDate = DateTime.UtcNow,
                });
            }

            if (!string.IsNullOrWhiteSpace(rubric.ValidationStatus))
            {
                _context.AiRubricValidationsV3.Add(new AiRubricValidationV3
                {
                    RubricDiscoveryId = row.RubricDiscoveryId,
                    AudioCaseSessionId = sessionId,
                    ValidationStatus = rubric.ValidationStatus,
                    QualityScore = rubric.QualityScore,
                    ValidationFlagsJson = rubric.ValidationFlags?.Count > 0
                        ? JsonSerializer.Serialize(rubric.ValidationFlags, JsonOptions)
                        : null,
                    ValidatedAt = DateTime.UtcNow,
                });
            }

            _context.AiRubricConfidences.Add(new AiRubricConfidence
            {
                RubricDiscoveryId = row.RubricDiscoveryId,
                AudioCaseSessionId = sessionId,
                FinalScore = confidence,
                RankOrder = rank++,
                Tier = rubric.RubricTier ?? ConceptGraphTierHelper.ResolveRubricTierLabel(confidence),
                EnteredDate = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureHomeopathicConceptsPersistedAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        Dictionary<string, long> homeoByName,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < graph.HomeopathicConcepts.Count; i++)
        {
            var h = graph.HomeopathicConcepts[i];
            if (string.IsNullOrWhiteSpace(h.ConceptName))
                continue;

            long? clinicalId = null;
            if (h.ClinicalConceptIndex >= 0 && h.ClinicalConceptIndex < graph.ClinicalConcepts.Count)
            {
                var clinical = graph.ClinicalConcepts[h.ClinicalConceptIndex];
                clinicalId = clinical.ClinicalConceptId > 0 ? clinical.ClinicalConceptId : null;
            }

            var row = new AiHomeopathicConcept
            {
                AudioCaseSessionId = sessionId,
                ClinicalConceptId = clinicalId ?? 0,
                ConceptName = h.ConceptName,
                Importance = h.Importance,
                SymptomClass = h.SymptomClass,
                IsSRP = h.IsSRP,
                Weight = h.Weight,
                Confidence = h.Confidence,
                ModelVersion = h.ModelVersion,
                EnteredDate = DateTime.UtcNow,
            };
            _context.AiHomeopathicConcepts.Add(row);
        }

        if (graph.HomeopathicConcepts.Count == 0)
            return;

        await _context.SaveChangesAsync(cancellationToken);

        var saved = await _context.AiHomeopathicConcepts.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .Select(x => new { x.HomeopathicConceptId, x.ConceptName })
            .ToListAsync(cancellationToken);

        foreach (var h in saved)
        {
            if (string.IsNullOrWhiteSpace(h.ConceptName))
                continue;
            homeoByName[h.ConceptName.Trim()] = h.HomeopathicConceptId;
        }

        for (var i = 0; i < graph.HomeopathicConcepts.Count && i < saved.Count; i++)
        {
            graph.HomeopathicConcepts[i].HomeopathicConceptId = saved
                .FirstOrDefault(s => string.Equals(s.ConceptName, graph.HomeopathicConcepts[i].ConceptName, StringComparison.OrdinalIgnoreCase))
                ?.HomeopathicConceptId ?? graph.HomeopathicConcepts[i].HomeopathicConceptId;
        }
    }

    public async Task SaveReasoningAuditAsync(
        Guid sessionId,
        AiReasoningAuditModel audit,
        CancellationToken cancellationToken = default)
    {
        _context.AiReasoningAudits.Add(new AiReasoningAudit
        {
            AudioCaseSessionId = sessionId,
            PipelineStage = Truncate(audit.PipelineStage, 50) ?? "Unknown",
            ModelId = Truncate(audit.ModelId, 20) ?? "unknown",
            RequestJson = audit.RequestJson,
            ResponseJson = audit.ResponseJson,
            LatencyMs = audit.LatencyMs,
            Success = audit.Success,
            // Column is NVARCHAR(2000) — long rejection dumps must not blow SaveChanges.
            ErrorMessage = Truncate(audit.ErrorMessage, 2000),
            EnteredDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLen ? value : value[..(maxLen - 3)] + "...";
    }

    public async Task<List<AiConceptMappingBootstrapModel>> GetActiveConceptMappingsAsync(
        CancellationToken cancellationToken = default) =>
        await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .Select(x => new AiConceptMappingBootstrapModel
            {
                HomeopathicConceptPattern = x.HomeopathicConceptPattern,
                SubSectionNamePattern = x.SubSectionNamePattern,
                Domain = x.Domain,
                PriorityOrder = x.PriorityOrder,
            }).ToListAsync(cancellationToken);

    public async Task<Dictionary<string, decimal>> GetLearnedConceptWeightsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.AiCaseLearnings.AsNoTracking()
            .GroupBy(x => x.FromConcept)
            .Select(g => new { g.Key, Weight = g.Sum(x => x.WeightDelta) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Key, x => x.Weight, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveCoverageMetricsAsync(
        Guid sessionId,
        CaseCoverageMetricsModel metrics,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        CancellationToken cancellationToken = default)
    {
        await _context.AiCaseCoverageMetrics.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _context.AiMissingSymptomCandidates.Where(x => x.AudioCaseSessionId == sessionId).ExecuteDeleteAsync(cancellationToken);

        _context.AiCaseCoverageMetrics.Add(new AiCaseCoverageMetrics
        {
            AudioCaseSessionId = sessionId,
            TranscriptCoverage = metrics.TranscriptCoverage,
            CaseCompleteness = metrics.CaseCompleteness,
            TotalBlocks = metrics.TotalBlocks,
            CoveredBlocks = metrics.CoveredBlocks,
            Tier1Count = acceptedRubrics.Count(r => (r.ConfidenceScore ?? r.MatchScore) >= 0.90m),
            Tier2Count = acceptedRubrics.Count(r =>
            {
                var c = r.ConfidenceScore ?? r.MatchScore;
                return c >= 0.75m && c < 0.90m;
            }),
            Tier3Count = acceptedRubrics.Count(r =>
            {
                var c = r.ConfidenceScore ?? r.MatchScore;
                return c >= 0.60m && c < 0.75m;
            }),
            MissingSymptomCount = metrics.MissingSymptomCount,
            MetricsJson = JsonSerializer.Serialize(metrics, JsonOptions),
            EnteredDate = DateTime.UtcNow,
        });

        foreach (var span in metrics.UncoveredSpans)
        {
            _context.AiMissingSymptomCandidates.Add(new AiMissingSymptomCandidate
            {
                AudioCaseSessionId = sessionId,
                TranscriptSpan = span,
                CategoryHint = "General",
                ResolutionStatus = "Pending",
                EnteredDate = DateTime.UtcNow,
            });
        }

        var session = await _context.AudioCaseSessions.FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId, cancellationToken);
        if (session != null)
        {
            session.TranscriptCoverageScore = metrics.TranscriptCoverage;
            session.CaseCompletenessScore = metrics.CaseCompleteness;
            session.RecallEngineVersion = "v3.5";
            session.ConceptGraphEngineVersion = "v3.5";
            session.ChangedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
