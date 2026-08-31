using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class ClinicalQueKeywordsModel
    {
        public int ClinicalQueKeywordId { get; set; }
        public int? QuestionsId { get; set; }
        public string KeywordQuestion { get; set; }
        public bool? IsDeleted { get; set; }

    }
}
