namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ReferenceRubricImportRowModel
    {
        public int RowNumber { get; set; }
        public string? SubSectionName { get; set; }
        public string? RefSubSectionName { get; set; }
        public string? SkipReason { get; set; }
    }

    public class ReferenceRubricImportErrorModel
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ReferenceRubricImportSkippedFileModel
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ContentBase64 { get; set; } = string.Empty;
    }

    public class ReferenceRubricImportResultModel
    {
        public int TotalRows { get; set; }
        public int InsertedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<ReferenceRubricImportErrorModel> Errors { get; set; } = new();
        public ReferenceRubricImportSkippedFileModel? SkippedFile { get; set; }
    }
}
