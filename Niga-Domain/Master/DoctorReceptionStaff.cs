using System;

namespace Niga_Domain.Master;

public partial class DoctorReceptionStaff
{
    public int ReceptionStaffId { get; set; }

    public int DoctorId { get; set; }

    public string UserId { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Address { get; set; }

    public string ContactNumber { get; set; } = null!;

    public string? EmailId { get; set; }

    public string? Country { get; set; }

    public string? State { get; set; }

    public string? City { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;
}
