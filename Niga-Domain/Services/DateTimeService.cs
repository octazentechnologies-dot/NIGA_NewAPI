 
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }

}
