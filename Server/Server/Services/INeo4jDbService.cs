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

        return await session.ExecuteReadAsync(async tx =>
        {
            var cypher = BuildCypherForEntity(request);

            var skip = (request.Page - 1) * request.PageSize;
            var limit = request.PageSize;
            var paginatedCypher = $"{cypher} SKIP {skip} LIMIT {limit}";

            _logger.LogInformation("Neo4j Paginated Query: {Cypher}", paginatedCypher);

            var dataResult = await tx.RunAsync(paginatedCypher);
            var items = new List<object>();

            await foreach (var record in dataResult)
            {
                if (record.Values.Count == 1 && record.Values.First().Value is INode node)
                    items.Add(node.Properties);
                else
                    items.Add(record.Values.ToDictionary(kv => kv.Key, kv => kv.Value));
            }

            var queryPart = cypher.Split(new[] { "RETURN" }, StringSplitOptions.None)[0];
            string aliasToCount = request.Entity == Entity.Orders ? "r" : "target";

            var countCypher = $"{queryPart} RETURN count(DISTINCT {aliasToCount}) as total";
            var countResult = await tx.RunAsync(countCypher);
            var countRecords = await countResult.ToListAsync();

            int totalCount = countRecords.Any() ? (int)countRecords[0]["total"].As<long>() : 0;

            stopwatch.Stop();
            return new PaginatedResult<dynamic>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                RequestTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            };
        });
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
                Entity.Articles => $"(me:User {{id: '{request.UserId}'}})-[:FOLLOWS*1..{request.FollowingLevel}]->(friend:User)-[:BOUGHT]->(target:Article)",
                Entity.Orders => $"(me:User {{id: '{request.UserId}'}})-[:FOLLOWS*1..{request.FollowingLevel}]->(u:User)-[r:BOUGHT]->(a:Article)",
                _ => "(target)"
            };
        }
        else
        {
            matchClause = request.Entity switch
            {
                Entity.Users => "(target:User)",
                Entity.Articles => "(target:Article)",
                Entity.Orders => "(u:User)-[r:BOUGHT]->(a:Article)",
                _ => "(target)"
            };
        }

        returnClause = request.Entity switch
        {
            Entity.Users => "RETURN target { .*, followingCount: COUNT { (target)-[:FOLLOWS]->() }, followersCount: COUNT { (target)<-[:FOLLOWS]-() } } as user",
            Entity.Articles => "RETURN DISTINCT target",
            Entity.Orders => "RETURN id(r) as id, u.id as userId, a.id as articleId, r.quantity as quantity, r.totalPrice as totalPrice",
            _ => "RETURN target"
        };

        var whereParts = new List<string>();
        foreach (var filter in request.Filters)
        {
            string fieldName = request.Entity switch
            {
                Entity.Articles => ((ArticlesFields)filter.FieldId).ToString(),
                Entity.Users => ((UsersFields)filter.FieldId).ToString(),
                Entity.Orders => ((OrdersFields)filter.FieldId).ToString(),
                _ => "Id"
            };

            string alias = request.Entity == Entity.Orders ?
                (fieldName == "UserId" ? "u" : (fieldName == "ArticleId" ? "a" : "r")) : "target";

            string propertyName = fieldName switch
            {
                "UserId" or "ArticleId" or "Id" => "id",
                "UserName" => "userName",
                "FollowersCount" => "followersCount",
                "FollowingCount" => "followingCount",
                _ => char.ToLower(fieldName[0]) + fieldName.Substring(1)
            };

            if (filter.Operator == FilterOperator.Equals)
            {
                var val = filter.Value?.ToString();
                whereParts.Add($"{alias}.{propertyName} = '{val}'");
            }
        }

        var whereClause = whereParts.Any() ? "WHERE " + string.Join(" AND ", whereParts) : "";

        return $"MATCH {matchClause} {whereClause} {returnClause}";
    }
}