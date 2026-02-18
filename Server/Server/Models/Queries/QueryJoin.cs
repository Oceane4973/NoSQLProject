using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Models.Requests.Enums;

namespace Server.Models.Requests;

/// <summary>
/// 
/// </summary>
public record QueryJoin
{
    /// <summary>
    /// 
    /// </summary>
    public Entity FromEntity { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public object? FromField { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public Entity ToEntity { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public object? ToField { get; init; }
}