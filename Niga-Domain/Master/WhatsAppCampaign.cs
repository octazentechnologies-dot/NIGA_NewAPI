using System;

namespace Niga_Domain.Master;

public partial class WhatsAppCampaign
{
    public int CampaignId { get; set; }

    public string CampaignName { get; set; } = null!;

    public string CampaignCategory { get; set; } = null!;

    public int DoctorId { get; set; }

    public string? MessageBody { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsBulk { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public bool DeleteStatus { get; set; }
}
