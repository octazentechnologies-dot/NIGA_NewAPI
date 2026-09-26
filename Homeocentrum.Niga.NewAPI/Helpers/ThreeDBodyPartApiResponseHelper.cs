using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Helpers
{
    public static class ThreeDBodyPartApiResponseHelper
    {
        public static PaginatedApiResponse<T> PaginatedSuccess<T>(
            PaginatedResult<T> data,
            string? message = null)
            => PaginatedApiResponse<T>.FromPaginatedResult(data, message);

        public static PaginatedApiFailureResponse PaginatedFailure(string message)
            => PaginatedApiFailureResponse.Create(message);

        public static PaginatedApiFailureResponse PaginatedError(string message)
            => PaginatedApiFailureResponse.Create(message);

        public static ApiResponse<T> Success<T>(T data, string? message = null)
            => ApiResponse<T>.CreateSuccess(data, message);

        public static PaginatedApiFailureResponse Failure(string message)
            => PaginatedApiFailureResponse.Create(message);

        public static PaginatedApiFailureResponse Error(string message)
            => PaginatedApiFailureResponse.Create(message);

        public static PrescriptionDetailsPaginatedApiResponse PrescriptionPaginatedSuccess(
            PrescriptionDetailsPaginatedResult result,
            string? message = null)
            => PrescriptionDetailsPaginatedApiResponse.FromResult(result, message);
    }
}
