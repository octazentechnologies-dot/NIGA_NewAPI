using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    #nullable disable
    public static class ImportJobStatuses
    {
        public const string Queued = "Queued";
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }

    public class ImportJobStartResult
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class ImportJobStatusModel
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public string FileName { get; set; }
        public int ProcessedRows { get; set; }
        public int TotalRows { get; set; }
        public int ProgressPercent { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public ImportResultModel Result { get; set; }
    }

    public class RubricRemedyImportJob
    {
        public string JobId { get; set; }
        public string FilePath { get; set; }
        public string OriginalFileName { get; set; }
    }
}
