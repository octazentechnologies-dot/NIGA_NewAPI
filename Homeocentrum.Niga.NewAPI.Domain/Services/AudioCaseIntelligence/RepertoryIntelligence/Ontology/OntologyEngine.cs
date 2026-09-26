using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ontology;

/// <summary>V7: clinical symptom relationships for expanded search.</summary>
public interface IOntologyEngine
{
    IReadOnlyList<V7OntologyNode> ExpandRelations(string concept);

    IReadOnlyList<string> ExpandSearchTerms(string concept, int maxTerms = 20);
}

public class OntologyEngine : IOntologyEngine
{
    private static readonly List<(string From, string Relation, string To)> Relations = BuildRelations();

    public IReadOnlyList<V7OntologyNode> ExpandRelations(string concept)
    {
        if (string.IsNullOrWhiteSpace(concept))
        {
            return Array.Empty<V7OntologyNode>();
        }

        var normalized = concept.Trim();
        var nodes = new List<V7OntologyNode>();

        foreach (var (from, relation, to) in Relations)
        {
            if (normalized.Contains(from, StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(to, StringComparison.OrdinalIgnoreCase))
            {
                nodes.Add(new V7OntologyNode
                {
                    Concept = from,
                    RelationType = relation,
                    RelatedConcept = to,
                });
            }
        }

        return nodes;
    }

    public IReadOnlyList<string> ExpandSearchTerms(string concept, int maxTerms = 20)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { concept.Trim() };

        foreach (var node in ExpandRelations(concept))
        {
            terms.Add(node.Concept);
            terms.Add(node.RelatedConcept);
            if (terms.Count >= maxTerms)
            {
                break;
            }
        }

        return terms.Take(maxTerms).ToList();
    }

    private static List<(string From, string Relation, string To)> BuildRelations() =>
        new()
        {
            ("convulsion", "Parent", "aura"),
            ("aura", "Child", "premonitory"),
            ("vibration", "Related", "aura"),
            ("shock", "Related", "aura"),
            ("hands", "Parent", "drops things"),
            ("drops things", "Child", "awkwardness"),
            ("awkwardness", "Sibling", "weakness"),
            ("fear", "ClinicalAssociation", "convulsion"),
            ("fear", "Broader", "anxiety"),
            ("salt", "Equivalent", "desire salt"),
            ("thirst", "Broader", "generals"),
            ("forgetfulness", "Narrower", "memory after convulsion"),
            ("talking in sleep", "Related", "somnambulism"),
            ("fear heights", "ClinicalAssociation", "vertigo"),
            ("meat", "Equivalent", "desire meat"),
            ("fit", "Equivalent", "convulsion"),
            ("seizure", "Equivalent", "convulsion"),
            ("attack", "Equivalent", "convulsion"),
        };
}
