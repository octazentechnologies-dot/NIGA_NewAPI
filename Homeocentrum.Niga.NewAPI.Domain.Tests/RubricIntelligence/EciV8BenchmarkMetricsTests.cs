using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public sealed class EciV8BenchmarkMetricsTests
{
    [Fact]
    public void PrecisionRecallF1_ComputesCorrectly()
    {
        var (p, r, f1) = EciMetricsCalculator.ComputePrecisionRecallF1(tp: 9, fp: 1, fn: 3);
        Assert.Equal(0.9m, p);
        Assert.Equal(0.75m, r);
        Assert.Equal(0.8182m, f1);
    }

    [Fact]
    public void Mrr_FirstHitRank2_IsHalf()
    {
        var mrr = EciMetricsCalculator.ComputeMrr(new List<int> { 2, 7, 9 });
        Assert.Equal(0.5m, mrr);
    }

    [Fact]
    public void TopKAccuracy_HitInsideTop5_Returns1()
    {
        var produced = new Dictionary<int, int> { [10] = 4, [11] = 7 };
        var expected = new List<int> { 10 };
        var top5 = EciMetricsCalculator.ComputeTopKAccuracy(5, produced, expected);
        Assert.Equal(1m, top5);
    }

    [Fact]
    public void Ndcg_PerfectRanking_Is1()
    {
        var expectedOrder = new List<int> { 1, 2, 3 };
        var produced = new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3 };
        var ndcg = EciMetricsCalculator.ComputeNdcg(expectedOrder, produced);
        Assert.Equal(1m, ndcg);
    }
}

