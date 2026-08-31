using API.Helpers;

namespace Niga_Domain.Helpers
{
    public class ParameterParams : PaginationParams
    {
        public string? search { get; set; }
        public string? SortBy { get; set; }
        public string? SortByName { get; set; }
        public int? UserId { get; set; }
        public int? SectionId { get; set; }
        public string? Flag { get; set; }
        public bool? IsExport { get; set; }
        public int? categoryId { get; set; }
        public DateTime? Date { get; set; }
        public int? allopathicDrugId { get; set; }
    }
}