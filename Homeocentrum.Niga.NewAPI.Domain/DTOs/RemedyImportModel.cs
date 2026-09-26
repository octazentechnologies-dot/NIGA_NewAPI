using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RemedyImportModel
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}