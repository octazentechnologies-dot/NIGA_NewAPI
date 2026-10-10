using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DiagnosisRubricDeleteTabWise
    {
        public string DiagnosisTab { get; set; }
        public int DiagnosisRubricId { get; set; }
        public int KeywordId { get; set; }
    }
}
