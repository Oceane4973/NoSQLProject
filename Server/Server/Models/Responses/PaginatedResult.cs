namespace Server.Models.Responses
{
    /// <summary>
    /// Paginated result container
    /// </summary>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// List of items in current page
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Total number of items
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Request time
        /// </summary>
        public long RequestTimeInMilliseconds { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;
    }
}
