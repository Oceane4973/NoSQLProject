using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Models.Requests.Enums;
using Server.Models.Requests.Enums.Fields;
using System.Text.Json.Serialization;

namespace Server.Models.Requests;

/// <summary>
/// 
/// </summary>
public record QueryBuilderRequest
{
    /// <summary>
    /// 
    /// </summary>
    public Entity Entity { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public List<QueryFilter> Filters { get; init; } = new();
    /// <summary>
    /// 
    /// </summary>
    public List<QueryJoin> Joins { get; init; } = new();
    /// <summary>
    /// 
    /// </summary>
    public int? FollowingLevel { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public Guid? UserId { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public string? SelectFields { get; init; }
    /// <summary>
    /// 
    /// </summary>

    public object? OrderByField { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public OrderDirection OrderDirection { get; init; } = OrderDirection.Descending;
    /// <summary>
    /// 
    /// </summary>

    public int Page { get; init; } = 1;
    /// <summary>
    /// 
    /// </summary>
    public int PageSize { get; init; } = 20;
}
