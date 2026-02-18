using System.Text.Json.Serialization;

namespace Server.Models.Requests.Enums;

/// <summary>
/// 
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterOperator>))]
public enum FilterOperator
{
    /// <summary>
    /// 
    /// </summary>
    Equals,
    /// <summary>
    /// 
    /// </summary>
    GreaterThan,
    /// <summary>
    /// 
    /// </summary>
    LessThan,
    /// <summary>
    /// 
    /// </summary>
    GreaterThanOrEqual,
    /// <summary>
    /// 
    /// </summary>
    LessThanOrEqual,
    /// <summary>
    /// 
    /// </summary>
    Like,
    /// <summary>
    /// 
    /// </summary>
    In
}