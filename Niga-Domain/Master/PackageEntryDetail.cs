using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class PackageEntryDetail
{
    public int PackageDetailId { get; set; }

    public int? PackageId { get; set; }

    public int? DoctorId { get; set; }

    public DateTime? ActivationDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? OrderId { get; set; }

    public string? TransactionId { get; set; }

    public string? PaymentId { get; set; }

    public bool? IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Doctor? Doctor { get; set; }

    public virtual PackageMaster? Package { get; set; }
}
