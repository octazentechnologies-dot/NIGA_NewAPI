using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

/// <summary>PAT-02.02 — Welcome / introduction slides for patient (or doctor) app.</summary>
public class WelcomeSlide
{
    public long WelcomeSlideId { get; set; }
    public string Audience { get; set; } = "Patient";
    public int SortOrder { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Version { get; set; } = "1";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
