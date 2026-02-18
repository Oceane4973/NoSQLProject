using System.Text.Json.Serialization;

namespace Server.Models.Requests.Enums;

/// <summary>
/// 
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ArticlesFields>))]
public enum ArticlesFields
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
/// 
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UsersFields>))]
public enum UsersFields
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
/// 
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrdersFields>))]
public enum OrdersFields
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
