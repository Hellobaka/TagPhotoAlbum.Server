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
    public DbSet<Passkey> Passkeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Photo entity
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(p => p.Id);
            modelBuilder.Entity<PhotoTag>()
                .HasKey(pt => new { pt.PhotoId, pt.TagId });

            // ���� Photo �� PhotoTag ֮��Ĺ�ϵ
            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Photo)
                .WithMany(p => p.Tags)
                .HasForeignKey(pt => pt.PhotoId);

            // ���� Tag �� PhotoTag ֮��Ĺ�ϵ
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

        // Configure Passkey entity
        modelBuilder.Entity<Passkey>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.CredentialId).IsUnique();

            // Configure relationship with User
            entity.HasOne(p => p.User)
                .WithMany(u => u.Passkeys)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}