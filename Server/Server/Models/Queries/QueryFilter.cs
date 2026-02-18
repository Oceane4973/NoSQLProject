using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Models.Requests.Enums;

namespace Server.Models.Requests;

/// <summary>
/// 
/// </summary>
public record QueryFilter
{
    /// <summary>
    /// 
    /// </summary>
    public int FieldId { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public FilterOperator Operator { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public object? Value { get; init; }
}