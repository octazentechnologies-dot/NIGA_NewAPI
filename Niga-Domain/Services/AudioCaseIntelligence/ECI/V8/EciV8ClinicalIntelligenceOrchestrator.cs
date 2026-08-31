using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Conversation;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Extraction;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Validation;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8;

public interface IEciV8ClinicalIntelligenceOrchestrator
{
    Task<EciV8Result> AnalyzeAsync(EciV8Request request, CancellationToken cancellationToken = default);
}

/// <summary>
/// ECI v8: end-to-end pipeline orchestrator.
/// Phase A/B includes: parser → extractor → validator → (graph enrichment hook) → database intelligence hook.
/// </summary>
public sealed class EciV8ClinicalIntelligenceOrchestrator : IEciV8ClinicalIntelligenceOrchestrator
{
    private readonly IEciConversationParser _parser;
    private readonly IEciStructuredSymptomExtractor _extractor;
    private readonly IEciClinicalValidator _validator;
    private readonly IEciDatabaseIntelligenceEngine _db;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<EciV8ClinicalIntelligenceOrchestrator> _logger;

    public EciV8ClinicalIntelligenceOrchestrator(
        IEciConversationParser parser,
        IEciStructuredSymptomExtractor extractor,
        IEciClinicalValidator validator,
        IEciDatabaseIntelligenceEngine db,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<EciV8ClinicalIntelligenceOrchestrator> logger)
    {
        _parser = parser;
        _extractor = extractor;
        _validator = validator;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EciV8Result> AnalyzeAsync(EciV8Request request, CancellationToken cancellationToken = default)
    {
        var result = new EciV8Result();
        var total = Stopwatch.StartNew();

        try
        {
            var sw = Stopwatch.StartNew();
            var parsed = await _parser.ParseAsync(request.Transcript, cancellationToken);
            sw.Stop();
            result.Diagnostics.Add(EciV8StageNames.ConversationParser, (int)sw.ElapsedMilliseconds,
                new { turnCount = parsed.Turns.Count });

            if (!parsed.Success)
            {
                result.Success = false;
                result.Error = parsed.Error ?? "Conversation parsing failed.";
                return result;
            }

            result.CleanTranscript = parsed.CleanTranscript;

            sw.Restart();
            var extracted = await _extractor.ExtractAsync(parsed.CleanTranscript, cancellationToken);
            sw.Stop();
            result.Diagnostics.Add(EciV8StageNames.StructuredSymptomExtraction, (int)sw.ElapsedMilliseconds,
                new { symptomCount = extracted.Symptoms.Count });
            result.ExtractedSymptoms = extracted.Symptoms;

            sw.Restart();
            var validated = _validator.Validate(extracted.Symptoms, parsed.CleanTranscript);
            sw.Stop();
            result.Diagnostics.Add(EciV8StageNames.ClinicalValidation, (int)sw.ElapsedMilliseconds,
                new { accepted = validated.Accepted.Count, rejected = validated.Rejected.Count });
            result.ValidatedSymptoms = validated.Validated;

            // Phase B: V3 graph is an enrichment-only hook. We pass it to DB engine but it must never select rubrics.
            sw.Restart();
            var rubrics = await _db.RetrieveRubricsAsync(
                validated.Accepted,
                parsed.CleanTranscript,
                request.Graph,
                cancellationToken);
            sw.Stop();
            result.Diagnostics.Add(EciV8StageNames.DatabaseIntelligence, (int)sw.ElapsedMilliseconds,
                new { rubricCount = rubrics.Count });

            result.FinalDatabaseRubrics = rubrics.ToList();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "ECI v8 analysis failed for SessionId={SessionId}", request.SessionId);
        }
        finally
        {
            total.Stop();
            result.Diagnostics.Add("Total", (int)total.ElapsedMilliseconds);
        }

        return result;
    }
}

