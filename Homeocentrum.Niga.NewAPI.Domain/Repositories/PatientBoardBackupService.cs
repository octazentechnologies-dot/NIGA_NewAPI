using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

public class PatientBoardBackupService : IPatientBoardBackupService
{
    private readonly NIGACentrumContext _context;

    public PatientBoardBackupService(NIGACentrumContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, SavePatientBoardBackupResultModel? Result)> SaveLatestBackupAsync(
        int doctorUserId,
        SavePatientBoardBackupRequest request)
    {
        if (doctorUserId <= 0)
        {
            return (false, "Invalid user.", null);
        }

        if (string.IsNullOrWhiteSpace(request.BackupPayload))
        {
            return (false, "Backup payload is required.", null);
        }

        request.BackupPayload = BoardBackupIntensityMerger.EnsureIntensityPersisted(request.BackupPayload);

        var now = DateTime.UtcNow;
        var existing = await _context.DoctorPatientBoardBackups
            .Where(x => x.DoctorUserId == doctorUserId && !x.DeleteStatus)
            .OrderByDescending(x => x.BackupId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.BackupPayload = request.BackupPayload;
            existing.PatientCount = request.PatientCount;
            existing.SchemaVersion = request.SchemaVersion <= 0 ? 1 : request.SchemaVersion;
            existing.ChangedBy = doctorUserId;
            existing.ChangedDate = now;
        }
        else
        {
            existing = new DoctorPatientBoardBackup
            {
                DoctorUserId = doctorUserId,
                BackupPayload = request.BackupPayload,
                PatientCount = request.PatientCount,
                SchemaVersion = request.SchemaVersion <= 0 ? 1 : request.SchemaVersion,
                EnteredBy = doctorUserId,
                EnteredDate = now,
                DeleteStatus = false,
            };
            _context.DoctorPatientBoardBackups.Add(existing);
        }

        await _context.SaveChangesAsync();

        return (true, "Patient board backup saved successfully.", new SavePatientBoardBackupResultModel
        {
            BackupId = existing.BackupId,
            PatientCount = existing.PatientCount,
            SavedAt = existing.ChangedDate ?? existing.EnteredDate,
        });
    }

    public async Task<(bool Success, string Message, PatientBoardBackupSummaryModel? Result)> GetBackupSummaryAsync(int doctorUserId)
    {
        if (doctorUserId <= 0)
        {
            return (false, "Invalid user.", null);
        }

        var backup = await _context.DoctorPatientBoardBackups
            .AsNoTracking()
            .Where(x => x.DoctorUserId == doctorUserId && !x.DeleteStatus)
            .OrderByDescending(x => x.BackupId)
            .FirstOrDefaultAsync();

        if (backup == null)
        {
            return (true, "No backup found.", new PatientBoardBackupSummaryModel
            {
                HasBackup = false,
            });
        }

        return (true, "Backup summary retrieved.", new PatientBoardBackupSummaryModel
        {
            HasBackup = true,
            BackupId = backup.BackupId,
            PatientCount = backup.PatientCount,
            SchemaVersion = backup.SchemaVersion,
            SavedAt = backup.ChangedDate ?? backup.EnteredDate,
        });
    }

    public async Task<(bool Success, string Message, PatientBoardBackupDetailModel? Result)> GetLatestBackupAsync(int doctorUserId)
    {
        if (doctorUserId <= 0)
        {
            return (false, "Invalid user.", null);
        }

        var backup = await _context.DoctorPatientBoardBackups
            .AsNoTracking()
            .Where(x => x.DoctorUserId == doctorUserId && !x.DeleteStatus)
            .OrderByDescending(x => x.BackupId)
            .FirstOrDefaultAsync();

        if (backup == null)
        {
            return (false, "No backup found.", null);
        }

        return (true, "Backup retrieved successfully.", new PatientBoardBackupDetailModel
        {
            BackupId = backup.BackupId,
            PatientCount = backup.PatientCount,
            SchemaVersion = backup.SchemaVersion,
            SavedAt = backup.ChangedDate ?? backup.EnteredDate,
            BackupPayload = BoardBackupIntensityMerger.EnsureIntensityPersisted(backup.BackupPayload),
        });
    }

    public async Task<(bool Success, string Message)> DeleteLatestBackupAsync(int doctorUserId)
    {
        if (doctorUserId <= 0)
        {
            return (false, "Invalid user.");
        }

        var backup = await _context.DoctorPatientBoardBackups
            .Where(x => x.DoctorUserId == doctorUserId && !x.DeleteStatus)
            .OrderByDescending(x => x.BackupId)
            .FirstOrDefaultAsync();

        if (backup == null)
        {
            return (false, "No backup found.");
        }

        backup.DeleteStatus = true;
        backup.ChangedBy = doctorUserId;
        backup.ChangedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Backup deleted successfully.");
    }
}
