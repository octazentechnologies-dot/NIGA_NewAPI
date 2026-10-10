 
using Homeocentrum.Niga.API.Domain.Interfaces;

namespace Homeocentrum.Niga.API.Domain.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }

}
