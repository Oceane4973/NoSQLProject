namespace Server.Models.Domains
{
    /// <summary>
    /// 
    /// </summary>
    public class UserFollow
    {
        /// Composite key of FollowerId and FollowingId
        public Guid FollowerId { get; set; }

        /// Navigation property to the follower user
        public User Follower { get; set; } = null!;

        /// Composite key of FollowerId and FollowingId
        public Guid FollowingId { get; set; }

        /// Navigation property to the following user
        public User Following { get; set; } = null!;

    }
}
