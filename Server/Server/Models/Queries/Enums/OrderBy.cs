
using System.Text.Json.Serialization;

namespace Server.Models.Requests.Enums.Fields;

/// <summary>
/// OrderBy pour Articles
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ArticlesOrderBy>))]
public enum ArticlesOrderBy
{
    /// <summary>
    /// 
    /// </summary>
    Id,
    /// <summary>
    /// 
    /// </summary>
    Name,
    /// <summary>
    /// 
    /// </summary>
    Price
}

/// <summary>
/// OrderBy pour Users
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UsersOrderBy>))]
public enum UsersOrderBy
{
    /// <summary>
    /// 
    /// </summary>
    Id,
    /// <summary>
    /// 
    /// </summary>
    UserName,
    /// <summary>
    /// 
    /// </summary>
    Email,
    /// <summary>
    /// 
    /// </summary>
    FollowersCount,
    /// <summary>
    /// 
    /// </summary>
    FollowingCount
}

/// <summary>
/// OrderBy pour Orders
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrdersOrderBy>))]
public enum OrdersOrderBy
{
    /// <summary>
    /// 
    /// </summary>
    Id,
    /// <summary>
    /// 
    /// </summary>
    UserId,
    /// <summary>
    /// 
    /// </summary>
    ArticleId,
    /// <summary>
    /// 
    /// </summary>
    Quantity,
    /// <summary>
    /// 
    /// </summary>
    TotalPrice
}

/// <summary>
/// Direction de tri
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrderDirection>))]
public enum OrderDirection
{
    /// <summary>
    /// 
    /// </summary>
    Ascending,
    /// <summary>
    /// 
    /// </summary>
    Descending
}
