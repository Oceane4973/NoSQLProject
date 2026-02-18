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

        /// Followers
        public List<UserFollow> Followers { get; set; } = new();

        /// Following
        public List<UserFollow> Following { get; set; } = new();

        /// Orders
        public List<Order> Orders { get; set; } = new();
    }
}
