namespace WebApplication1.Models
{
    public class User
    {
        private static int _id = 8;
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }

        public User(int id, string name, string email, bool isActive = true)
        {
            Id = id;
            Name = name;
            Email = email;
            IsActive = isActive;
        }
        public User(string name, string email)
            : this(_id, name, email,false)
        {
            _id++;
        }
    }
}
