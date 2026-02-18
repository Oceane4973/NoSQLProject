using Microsoft.AspNetCore.Mvc;
using Server.Models.Queries.Enums;
using Server.Models.Requests;
using Server.Models.Responses;
using Server.Services;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueryBuilderController : ControllerBase
{
    private readonly PostgresDbService _pgService;
    private readonly Neo4jDbService _neo4jService;

    /// <summary>
    /// 
    /// </summary>
    public QueryBuilderController(PostgresDbService pgService, Neo4jDbService neo4jService)
    {
        _pgService = pgService;
        _neo4jService = neo4jService;
    }

    /// <summary>
    /// 
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<List<PaginatedResult<dynamic>>>> Execute([FromBody] QueryBuilderRequest request, [FromQuery] Database targets = Database.Both)
    {
        var results = new List<PaginatedResult<dynamic>> ();

        if (targets == Database.Postgres || targets == Database.Both)
        {
            var pgResult = await _pgService.ExecuteQueryAsync(request);
            results.Add(pgResult);
        }

        if (targets == Database.Neo4j || targets == Database.Both)
        {
            var neoResult = await _neo4jService.ExecuteQueryAsync(request);
            results.Add(neoResult);
        }

        return Ok(results);
    }
}
