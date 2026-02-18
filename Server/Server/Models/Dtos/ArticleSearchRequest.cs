using Server.Models.Dtos.Enums;

namespace BuyAndRent.Shared.Dtos.DataStorageService;

/// <summary>
/// 
/// </summary>
public class ArticleSearchRequest
{
    /// <summary>
    /// Page number (1-based). Default is 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size. Default is 20.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// To search for articles by name. Partial matches allowed.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// To filter by specific article IDs. If provided, only these articles will be returned.
    /// </summary>
    public List<Guid>? ArticleIds { get; set; }

    /// <summary>
    /// Search all articles sold by a specific user.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// If true, return only articles sold by users that the requesting user follows. Requires UserId to be set.
    /// </summary>
    public bool OnlyFromFollowing { get; set; }

    /// <summary>
    /// Filter by price range.
    /// </summary>
    /// Min price
    public decimal? MinPrice { get; set; }

    /// Max price
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// To include seller details in the search results.
    /// </summary>
    public bool IncludeSellerDetails { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public ArticleFilterOrderBy? OrderBy { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsDescending { get; set; }
}
