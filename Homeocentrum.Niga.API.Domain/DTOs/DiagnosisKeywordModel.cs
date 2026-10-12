using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DiagnosisKeywordModel
    {
        public int KeywordId { get; set; } = 0;
       public string keyword { get; set; } = string.Empty;
        public List<int>? SectionIds { get; set; }
    }
}
