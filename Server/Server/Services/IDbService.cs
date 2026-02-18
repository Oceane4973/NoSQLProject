using Microsoft.AspNetCore.Mvc;
using Server.Models.Dtos;
using Server.Models.Requests;
using Server.Models.Responses;

namespace Server.Services
{

    /// <summary>
    /// Interface for database service
    /// </summary>
    public interface IDbService
    {
        /// <summary>
        /// Execute complex query via QueryBuilder
        /// </summary>
        Task<PaginatedResult<dynamic>> ExecuteQueryAsync(QueryBuilderRequest request);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="articles"></param>
        /// <returns></returns>
        Task BulkImportArticles([FromBody] List<ArticleDto> articles);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        Task BulkImportUsers([FromBody] List<UserDto> users);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        Task BulkImportOrders([FromBody] List<OrderDto> orders);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="follows"></param>
        /// <returns></returns>
        Task BulkImportSocialGraph([FromBody] List<FollowDto> follows);

    }
}
