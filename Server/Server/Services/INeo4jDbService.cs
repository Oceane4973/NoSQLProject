using Neo4j.Driver;
using Server.Models.Dtos;
using Server.Models.Requests;
using Server.Models.Responses;
using Server.Models.Requests.Enums;
using System.Diagnostics;
using System.Linq;

namespace Server.Services;

/// <summary>
/// 
/// </summary>
public class Neo4jDbService : IDbService
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jDbService> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="logger"></param>
    public Neo4jDbService(IDriver driver, ILogger<Neo4jDbService> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<PaginatedResult<dynamic>> ExecuteQueryAsync(QueryBuilderRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        await using var session = _driver.AsyncSession();
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cypher = BuildCypherForEntity(request);

            var countCypher = $"WITH {cypher.Replace("RETURN", "").Replace("SKIP", "").Replace("LIMIT", "")} RETURN count(*) as total";
            var countResult = await tx.RunAsync(countCypher);
            var totalRecord = await countResult.SingleAsync();
            var totalCount = totalRecord["total"].As<int>();

            var dataCypher = $"{cypher} SKIP {(request.Page - 1) * request.PageSize} LIMIT {request.PageSize}";
            var dataResult = await tx.RunAsync(dataCypher);

            var items = new List<object>();
            await foreach (var record in dataResult)
            {
                items.Add(record.As<Dictionary<string, object>>());
            }

            return new PaginatedResult<dynamic>
            {
                Items = items.Cast<dynamic>().ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        });

        stopwatch.Stop();
        result.RequestTimeInMilliseconds = stopwatch.ElapsedMilliseconds;
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="articles"></param>
    /// <returns></returns>
    public async Task BulkImportArticles(List<ArticleDto> articles)
    {
        const int BATCH_SIZE = 1000;

        await using var session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

        for (int i = 0; i < articles.Count; i += BATCH_SIZE)
        {
            var batch = articles.Skip(i).Take(BATCH_SIZE).Select(a => new
            {
                id = a.Id.ToString(),
                name = a.Name ?? "",
                price = (double)a.Price
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    UNWIND $articles as article
                    MERGE (a:Article {id: article.id})
                    SET a.name = article.name, 
                        a.price = article.price",
                    new { articles = batch });
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="users"></param>
    /// <returns></returns>
    public async Task BulkImportUsers(List<UserDto> users)
    {
        const int BATCH_SIZE = 1000;

        await using var session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

        for (int i = 0; i < users.Count; i += BATCH_SIZE)
        {
            var batch = users.Skip(i).Take(BATCH_SIZE).Select(u => new
            {
                id = u.Id.ToString(),
                name = u.UserName ?? "",
                email = u.Email ?? ""
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    UNWIND $users as user
                    MERGE (u:User {id: user.id})
                    SET u.name = user.name, 
                        u.email = user.email",
                    new { users = batch });
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    public async Task BulkImportOrders(List<OrderDto> orders)
    {
        const int BATCH_SIZE = 500;

        await using var session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

        for (int i = 0; i < orders.Count; i += BATCH_SIZE)
        {
            var batch = orders.Skip(i).Take(BATCH_SIZE).Select(o => new
            {
                userId = o.UserId.ToString(),
                articleId = o.ArticleId.ToString(),
                quantity = o.Quantity,
                totalPrice = o.TotalPrice
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    UNWIND $orders as order
                    MATCH (u:User {id: order.userId})
                    MATCH (a:Article {id: order.articleId})
                    MERGE (u)-[r:BOUGHT]->(a)
                    SET r.quantity = order.quantity,
                        r.totalPrice = order.totalPrice",
                    new { orders = batch });
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="follows"></param>
    /// <returns></returns>
    public async Task BulkImportSocialGraph(List<FollowDto> follows)
    {
        const int BATCH_SIZE = 1000;

        await using var session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

        for (int i = 0; i < follows.Count; i += BATCH_SIZE)
        {
            var batch = follows.Skip(i).Take(BATCH_SIZE).Select(f => new
            {
                followerId = f.FollowerId.ToString(),
                followingId = f.FollowingId.ToString()
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    UNWIND $follows as follow
                    MATCH (follower:User {id: follow.followerId})
                    MATCH (following:User {id: follow.followingId})
                    MERGE (follower)-[:FOLLOWS]->(following)",
                    new { follows = batch });
            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private string BuildCypherForEntity(QueryBuilderRequest request)
    {
        return request.Entity switch
        {
            Entity.Users => "MATCH (u:User) RETURN u LIMIT 20",
            Entity.Articles => "MATCH (a:Article) RETURN a LIMIT 20",
            Entity.Orders => """
                MATCH (u:User)-[r:BOUGHT]->(a:Article)
                RETURN u.name as userName, r.quantity, r.totalPrice, a.name as articleName LIMIT 20
                """,
            _ => "RETURN 'Entity not supported' as message LIMIT 1"
        };
    }
}
