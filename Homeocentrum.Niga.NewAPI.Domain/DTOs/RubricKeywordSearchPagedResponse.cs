using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RubricKeywordSearchPagedResponse
    {
        public List<RubricKeywordModel> Items { get; set; } = new List<RubricKeywordModel>();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public bool HasMore { get; set; }
    }
}
