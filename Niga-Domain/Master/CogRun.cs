using System;

namespace Niga_Domain.Master;

public class CogRun
{
    public long CogRunId { get; set; }
    public int DoctorId { get; set; }
    public int? PatientId { get; set; }
    public string InputJson { get; set; } = null!;
    public string? OutputJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
