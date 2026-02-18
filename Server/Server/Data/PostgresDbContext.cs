using Microsoft.EntityFrameworkCore;
using Server.Models.Domains;

namespace Server.Data;

/// <summary>
/// 
/// </summary>
/// <param name="options"></param>
public class PostgresDbContext(DbContextOptions<PostgresDbContext> options) : DbContext(options)
{
    /// <summary>
    /// User DbSet
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Articles DbSet
    /// </summary>
    public DbSet<Article> Articles { get; set; }

    /// <summary>
    /// Orders DbSet
    /// </summary>
    public DbSet<Order> Orders { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Followers)
            .WithMany(u => u.Following)
            .UsingEntity<Dictionary<string, object>>(
                "UserFollows",
                j => j.HasOne<User>().WithMany().HasForeignKey("FollowerId"),
                j => j.HasOne<User>().WithMany().HasForeignKey("FollowingId")
            );

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId);

            entity.HasOne(o => o.Article)
                  .WithMany(a => a.Orders)
                  .HasForeignKey(o => o.ArticleId);
        });
    }
}