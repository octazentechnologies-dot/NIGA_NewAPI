using System;
using System.Collections.Generic;

namespace Niga_Domain.DTOs
{
    #nullable disable
    public class ImportResultModel
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string Message { get; set; }
        public int NewlyAddedremedyCount { get; set; }
        public int ExistingremedyCount { get; set; }
        public int SkippedCount { get; set; }
        public List<SkippedRowDetail> SkippedRows { get; set; } = new List<SkippedRowDetail>();
        public string SkipFilePath { get; set; }
        public string SkipFileBase64 { get; set; }
        public string SkipFileName { get; set; }
    }

    public class SkippedRowDetail
    {
        public int RowNumber { get; set; }
        public string Subsection { get; set; }
        public string GradeValue { get; set; }
        public string AuthorValue { get; set; }
        public string Reason { get; set; }
    }

    public class ExcelRubricRemedyRow
    {
        public string Subsection { get; set; }
        public string Grade_1 { get; set; }
        public string Author_1 { get; set; }
        public string Grade_2 { get; set; }
        public string Author_2 { get; set; }
        public string Grade_3 { get; set; }
        public string Author_3 { get; set; }
        public string Grade_4 { get; set; }
        public string Author_4 { get; set; }
    }
}
