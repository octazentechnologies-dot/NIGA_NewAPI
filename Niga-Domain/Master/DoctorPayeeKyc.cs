using System;

namespace Niga_Domain.Master;

public class DoctorPayeeKyc
{
    public long DoctorPayeeKycId { get; set; }
    public int DoctorId { get; set; }
    public string? AccountHolder { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? Ifsc { get; set; }
    public string? Pan { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool DeleteStatus { get; set; }
}
