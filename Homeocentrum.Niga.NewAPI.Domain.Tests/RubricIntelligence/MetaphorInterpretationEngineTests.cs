using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class MetaphorInterpretationEngineTests
{
    [Fact]
    public async Task EnrichAsync_AppliesApprovedMetaphor_ToMatchingConcept()
    {
        var engine = new MetaphorInterpretationEngine(new FakeMetaphorAdminService());
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "vibration in hands before fit",
                Confidence = 0.7m,
            },
        };

        var result = await engine.EnrichAsync(concepts, "en");
        Assert.Equal(1, result.MetaphorsMatched);
        Assert.Contains("aura", result.Concepts[0].ClinicalMeaning!, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeMetaphorAdminService : IRubricIntelligenceAdminService
    {
        public Task<RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>> GetWeightsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>());
        public Task<HomeopathicWeightRuleModel?> GetWeightByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<HomeopathicWeightRuleModel?>(null);
        public Task<(bool Success, string Message, HomeopathicWeightRuleModel? Result)> UpdateWeightAsync(int adminUserId, int id, HomeopathicWeightRuleUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<RubricMetaphorDictionary>> SearchApprovedMetaphorsAsync(IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RubricMetaphorDictionary>
            {
                new()
                {
                    NormalizedExpression = "vibration before fit",
                    ClinicalMeaning = "Prodromal aura before convulsion",
                    RubricMeaning = "GENERALITIES - CONVULSIONS - aura",
                    ConfidenceWeight = 0.92m,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Language = "en",
                    PatientExpression = "vibration before fit",
                },
            });

        public Task<List<(RubricAlias Alias, string SubSectionName)>> SearchActiveAliasesAsync(IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<(RubricAlias, string)>());

        public Task<RubricIntelligenceAdminListModel<RubricMetaphorModel>> GetMetaphorsAsync(string? search, string? language, string? approvalStatus, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RubricMetaphorModel?> GetMetaphorByIdAsync(long id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message, RubricMetaphorModel? Result)> CreateMetaphorAsync(int adminUserId, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message, RubricMetaphorModel? Result)> UpdateMetaphorAsync(int adminUserId, long id, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message)> DeleteMetaphorAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message)> ApproveMetaphorAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message)> RejectMetaphorAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RubricIntelligenceAdminListModel<RubricAliasModel>> GetAliasesAsync(string? search, string? language, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RubricAliasModel?> GetAliasByIdAsync(long id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message, RubricAliasModel? Result)> CreateAliasAsync(int adminUserId, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message, RubricAliasModel? Result)> UpdateAliasAsync(int adminUserId, long id, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(bool Success, string Message)> DeleteAliasAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
