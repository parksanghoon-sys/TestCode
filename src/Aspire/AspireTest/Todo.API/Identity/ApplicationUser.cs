using Microsoft.AspNetCore.Identity;

namespace Todo.API.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
