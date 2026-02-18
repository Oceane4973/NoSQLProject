using System.Text.Json.Serialization;

namespace Server.Models.Requests.Enums;

/// <summary>
/// 
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Entity>))]
public enum Entity
{
    /// <summary>
    /// 
    /// </summary>
    Articles,

    /// <summary>
    /// 
    /// </summary>
    Users,

    /// <summary>
    /// 
    /// </summary>
    Orders,
}