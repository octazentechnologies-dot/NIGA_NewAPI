using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class PrescriptionRubricDetailModel
    {
        public int PrescriptionRubricId { get; set; } = 0;
        public int RubricId { get; set; } = 0;
        public int IntensityId { get; set; } = 0;
        public int RemedyCount { get; set; } = 0;
    }

    public class PrescriptionRubricDetailViewModel
    {
        public int PrescriptionRubricId { get; set; } = 0;
        public int AppointmentId { get; set; } = 0;
        public int RubricId { get; set; } = 0;
        public string RubricName { get; set; } = string.Empty;
        public int IntensityId { get; set; } = 0;
        public int RemedyCount { get; set; } = 0;
        public string? CreatedDate { get; set; }
    }

    /// <summary>
    /// Combined prescription rubric and remedy details for an appointment.
    /// </summary>
    public class PrescriptionDetailsByAppointmentModel
    {
        public List<PrescriptionRubricDetailViewModel> RubricDetails { get; set; } = new();

        public List<PrescriptionRemedyDetailViewModel> RemedyDetails { get; set; } = new();
    }

    /// <summary>
    /// Query parameters for GetPrescriptionDetailsByAppointmentId.
    /// </summary>
    public class GetPrescriptionDetailsByAppointmentIdRequest
    {
        [Required(ErrorMessage = "AppointmentId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "AppointmentId must be greater than 0")]
        public int AppointmentId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, PaginationRequestModel.MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Paginated prescription rubric and remedy details for an appointment.
    /// </summary>
    public class PrescriptionDetailsPaginatedResult
    {
        public PrescriptionDetailsByAppointmentModel Details { get; set; } = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int RubricTotalRecords { get; set; }

        public int RubricTotalPages { get; set; }

        public int RemedyTotalRecords { get; set; }

        public int RemedyTotalPages { get; set; }
    }

    /// <summary>
    /// Paginated API response for prescription details with rubric and remedy totals.
    /// </summary>
    public class PrescriptionDetailsPaginatedApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int RubricTotalRecords { get; set; }

        public int RubricTotalPages { get; set; }

        public int RemedyTotalRecords { get; set; }

        public int RemedyTotalPages { get; set; }

        public PrescriptionDetailsByAppointmentModel ResultObject { get; set; } = new();

        public static PrescriptionDetailsPaginatedApiResponse FromResult(
            PrescriptionDetailsPaginatedResult result,
            string? message = null) => new()
        {
            Success = true,
            Message = message ?? "Prescription details retrieved successfully.",
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            RubricTotalRecords = result.RubricTotalRecords,
            RubricTotalPages = result.RubricTotalPages,
            RemedyTotalRecords = result.RemedyTotalRecords,
            RemedyTotalPages = result.RemedyTotalPages,
            ResultObject = result.Details
        };
    }

    public class PrescriptionDetailModel
    {

        public PrescriptionDetailModel() {
            this.PrescriptionRubricDetailList = new List<PrescriptionRubricDetailModel>();
            this.PrescriptionRemedyDetailList = new List<PrescriptionRemedyDetailModel>();
        }
        public int AppointmentId { get; set; } = 0;

        public List<PrescriptionRubricDetailModel> PrescriptionRubricDetailList { get; set; }  
        public List<PrescriptionRemedyDetailModel> PrescriptionRemedyDetailList { get; set; }

    }
}
