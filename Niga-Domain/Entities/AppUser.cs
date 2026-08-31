using Niga_Domain.Master;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser<int>
    {
#nullable disable

        public ICollection<AppUserRole> UserRoles { get; set; }
    }
}