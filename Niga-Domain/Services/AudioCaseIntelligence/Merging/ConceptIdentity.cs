namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>Stable Guids for V3 homeopathic concept ids across discovery/citation paths.</summary>
public static class ConceptIdentity
{
    public static Guid FromHomeopathicConceptId(long homeopathicConceptId)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(0xC607_A11C).CopyTo(bytes, 0);
        BitConverter.GetBytes(homeopathicConceptId).CopyTo(bytes, 4);
        return new Guid(bytes);
    }
}
