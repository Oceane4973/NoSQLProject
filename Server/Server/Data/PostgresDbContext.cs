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
    /// Users DbSet
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// UserFollows DbSet
    /// </summary>
    public DbSet<UserFollow> UserFollows { get; set; }

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

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.ToTable("UserFollows");
            entity.HasKey(f => new { f.FollowerId, f.FollowingId });

            entity.HasOne(f => f.Follower)
                  .WithMany(u => u.Following)
                  .HasForeignKey(f => f.FollowerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Following)
                  .WithMany(u => u.Followers)
                  .HasForeignKey(f => f.FollowingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId);

            entity.HasOne(o => o.Article)
                  .WithMany(a => a.Orders)
                  .HasForeignKey(o => o.ArticleId);
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("Articles");
        });
    }
}