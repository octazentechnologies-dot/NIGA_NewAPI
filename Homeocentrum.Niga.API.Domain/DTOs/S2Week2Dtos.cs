using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class PublicDoctorListRequest
    {
        public string? Q { get; set; }
        public string? City { get; set; }
        public string? Qualification { get; set; }
        /// <summary>fee, name, or online (default).</summary>
        public string? SortBy { get; set; }
        public bool? IsOnline { get; set; }
        public bool? TeleOnly { get; set; }
        public decimal? MinFee { get; set; }
        public decimal? MaxFee { get; set; }
        /// <summary>Optional CareCategories id (BE-029). Filters when a doctor–category map exists; otherwise ignored safely.</summary>
        public int? CareCategoryId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PublicDoctorCardDto
    {
        public int DoctorId { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        /// <summary>First + Middle + Last (empty parts omitted).</summary>
        public string DisplayName { get; set; } = string.Empty;
        public string? Qualification { get; set; }
        public string? City { get; set; }
        public string? ClinicName { get; set; }
        public decimal? ConsultFeeInClinic { get; set; }
        public decimal? ConsultFeeTele { get; set; }
        /// <summary>Doctor.FeeCurrency. Null when the profile has no currency.</summary>
        public string? FeeCurrency { get; set; }
        public bool IsOnline { get; set; }
        public bool Verified { get; set; }
        /// <summary>Same flag as <see cref="Verified"/>. Kept so public clients can read isVerified.</summary>
        public bool IsVerified { get => Verified; set => Verified = value; }
        public string? PhotoPath { get; set; }
        /// <summary>Absolute URL of GET /api/Profile/Photo/{doctorId}. Null when the doctor has no photo.</summary>
        public string? PhotoUrl { get; set; }
        /// <summary>Doctor.PassingCertNo. Null when the profile has no certificate number.</summary>
        public string? RegistrationNumber { get; set; }
        public string? PassingUniversity { get; set; }
        /// <summary>Not stored on the doctor row. Null until the profile has a years-of-practice field.</summary>
        public int? YearsExperience { get; set; }
        /// <summary>Next open slot start (ISO), or null when none (BE-013).</summary>
        public DateTime? NextAvailableAt { get; set; }
        /// <summary>Clinic street address when stored; null otherwise (BE-014).</summary>
        public string? ClinicAddressLine { get; set; }
        public string RankingSummary { get; set; } = string.Empty;
        public List<string> RankingReasons { get; set; } = new();
        public decimal? AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class PublicDoctorProfileDto : PublicDoctorCardDto
    {
        public string? WorkingHoursNote { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public new List<string> RankingReasons { get; set; } = new();
    }

    public class PublicBookingCreateRequest
    {
        [Required]
        public string Mobile { get; set; } = string.Empty;
        [Required]
        public string PatientName { get; set; } = string.Empty;
        public string? Email { get; set; }
        [Required]
        public DateTime AppointmentDate { get; set; }
        [Required]
        public string AppointmentTime { get; set; } = string.Empty;
        public string VisitType { get; set; } = "InClinic";
        public string ConsultMode { get; set; } = "InClinic";
        public bool IsTele { get; set; }
        [Required]
        public long BookingSessionId { get; set; }
        public string? ConsentPolicyVersion { get; set; }
        /// <summary>When true, the hold is pay-at-clinic. Rejected when the doctor fee config disables it.</summary>
        public bool? PayAtClinic { get; set; }
    }

    public class PatientAuthRequestOtpModel
    {
        [Required]
        public string Mobile { get; set; } = string.Empty;
    }

    public class PatientAuthVerifyOtpModel
    {
        [Required]
        public string Mobile { get; set; } = string.Empty;
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class DoctorProfileMeDto
    {
        public int DoctorId { get; set; }
        public long UserId { get; set; }
        /// <summary>Login name from UserMaster, separate from the display name.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LoginId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RoleName { get; set; }
        public string FirstName { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClinicName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EmailId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MobileNo { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? City { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AddressLine1 { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AddressLine2 { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? State { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StateId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Pincode { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? QualificationId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QualificationName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PassingUniversity { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PassingCertNo { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CasePaperValidity { get; set; }
        public bool PracticeActivated { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PackageName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CountryId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CountryName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DistrictId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DistrictName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CityId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CityName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PinCodeId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? ConsultFeeInClinic { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? ConsultFeeTele { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhotoPath { get; set; }
        /// <summary>Absolute URL of the photo (GET /api/Profile/Photo/{doctorId}). Omitted when the doctor has no photo.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhotoUrl { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WorkingHoursNote { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? FollowUpFeeInClinic { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? FollowUpFeeTele { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FreeFollowUpDaysInClinic { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FreeFollowUpDaysTele { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FeeCurrency { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? GoogleMapsLink { get; set; }
        public bool IsOnline { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public bool DirectoryVisible { get; set; }
        /// <summary>Same stored value as PassingCertNo. There is no separate registration-number column.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RegistrationNo { get; set; }
        /// <summary>Not stored on the doctor row. Omitted until a column exists.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RegistrationValidUntil { get; set; }
        /// <summary>Not stored on the doctor row. Omitted until a column exists.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IssuingCouncil { get; set; }
        /// <summary>Not stored. Account age is on GET /api/Doctor/PracticeStats as accountAgeDays.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? YearsExperience { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DoctorPayeeKycDto? Kyc { get; set; }
    }

    /// <summary>REC-02.01 — Profile/Me for role Reception. Stored on DoctorReceptionStaff, not a new table.</summary>
    public class ReceptionProfileMeDto
    {
        public string Role { get; set; } = "Reception";
        public int ReceptionStaffId { get; set; }
        public int DoctorId { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmailId { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }

    public class DoctorCredentialDocumentDto
    {
        public int DoctorCredentialDocumentId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Degree { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Specialization { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Institution { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PassingYear { get; set; }
        public DateTime EnteredDate { get; set; }
        /// <summary>Not stored on the credential file row.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ValidUntil { get; set; }
        /// <summary>Not stored per document. Doctor verification is VerificationStatus on the profile.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Verified { get; set; }
        /// <summary>Not stored on the credential file row.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IssuingCouncil { get; set; }
        /// <summary>Authorised GET for the stored file (owning doctor or admin JWT).</summary>
        public string DownloadUrl => $"/api/Profile/CredentialDocuments/{DoctorCredentialDocumentId}/File";
    }

    public class DoctorCredentialsMeDto
    {
        public int DoctorId { get; set; }
        public int? DoctorVerificationId { get; set; }
        public string Status { get; set; } = "Pending";
        public List<DoctorCredentialDocumentDto> Documents { get; set; } = new();
    }

    public class DoctorProfileUpdateRequest
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? ClinicName { get; set; }
        public string? EmailId { get; set; }
        public string? MobileNo { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? State { get; set; }
        public int? StateId { get; set; }
        public string? Pincode { get; set; }
        public int? QualificationId { get; set; }
        public string? PassingUniversity { get; set; }
        public string? PassingCertNo { get; set; }
        public int? CasePaperValidity { get; set; }
        public bool? PracticeActivated { get; set; }
        public int? CountryId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
        public int? PinCodeId { get; set; }
        public decimal? ConsultFeeInClinic { get; set; }
        public decimal? ConsultFeeTele { get; set; }
        /// <summary>True clears the tele fee (teleconsultation turned off).</summary>
        public bool? TeleDisabled { get; set; }
        public decimal? FollowUpFeeInClinic { get; set; }
        public decimal? FollowUpFeeTele { get; set; }
        public int? FreeFollowUpDaysInClinic { get; set; }
        public int? FreeFollowUpDaysTele { get; set; }
        public string? FeeCurrency { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string? WorkingHoursNote { get; set; }
        /// <summary>REC-02.02 — used by Reception Profile/Me PUT; ignored for doctors.</summary>
        public string? Country { get; set; }
        public DoctorPayeeKycDto? Kyc { get; set; }
    }

    public class DoctorPayeeKycDto
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AccountHolder { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BankName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AccountNumber { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Ifsc { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Pan { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BranchName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AccountType { get; set; }
    }

    public class AvailabilityMeDto
    {
        public int DoctorId { get; set; }
        public bool IsOnline { get; set; }
        public string? WorkingHoursNote { get; set; }
        public DoctorDailyScheduleModel? TodaySchedule { get; set; }
        public List<DoctorDailyScheduleModel> WeekSchedules { get; set; } = new();
        public int? GapMinutes { get; set; }
        public int? BookingHorizonDays { get; set; }
    }

    public class AvailabilityDayHours
    {
        public DateTime ScheduleDate { get; set; }
        public TimeOnly WorkStartTime { get; set; }
        public TimeOnly WorkEndTime { get; set; }
        public int SlotIntervalMinutes { get; set; } = 15;
        /// <summary>When true, BreakStartTime and BreakEndTime are written. Null clears the break.</summary>
        public bool? IncludeBreak { get; set; }
        public TimeOnly? BreakStartTime { get; set; }
        public TimeOnly? BreakEndTime { get; set; }
    }

    public class AvailabilityUpdateRequest
    {
        public bool IsOnline { get; set; }
        public string? WorkingHoursNote { get; set; }
        /// <summary>Optional daily slot hours for ScheduleDate (default today). All three required together.</summary>
        public DateTime? ScheduleDate { get; set; }
        public TimeOnly? WorkStartTime { get; set; }
        public TimeOnly? WorkEndTime { get; set; }
        public int? SlotIntervalMinutes { get; set; }
        public int? GapMinutes { get; set; }
        public int? BookingHorizonDays { get; set; }
        public List<AvailabilityDayHours>? Days { get; set; }
        /// <summary>Dates to remove from DoctorDailySchedule (day marked unavailable in the Hours UI).</summary>
        public List<DateTime>? ClearScheduleDates { get; set; }
    }

    public class CogRubricInput
    {
        public int SubSectionId { get; set; }
        public int Intensity { get; set; } = 1;
    }

    public class CenterOfGravityRequest
    {
        public int? PatientId { get; set; }
        public List<CogRubricInput> Rubrics { get; set; } = new();
    }

    public class CenterOfGravityRemedyDto
    {
        public int RemedyId { get; set; }
        public string RemedyName { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<int> ContributingSubSectionIds { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    public class EnquiryCreateRequest
    {
        [Required]
        public string EnquiryName { get; set; } = string.Empty;
        public string? EmailId { get; set; }
        public string? MobileNo { get; set; }
        [Required]
        public string EnquiryDetails { get; set; } = string.Empty;
    }

    public class EnquiryListItemDto
    {
        public int EnquiryId { get; set; }
        public string? EnquiryName { get; set; }
        public DateTime? EnquiryDate { get; set; }
        public string? EmailId { get; set; }
        public string? MobileNo { get; set; }
        public string? EnquiryDetails { get; set; }
        public bool? EnquiryStatus { get; set; }
        public string? TicketStatus { get; set; }
        public long? AssignedTo { get; set; }
        /// <summary>SUP-05.01 — first-response SLA is 24 hours after EnquiryDate.</summary>
        public DateTime? SlaDueAt { get; set; }
        public bool SlaBreached { get; set; }
    }

    public class PatientHomeDashboardDto
    {
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public int FamilyCount { get; set; }
        public int CaregiverCount { get; set; }
        public int UpcomingAppointments { get; set; }
        public List<PatientAppointmentModel> NextAppointments { get; set; } = new();
    }

    public class CareCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ActivateByTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ResendActivationRequest
    {
        [Required]
        public string EmailId { get; set; } = string.Empty;
    }
}
