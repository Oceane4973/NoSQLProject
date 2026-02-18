using Microsoft.AspNetCore.Mvc;
using Server.Models.Domains;
using Server.Models.Requests;
using Server.Models.Responses;
using Server.Services;

namespace Server.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Route("api/social-network")]
    [Produces("application/json")]
    public class SocialNetworkController : ControllerBase
    {
        private readonly IPostgresDbService _postgresDbService;

        private readonly ILogger<SocialNetworkController> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postgresDbService"></param>
        /// <param name="logger"></param>
        public SocialNetworkController(IPostgresDbService postgresDbService, ILogger<SocialNetworkController> logger)
        {
            _postgresDbService = postgresDbService;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("search/articles")]
        [ProducesResponseType(typeof(PaginatedResult<Article>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchArticles([FromQuery] ArticleSearchRequest request)
        {
            var results = await _postgresDbService.SearchArticlesAsync(request);

            return Ok(results);
        }
    }
}