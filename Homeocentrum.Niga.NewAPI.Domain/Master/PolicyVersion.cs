using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public class PolicyVersion
{
    public int PolicyVersionId { get; set; }
    public string PolicyType { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? BodyHtml { get; set; }
    public DateTime EffectiveAt { get; set; }
    public bool IsCurrent { get; set; }
}
