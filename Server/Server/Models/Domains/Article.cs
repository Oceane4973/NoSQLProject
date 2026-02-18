namespace Server.Models.Domains
{
    /// Article model
    public class Article
    {
        /// Article ID
        public Guid Id { get; set; }

        /// Article name
        public string Name { get; set; } = null!;

        /// Article price
        public decimal Price { get; set; }


        /// To inverse the navigation
        public List<Order> Orders { get; set; } = new();
    }
}
