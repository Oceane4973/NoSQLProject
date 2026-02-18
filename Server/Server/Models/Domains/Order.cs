namespace Server.Models.Domains
{
    /// Order model
    public class Order
    {
        /// Order ID
        public Guid Id { get; set; }

        /// User ID
        public Guid UserId { get; set; }

        /// Article ID
        public Guid ArticleId { get; set; }

        /// Article quantity
        public int Quantity { get; set; }


        /// To reverse the navigation
        public User User { get; set; } = null!;

        /// To reverse the navigation
        public Article Article { get; set; } = null!;


        /// Total price of the order
        public decimal TotalPrice => Quantity * Article.Price;
    }
}
