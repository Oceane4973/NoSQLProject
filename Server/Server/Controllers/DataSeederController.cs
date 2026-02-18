using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Domains;
using EFCore.BulkExtensions;
using Server.Models.Dtos;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DataSeederController : ControllerBase
{
    private readonly PostgresDbContext _context;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public DataSeederController(PostgresDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="articles"></param>
    /// <returns></returns>
    [HttpPost("articles")]
    public async Task<IActionResult> BulkImportArticles([FromBody] List<ArticleDto> articles)
    {
        var entities = articles.Select(a => new Article
        {
            Id = a.Id,
            Name = a.Name,
            Price = a.Price
        }).ToList();

        await _context.BulkInsertAsync(entities);
        return Ok(new { Count = entities.Count, Message = "Articles imported" });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="users"></param>
    /// <returns></returns>
    [HttpPost("users")]
    public async Task<IActionResult> BulkImportUsers([FromBody] List<UserDto> users)
    {
        var entities = users.Select(u => new User
        {
            Id = u.Id,
            Name = u.UserName,
            Email = u.Email
        }).ToList();

        await _context.BulkInsertAsync(entities);
        return Ok(new { Count = entities.Count, Message = "Users imported" });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    [HttpPost("orders")]
    public async Task<IActionResult> BulkImportOrders([FromBody] List<OrderDto> orders)
    {
        var entities = orders.Select(o => new Order
        {
            Id = o.Id,
            UserId = o.UserId,
            ArticleId = o.ArticleId,
            Quantity = o.Quantity
        }).ToList();

        await _context.BulkInsertAsync(entities);
        return Ok(new { Count = entities.Count, Message = "Orders imported" });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="follows"></param>
    /// <returns></returns>
    [HttpPost("social-graph")]
    public async Task<IActionResult> BulkImportSocialGraph([FromBody] List<FollowDto> follows)
    {
        var userIds = follows.SelectMany(f => new[] { f.FollowerId, f.FollowingId }).Distinct().ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Include(u => u.Followers)
            .Include(u => u.Following)
            .ToListAsync();

        foreach (var follow in follows)
        {
            var follower = users.First(u => u.Id == follow.FollowerId);
            var following = users.First(u => u.Id == follow.FollowingId);

            if (!follower.Following.Contains(following))
                follower.Following.Add(following);

            if (!following.Followers.Contains(follower))
                following.Followers.Add(follower);
        }

        await _context.BulkSaveChangesAsync();
        return Ok(new { Count = follows.Count, Message = "Social graph imported" });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setup"></param>
    /// <returns></returns>
    [HttpPost("full-setup")]
    public async Task<IActionResult> FullBulkSetup([FromBody] SetupDto setup)
    {
        await BulkImportArticles(setup.Articles);
        await BulkImportUsers(setup.Users);
        await BulkImportSocialGraph(setup.Follows);
        await BulkImportOrders(setup.Orders);

        return Ok(new { Message = "Full setup completed" });
    }
}