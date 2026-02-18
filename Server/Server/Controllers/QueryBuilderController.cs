using Microsoft.AspNetCore.Mvc;
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
    private readonly IPostgresDbService _service;

    /// <summary>
    /// 
    /// </summary>
    public QueryBuilderController(IPostgresDbService service)
    {
        _service = service;
    }

    /// <summary>
    /// 
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<PaginatedResult<dynamic>>> Execute([FromBody] QueryBuilderRequest request)
    {
        var result = await _service.ExecuteQueryAsync(request);
        return Ok(result);
    }
}
