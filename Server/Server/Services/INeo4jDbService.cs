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

            var parts = cypher.Split(new[] { "RETURN" }, StringSplitOptions.None);
            var matchPart = parts[0];

            var countCypher = $"{matchPart} RETURN count(*) as total";
            var countResult = await tx.RunAsync(countCypher);
            var totalRecord = await countResult.SingleAsync();
            var totalCount = totalRecord["total"].As<int>();

            var cleanDataCypher = cypher.Split("LIMIT")[0].Split("SKIP")[0];
            var dataCypher = $"{cleanDataCypher} SKIP {(request.Page - 1) * request.PageSize} LIMIT {request.PageSize}";

            var dataResult = await tx.RunAsync(dataCypher);

            var items = new List<object>();
            await foreach (var record in dataResult)
            {
                var values = record.Values.FirstOrDefault().Value;

                if (values is INode node)
                {
                    items.Add(node.Properties);
                }
                else
                {
                    items.Add(record.Values);
                }
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
                var result = await tx.RunAsync(@"
                    UNWIND $follows as follow
                    MATCH (follower:User {id: follow.followerId})
                    MATCH (following:User {id: follow.followingId})
                    MERGE (follower)-[:FOLLOWS]->(following)",
                    new { follows = batch });

                var summary = await result.ConsumeAsync();
                _logger.LogInformation($"FOLLOWS batch processed. Relationships created: {summary.Counters.RelationshipsCreated}");
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
        string matchClause;
        string returnClause;

        if (request.UserId.HasValue && request.FollowingLevel.HasValue && request.FollowingLevel > 0)
        {
            matchClause = request.Entity switch
            {
                Entity.Users => $"(me:User {{id: '{request.UserId}'}})-[:FOLLOWS*1..{request.FollowingLevel}]->(target:User)",
                Entity.Articles => $"(me:User {{id: '{request.UserId}'}})-[:FOLLOWS*1..{request.FollowingLevel}]->(friend)-[:BOUGHT]->(target:Article)",
                Entity.Orders => $"(me:User {{id: '{request.UserId}'}})-[:FOLLOWS*1..{request.FollowingLevel}]->(u:User)-[r:BOUGHT]->(a:Article)",
                _ => "n"
            };
        }
        else
        {
            matchClause = request.Entity switch
            {
                Entity.Users => "(target:User)",
                Entity.Articles => "(target:Article)",
                Entity.Orders => "(u:User)-[r:BOUGHT]->(a:Article)",
                _ => "n"
            };
        }

        returnClause = request.Entity switch
        {
            Entity.Users => "RETURN target",
            Entity.Articles => "RETURN target",
            Entity.Orders => "RETURN { id: id(r), userId: u.id, articleId: a.id, quantity: r.quantity, totalPrice: r.totalPrice }",
            _ => "RETURN n"
        };

        var whereClause = "";
        if (request.Filters.Any())
        {
            var idFilter = request.Filters.FirstOrDefault(f => f.FieldId == 0);
            if (idFilter != null)
            {
                whereClause = $"WHERE target.id = '{idFilter.Value}' OR u.id = '{idFilter.Value}'";
            }
        }

        return $"MATCH {matchClause} {whereClause} {returnClause}";
    }
}
