using Homeocentrum.Niga.NewAPI.Domain.Master;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppRole : IdentityRole<int>
    { 
        public ICollection<AppUserRole> UserRoles { get; set; }
        
       
        
    }
}