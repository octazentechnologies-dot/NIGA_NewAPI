namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    public interface IDateTime
    {
        DateTime Now { get; }
         DateTime UtcNow { get; }
    }
}
