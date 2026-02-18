using BuyAndRent.Shared.Dtos.DataStorageService;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Domains;
using Server.Models.Dtos;
using Server.Models.Dtos.Enums;

namespace Server.Services;

/// <summary>
/// Interface for postgres business service
/// </summary>
public interface IPostgresDbService
{
    /// <summary>
    /// Search specific artickes
    /// </summary>
    Task<PaginatedResult<Article>> SearchArticlesAsync(ArticleSearchRequest request);
}

/// <summary>
/// Implementation of user business service
/// </summary>
public class PostgresDbService : IPostgresDbService
{
    private readonly PostgresDbContext _dbContext;
    private readonly ILogger<PostgresDbService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    public PostgresDbService(
        PostgresDbContext dbContext,
        ILogger<PostgresDbService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<PaginatedResult<Article>> SearchArticlesAsync(ArticleSearchRequest request)
    {
        var query = _dbContext.Articles.AsQueryable();

        if (request.ArticleIds != null && request.ArticleIds.Any())
        {
            query = query.Where(a => request.ArticleIds.Contains(a.Id));
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.Orders.Any(o => o.UserId == request.UserId));
            if (request.OnlyFromFollowing)
            {
                query = query.Where(a => a.Orders.Any(o =>
                    _dbContext.Users.Any(u => u.Id == request.UserId && u.Following.Any(f => f.Id == o.UserId))));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{request.SearchTerm}%"));
        }

        if (request.MinPrice.HasValue) query = query.Where(a => a.Price >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue) query = query.Where(a => a.Price <= request.MaxPrice.Value);

        if (request.IncludeSellerDetails)
        {
            query = query.Include(a => a.Orders).ThenInclude(o => o.User);
        }

        var totalCount = await query.CountAsync();

        query = ApplySorting(query, request);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var pagedArticles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Article>
        {
            Items = pagedArticles,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private IQueryable<Article> ApplySorting(IQueryable<Article> query, ArticleSearchRequest request)
    {
        var isDesc = request.IsDescending;

        return request.OrderBy switch
        {
            ArticleFilterOrderBy.Name =>
                isDesc ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),

            ArticleFilterOrderBy.Price =>
                isDesc ? query.OrderByDescending(a => a.Price) : query.OrderBy(a => a.Price),

            ArticleFilterOrderBy.MostSold =>
                isDesc ? query.OrderByDescending(a => a.Orders.Count) : query.OrderBy(a => a.Orders.Count),

            ArticleFilterOrderBy.SellerPopularity =>
                isDesc ? query.OrderByDescending(a => a.Orders.Select(o => o.User.Followers.Count).DefaultIfEmpty(0).Max())
                       : query.OrderBy(a => a.Orders.Select(o => o.User.Followers.Count).DefaultIfEmpty(0).Max()),

            _ => query.OrderByDescending(a => a.Id)
        };
    }
}
