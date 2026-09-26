using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PackageEntryDetail
{
    // M02 W7 ADM-B03: This table is S1 SaaS doctor subscription only.
    // Do NOT reuse for S2 consult billing or S5 medicine/pharmacy ledger.

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
