using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    public class PublicDoctorListRequest
    {
        public string? Q { get; set; }
        public string? City { get; set; }
        public bool? IsOnline { get; set; }
        public bool? TeleOnly { get; set; }
        public decimal? MinFee { get; set; }
        public decimal? MaxFee { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PublicDoctorCardDto
    {
        public int DoctorId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Qualification { get; set; }
        public string? City { get; set; }
        public string? ClinicName { get; set; }
        public decimal? ConsultFeeInClinic { get; set; }
        public decimal? ConsultFeeTele { get; set; }
        public bool IsOnline { get; set; }
        public bool Verified { get; set; }
        public string? PhotoPath { get; set; }
        public string RankingSummary { get; set; } = string.Empty;
    }

    public class PublicDoctorProfileDto : PublicDoctorCardDto
    {
        public string? WorkingHoursNote { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public List<string> RankingReasons { get; set; } = new();
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
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? ClinicName { get; set; }
        public string? EmailId { get; set; }
        public string? MobileNo { get; set; }
        public string? City { get; set; }
        public int? QualificationId { get; set; }
        public string? QualificationName { get; set; }
        public string? PassingUniversity { get; set; }
        public string? PassingCertNo { get; set; }
        public decimal? ConsultFeeInClinic { get; set; }
        public decimal? ConsultFeeTele { get; set; }
        public string? PhotoPath { get; set; }
        public string? WorkingHoursNote { get; set; }
        public bool IsOnline { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public bool DirectoryVisible { get; set; }
        public DoctorPayeeKycDto? Kyc { get; set; }
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
        public int? QualificationId { get; set; }
        public string? PassingUniversity { get; set; }
        public string? PassingCertNo { get; set; }
        public decimal? ConsultFeeInClinic { get; set; }
        public decimal? ConsultFeeTele { get; set; }
        public string? WorkingHoursNote { get; set; }
        public DoctorPayeeKycDto? Kyc { get; set; }
    }

    public class DoctorPayeeKycDto
    {
        public string? AccountHolder { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? Ifsc { get; set; }
        public string? Pan { get; set; }
    }

    public class AvailabilityMeDto
    {
        public int DoctorId { get; set; }
        public bool IsOnline { get; set; }
        public string? WorkingHoursNote { get; set; }
        public DoctorDailyScheduleModel? TodaySchedule { get; set; }
    }

    public class AvailabilityUpdateRequest
    {
        public bool IsOnline { get; set; }
        public string? WorkingHoursNote { get; set; }
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
