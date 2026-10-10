namespace Homeocentrum.Niga.API.Domain.Interfaces
{
    public interface IDateTime
    {
        DateTime Now { get; }
         DateTime UtcNow { get; }
    }
}
