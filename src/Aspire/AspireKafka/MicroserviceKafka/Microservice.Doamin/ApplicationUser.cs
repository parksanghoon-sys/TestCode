using Microsoft.AspNetCore.Identity;

namespace Microservice.Doamin
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
