using System.Text.Json.Serialization;

namespace Server.Models.Queries.Enums
{
    /// <summary>
    /// 
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<Database>))]
    public enum Database
    {
        /// <summary>
        /// 
        /// </summary>
        Postgres,

        /// <summary>
        /// 
        /// </summary>
        Neo4j,

        /// <summary>
        /// 
        /// </summary>
        Both,
    }
}
