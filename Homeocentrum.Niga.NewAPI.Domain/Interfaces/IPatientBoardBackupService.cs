using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IPatientBoardBackupService
{
    Task<(bool Success, string Message, SavePatientBoardBackupResultModel? Result)> SaveLatestBackupAsync(
        int doctorUserId,
        SavePatientBoardBackupRequest request);

    Task<(bool Success, string Message, PatientBoardBackupSummaryModel? Result)> GetBackupSummaryAsync(int doctorUserId);

    Task<(bool Success, string Message, PatientBoardBackupDetailModel? Result)> GetLatestBackupAsync(int doctorUserId);

    Task<(bool Success, string Message)> DeleteLatestBackupAsync(int doctorUserId);
}
