using System;
using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    public class AddReceptionStaffRequest
    {
        public int DoctorUserID { get; set; }

        [Required(ErrorMessage = "UserID is required")]
        [MaxLength(100)]
        public string UserID { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(500)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "FullName is required")]
        [MaxLength(250)]
        public string FullName { get; set; } = null!;

        public string? Address { get; set; }

        [Required(ErrorMessage = "ContactNumber is required")]
        [MaxLength(50)]
        public string ContactNumber { get; set; } = null!;

        [MaxLength(250)]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? EmailId { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public int? EnteredBy { get; set; }
    }

    public class UpdateReceptionStaffRequest
    {
        [Required(ErrorMessage = "ReceptionStaffID is required")]
        public int ReceptionStaffID { get; set; }

        [Required(ErrorMessage = "FullName is required")]
        [MaxLength(250)]
        public string FullName { get; set; } = null!;

        public string? Address { get; set; }

        [Required(ErrorMessage = "ContactNumber is required")]
        [MaxLength(50)]
        public string ContactNumber { get; set; } = null!;

        [MaxLength(250)]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? EmailId { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public int? ChangedBy { get; set; }
    }

    public class DeleteReceptionStaffRequest
    {
        [Required(ErrorMessage = "ReceptionStaffID is required")]
        public int ReceptionStaffID { get; set; }

        public int? ChangedBy { get; set; }
    }

    public class ReceptionStaffLoginRequest
    {
        [Required(ErrorMessage = "UserID is required")]
        public string UserID { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }

    public class GetReceptionStaffListRequest
    {
        public int DoctorUserID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, PaginationRequestModel.MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }

    public class ReceptionStaffResponseModel
    {
        public int ReceptionStaffID { get; set; }

        public int DoctorID { get; set; }

        public string? DoctorName { get; set; }

        public string UserID { get; set; } = null!;

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
    }

    public class ReceptionStaffListItemModel
    {
        public int ReceptionStaffID { get; set; }

        public int DoctorID { get; set; }

        public string UserID { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string ContactNumber { get; set; } = null!;

        public string? EmailId { get; set; }

        public string? City { get; set; }

        public DateTime EnteredDate { get; set; }
    }

    public class AddReceptionStaffResultModel
    {
        public int ReceptionStaffID { get; set; }

        public int DoctorID { get; set; }

        public string UserID { get; set; } = null!;

        public string FullName { get; set; } = null!;
    }

    public class ReceptionStaffLoginResponseModel
    {
        public int ReceptionStaffID { get; set; }

        public int DoctorID { get; set; }

        public string FullName { get; set; } = null!;

        public string Token { get; set; } = null!;
    }
}
