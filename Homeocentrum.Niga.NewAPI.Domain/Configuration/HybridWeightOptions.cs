namespace Homeocentrum.Niga.NewAPI.Domain.Configuration;

public class HybridWeightOptions
{
    public decimal Embedding { get; set; } = 0.40m;

    public decimal Alias { get; set; } = 0.30m;

    public decimal ClinicalMeaning { get; set; } = 0.20m;

    public decimal KeywordLike { get; set; } = 0.10m;
}
