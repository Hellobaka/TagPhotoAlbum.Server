using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TagPhotoAlbum.Server.Models;

namespace TagPhotoAlbum.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Photo> Photos { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PhotoTag> PhotoTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Photo entity
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(p => p.Id);
            modelBuilder.Entity<PhotoTag>()
            .HasKey(pt => new { pt.PhotoId, pt.TagId });

            // 配置 Photo 和 PhotoTag 之间的关系
            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Photo)
                .WithMany(p => p.Tags)
                .HasForeignKey(pt => pt.PhotoId);

            // 配置 Tag 和 PhotoTag 之间的关系
            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PhotoTags)
                .HasForeignKey(pt => pt.TagId);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}