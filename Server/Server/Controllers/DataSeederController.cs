using Microsoft.AspNetCore.Mvc;
using Server.Models.Dtos;
using Server.Models.Queries.Enums;
using Server.Services;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DataSeederController : ControllerBase
{
    private readonly PostgresDbService _pgService;
    private readonly Neo4jDbService _neo4jService;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pgService"></param>
    /// <param name="neo4jService"></param>
    public DataSeederController(PostgresDbService pgService, Neo4jDbService neo4jService)
    {
        _pgService = pgService;
        _neo4jService = neo4jService;
    }

    /// <summary>
    /// Bulk Articles → Postgres/Neo4j/Both
    /// </summary>
    [HttpPost("articles")]
    public async Task<IActionResult> BulkImportArticles([FromBody] List<ArticleDto> articles, [FromQuery] Database targets = Database.Both)
    {
        var results = new List<string>();

        if (targets == Database.Postgres || targets == Database.Both)
        {
            await _pgService.BulkImportArticles(articles);
            results.Add("Postgres: Articles imported");
        }

        if (targets == Database.Neo4j || targets == Database.Both)
        {
            await _neo4jService.BulkImportArticles(articles);
            results.Add($"Neo4j: Articles imported");
        }

        return Ok(new { Message = string.Join(", ", results) });
    }

    /// <summary>
    /// Bulk Users → Postgres/Neo4j/Both  
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> BulkImportUsers([FromBody] List<UserDto> users, [FromQuery] Database targets = Database.Both)
    {
        var results = new List<string>();

        if (targets == Database.Postgres || targets == Database.Both)
        {
            await _pgService.BulkImportUsers(users);
            results.Add("Postgres: Users imported");
        }

        if (targets == Database.Neo4j || targets == Database.Both)
        {
            await _neo4jService.BulkImportUsers(users);
            results.Add("Neo4j: Users imported");
        }

        return Ok(new { Message = string.Join(", ", results) });
    }

    /// <summary>
    /// Bulk Orders → Postgres/Neo4j/Both
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> BulkImportOrders([FromBody] List<OrderDto> orders, [FromQuery] Database targets = Database.Both)
    {
        var results = new List<string>();

        if (targets == Database.Postgres || targets == Database.Both)
        {
            await _pgService.BulkImportOrders(orders);
            results.Add("Postgres: Orders imported");
        }

        if (targets == Database.Neo4j || targets == Database.Both)
        {
            await _neo4jService.BulkImportOrders(orders);
            results.Add("Neo4j: Orders imported");
        }

        return Ok(new { Message = string.Join(", ", results) });
    }

    /// <summary>
    /// Bulk Social Graph → Postgres/Neo4j/Both
    /// </summary>
    [HttpPost("social-graph")]
    public async Task<IActionResult> BulkImportSocialGraph([FromBody] List<FollowDto> follows, [FromQuery] Database targets = Database.Both)
    {
        var results = new List<string>();

        if (targets == Database.Postgres || targets == Database.Both)
        {
            await _pgService.BulkImportSocialGraph(follows);
            results.Add("Postgres: Social graph imported");
        }

        if (targets == Database.Neo4j || targets == Database.Both)
        {
            await _neo4jService.BulkImportSocialGraph(follows);
            results.Add("Neo4j: Social graph imported");
        }

        return Ok(new { Message = string.Join(", ", results) });
    }

    /// <summary>
    /// Full setup → 4000 users/articles/orders + social graph
    /// </summary>
    [HttpPost("full-setup")]
    public async Task<IActionResult> FullBulkSetup([FromBody] SetupDto setup, [FromQuery] Database targets = Database.Both)
    {
        var results = new List<string>();

        // Articles
        await BulkImportArticles(setup.Articles, targets);
        results.Add("Articles OK");

        // Users  
        await BulkImportUsers(setup.Users, targets);
        results.Add("Users OK");

        // Social Graph
        await BulkImportSocialGraph(setup.Follows, targets);
        results.Add("Social graph OK");

        // Orders
        await BulkImportOrders(setup.Orders, targets);
        results.Add("Orders OK");

        return Ok(new
        {
            Message = $"Full setup completed: {string.Join(", ", results)}",
            Targets = targets.ToString()
        });
    }
}
