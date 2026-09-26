namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RubricEmbedding
{
    public long Id { get; set; }

    public int RubricId { get; set; }

    public string EmbeddingJson { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string TextHash { get; set; } = null!;

    public string SourceType { get; set; } = "SubSection";

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
