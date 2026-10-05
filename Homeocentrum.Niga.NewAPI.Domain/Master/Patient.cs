using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class Patient:AuditableEntities
{
    public int PatientId { get; set; }

    public string? PatientName { get; set; }

    /// <summary>Legacy single-line address (kept in sync for older clients).</summary>
    public string? Address { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? Landmark { get; set; }

    public int? StateId { get; set; }

    public int? CountryId { get; set; }

    public int? DistrictId { get; set; }

    public int? CityId { get; set; }

    /// <summary>FK to PinCodeMaster.PinCodeId (not free-text PIN).</summary>
    public int? PinCodeId { get; set; }

    public string? MobileNo { get; set; }

    public string? PhoneNo { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public int? Gender { get; set; }

    public bool? DeleteStatus { get; set; }

    public string? Email { get; set; }

    public int? Age { get; set; }

    public bool IsWhatsAppOptIn { get; set; }

    public DateTime? WhatsAppOptInDate { get; set; }

    public virtual ICollection<CaseEntryDetail> CaseEntryDetails { get; set; } = new List<CaseEntryDetail>();

    public virtual CountryMaster? Country { get; set; }

    public virtual ICollection<PatientAppointment> PatientAppointments { get; set; } = new List<PatientAppointment>();

    public virtual StateMaster? State { get; set; }
}
