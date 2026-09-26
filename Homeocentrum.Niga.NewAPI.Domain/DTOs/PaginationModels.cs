using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class PaginationRequestModel
    {
        public const int MaxPageSize = 100;

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        public string? SearchText { get; set; }

        public string? SortBy { get; set; } = "EnteredDate";

        public string? SortDirection { get; set; } = "desc";
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Flat paginated API response: pagination metadata and resultObject at root level.
    /// </summary>
    public class PaginatedApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }

        public List<T> ResultObject { get; set; } = new();

        public static PaginatedApiResponse<T> FromPaginatedResult(
            PaginatedResult<T> result,
            string? message = null)
        {
            return new PaginatedApiResponse<T>
            {
                Success = true,
                Message = message ?? "Data retrieved successfully.",
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalRecords = result.TotalRecords,
                TotalPages = result.TotalPages,
                ResultObject = result.Items ?? new List<T>()
            };
        }
    }

    public class PaginatedApiFailureResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public static PaginatedApiFailureResponse Create(string message) => new()
        {
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// Standard API response with success, message, and resultObject at root level.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public T? ResultObject { get; set; }

        public static ApiResponse<T> CreateSuccess(T result, string? message = null) => new()
        {
            Success = true,
            Message = message ?? "Data retrieved successfully.",
            ResultObject = result
        };

        public static ApiResponse<T> CreateFailure(string message, T? result = default) => new()
        {
            Success = false,
            Message = message,
            ResultObject = result
        };
    }
}
