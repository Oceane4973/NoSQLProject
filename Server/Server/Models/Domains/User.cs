namespace Server.Models.Domains
{
    /// User model
    public class User
    {
        /// User ID
        public Guid Id { get; set; }

        /// User name
        public string Name { get; set; } = null!;

        /// User email
        public string Email { get; set; } = null!;


        /// User followers
        public List<User> Followers { get; set; } = new List<User>();

        /// User following
        public List<User> Following { get; set; } = new();

        /// User orders
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
