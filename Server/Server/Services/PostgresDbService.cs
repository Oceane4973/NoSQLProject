using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Domains;
using Server.Models.Dtos;
using Server.Models.Requests;
using Server.Models.Requests.Enums;
using Server.Models.Requests.Enums.Fields;
using Server.Models.Responses;
using System.Diagnostics;

namespace Server.Services;

/// <summary>
/// Implementation of user business service
/// </summary>
public class PostgresDbService : IDbService
{
    private readonly PostgresDbContext _context;
    private readonly ILogger<PostgresDbService> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    public PostgresDbService(PostgresDbContext dbContext, ILogger<PostgresDbService> logger)
    {
        _context = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Execute QueryBuilder par Entity (switch complet)
    /// </summary>
    public async Task<PaginatedResult<dynamic>> ExecuteQueryAsync(QueryBuilderRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Executing QueryBuilder: Entity={Entity}", request.Entity);

        try
        {
            var result = request.Entity switch
            {
                Entity.Articles => await ExecuteArticlesQuery(request),
                Entity.Users => await ExecuteUsersQuery(request),
                Entity.Orders => await ExecuteOrdersQuery(request),
                _ => throw new ArgumentException($"Entity {request.Entity} not supported")
            };

            stopwatch.Stop();
            result.RequestTimeInMilliseconds = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("QueryBuilder OK: Entity={Entity}, Time={Time}ms, Count={Count}",
                request.Entity, result.RequestTimeInMilliseconds, result.TotalCount);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "QueryBuilder ERROR: Entity={Entity}, Time={Time}ms", request.Entity, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task<PaginatedResult<dynamic>> ExecuteArticlesQuery(QueryBuilderRequest request)
    {
        var query = _context.Articles.AsQueryable();

        // FollowingLevel
        if (request.UserId.HasValue && request.FollowingLevel.HasValue && request.FollowingLevel > 0)
        {
            var reachableUsers = await GetReachableUsersAsync(request.UserId.Value, request.FollowingLevel.Value);
            query = query.Where(a => a.Orders.Any(o => reachableUsers.Contains(o.UserId)));
        }

        // ArticlesFields
        foreach (var filter in request.Filters)
        {
            query = ApplyArticlesFilter(query, (ArticlesFields)filter.FieldId);
        }

        if (request.OrderByField is ArticlesOrderBy orderBy)
        {
            query = ApplyArticlesOrderBy(query, orderBy, request.OrderDirection);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new { a.Id, a.Name, a.Price })
            .Cast<dynamic>()
            .ToListAsync();

        return new PaginatedResult<dynamic>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task<PaginatedResult<dynamic>> ExecuteUsersQuery(QueryBuilderRequest request)
    {
        var query = _context.Users
            .Include(u => u.Followers)
            .Include(u => u.Following)
            .AsQueryable();

        if (request.UserId.HasValue && request.FollowingLevel.HasValue && request.FollowingLevel > 0)
        {
            var reachableUsers = await GetReachableUsersAsync(request.UserId.Value, request.FollowingLevel.Value);
            query = query.Where(u => reachableUsers.Contains(u.Id));
        }

        foreach (var filter in request.Filters)
        {
            query = ApplyUsersFilter(query, (UsersFields)filter.FieldId);
        }

        if (request.OrderByField is UsersOrderBy orderBy)
        {
            query = ApplyUsersOrderBy(query, orderBy, request.OrderDirection);

        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                FollowersCount = u.Followers.Count,
                FollowingCount = u.Following.Count
            })
            .Cast<dynamic>()
            .ToListAsync();

        return new PaginatedResult<dynamic>
        {
            Items = items.Cast<dynamic>().ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task<PaginatedResult<dynamic>> ExecuteOrdersQuery(QueryBuilderRequest request)
    {
        var query = _context.Orders
            .Include(o => o.Article)
            .Include(o => o.User)
            .AsQueryable();

        if (request.UserId.HasValue && request.FollowingLevel.HasValue && request.FollowingLevel > 0)
        {
            var reachableUsers = await GetReachableUsersAsync(request.UserId.Value, request.FollowingLevel.Value);
            query = query.Where(o => reachableUsers.Contains(o.UserId));
        }

        foreach (var filter in request.Filters)
        {
            query = ApplyOrdersFilter(query, (OrdersFields)filter.FieldId);
        }

        if (request.OrderByField is OrdersOrderBy orderBy)
        {
            query = ApplyOrdersOrderBy(query, orderBy, request.OrderDirection);

        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                o.ArticleId,
                o.Quantity,
                o.TotalPrice
            })
            .ToListAsync();

        return new PaginatedResult<dynamic>
        {
            Items = items.Cast<dynamic>().ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="levels"></param>
    /// <returns></returns>
    private async Task<HashSet<Guid>> GetReachableUsersAsync(Guid userId, int levels)
    {
        var currentLevelIds = new HashSet<Guid> { userId };
        var allReachableUserIds = new HashSet<Guid> { userId };

        for (int i = 0; i < levels; i++)
        {
            var nextLevelIds = await _context.UserFollows
                .Where(uf => currentLevelIds.Contains(uf.FollowerId))
                .Select(uf => uf.FollowingId)
                .Distinct()
                .ToListAsync();

            var newIds = nextLevelIds.Except(allReachableUserIds);
            if (!newIds.Any()) break;

            foreach (var id in newIds) allReachableUserIds.Add(id);
            currentLevelIds = new HashSet<Guid>(newIds);
        }

        return allReachableUserIds;
    }

    private static IQueryable<Article> ApplyArticlesFilter(IQueryable<Article> query, ArticlesFields field)
    {
        return field switch
        {
            ArticlesFields.Price => query.Where(a => a.Price > 0),
            ArticlesFields.Name => query.Where(a => !string.IsNullOrEmpty(a.Name)),
            _ => query
        };
    }

    private static IQueryable<User> ApplyUsersFilter(IQueryable<User> query, UsersFields field)
    {
        return field switch
        {
            UsersFields.FollowersCount => query.Where(u => u.Followers.Any()),
            _ => query
        };
    }

    private static IQueryable<Order> ApplyOrdersFilter(IQueryable<Order> query, OrdersFields field)
    {
        return field switch
        {
            OrdersFields.TotalPrice => query.Where(o => o.TotalPrice > 0),
            _ => query
        };
    }

    private IQueryable<Article> ApplyArticlesOrderBy(IQueryable<Article> query, ArticlesOrderBy field, OrderDirection direction)
    {
        return (field, direction) switch
        {
            (ArticlesOrderBy.Id, OrderDirection.Ascending) => query.OrderBy(a => a.Id),
            (ArticlesOrderBy.Id, OrderDirection.Descending) => query.OrderByDescending(a => a.Id),
            (ArticlesOrderBy.Name, OrderDirection.Ascending) => query.OrderBy(a => a.Name),
            (ArticlesOrderBy.Name, OrderDirection.Descending) => query.OrderByDescending(a => a.Name),
            (ArticlesOrderBy.Price, OrderDirection.Ascending) => query.OrderBy(a => a.Price),
            (ArticlesOrderBy.Price, OrderDirection.Descending) => query.OrderByDescending(a => a.Price),
            _ => query.OrderByDescending(a => a.Id) // Default
        };
    }

    private IQueryable<User> ApplyUsersOrderBy(IQueryable<User> query, UsersOrderBy field, OrderDirection direction)
    {
        return (field, direction) switch
        {
            (UsersOrderBy.Id, OrderDirection.Ascending) => query.OrderBy(u => u.Id),
            (UsersOrderBy.Id, OrderDirection.Descending) => query.OrderByDescending(u => u.Id),
            (UsersOrderBy.UserName, OrderDirection.Ascending) => query.OrderBy(u => u.Name),
            (UsersOrderBy.FollowersCount, OrderDirection.Descending) => query.OrderByDescending(u => u.Followers.Count),
            _ => query.OrderByDescending(u => u.Id)
        };
    }

    private IQueryable<Order> ApplyOrdersOrderBy(IQueryable<Order> query, OrdersOrderBy field, OrderDirection direction)
    {
        return (field, direction) switch
        {
            (OrdersOrderBy.Id, OrderDirection.Ascending) => query.OrderBy(o => o.Id),
            (OrdersOrderBy.Id, OrderDirection.Descending) => query.OrderByDescending(o => o.Id),
            (OrdersOrderBy.Quantity, OrderDirection.Ascending) => query.OrderBy(o => o.Quantity),
            (OrdersOrderBy.Quantity, OrderDirection.Descending) => query.OrderByDescending(o => o.Quantity),
            (OrdersOrderBy.TotalPrice, OrderDirection.Descending) => query.OrderByDescending(o => o.TotalPrice),
            _ => query.OrderByDescending(o => o.Id)
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="articles"></param>
    /// <returns></returns>
    public async Task BulkImportArticles([FromBody] List<ArticleDto> articles)
    {
        var entities = articles.Select(a => new Article
        {
            Id = a.Id,
            Name = a.Name,
            Price = a.Price
        }).ToList();

        await _context.BulkInsertAsync(entities);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="users"></param>
    /// <returns></returns>
    public async Task BulkImportUsers([FromBody] List<UserDto> users)
    {
        var entities = users.Select(u => new User
        {
            Id = u.Id,
            Name = u.UserName,
            Email = u.Email
        }).ToList();

        await _context.BulkInsertAsync(entities);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orders"></param>
    /// <returns></returns>
    public async Task BulkImportOrders([FromBody] List<OrderDto> orders)
    {
        var entities = orders.Select(o => new Order
        {
            Id = o.Id,
            UserId = o.UserId,
            ArticleId = o.ArticleId,
            Quantity = o.Quantity
        }).ToList();

        await _context.BulkInsertAsync(entities);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="follows"></param>
    /// <returns></returns>
    public async Task BulkImportSocialGraph([FromBody] List<FollowDto> follows)
    {
        var entities = follows
            .GroupBy(f => new { f.FollowerId, f.FollowingId })
            .Select(g => new UserFollow
            {
                FollowerId = g.Key.FollowerId,
                FollowingId = g.Key.FollowingId
            })
            .ToList();

        await _context.BulkInsertAsync(entities, b => b.IncludeGraph = false);
    }

}