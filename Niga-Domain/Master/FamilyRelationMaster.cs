namespace Niga_Domain.Master;

/// <summary>Searchable family relation list. Patients can add a missing relation.</summary>
public class FamilyRelationMaster
{
    public int RelationId { get; set; }
    public string RelationName { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool DeleteStatus { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime EnteredDate { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? ChangedDate { get; set; }
}
