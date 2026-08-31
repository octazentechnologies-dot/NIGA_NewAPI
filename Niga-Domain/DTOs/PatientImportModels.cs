namespace Niga_Domain.DTOs
{
    public class PatientImportRowModel
    {
        public int RowNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? MobileNo { get; set; }
        public string? PhoneNo { get; set; }
        public string? Email { get; set; }
        public string? ReferBy { get; set; }
        public string? WhatsAppOptIn { get; set; }
        public string? AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? SkipReason { get; set; }
    }

    public class PatientImportErrorModel
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PatientImportSkippedFileModel
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ContentBase64 { get; set; } = string.Empty;
    }

    public class PatientImportResultModel
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<PatientImportErrorModel> Errors { get; set; } = new();
        public PatientImportSkippedFileModel? SkippedFile { get; set; }
        public bool HasAppointments { get; set; }
    }
}
