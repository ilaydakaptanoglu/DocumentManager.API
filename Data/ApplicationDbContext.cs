using DocumentManager.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Existing DbSets
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<FileEntity> Files => Set<FileEntity>();

    // NEW: Authentication DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Existing folder parent-child relationship
        modelBuilder.Entity<Folder>()
            .HasOne(f => f.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(f => f.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Existing file-folder relationship
        modelBuilder.Entity<FileEntity>()
            .HasOne(f => f.Folder)
            .WithMany(p => p.Files)
            .HasForeignKey(f => f.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        // NEW: User-Role relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // NEW: File-User relationship
        modelBuilder.Entity<FileEntity>()
            .HasOne(f => f.User)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // NEW: Folder-User relationship
        modelBuilder.Entity<Folder>()
            .HasOne(f => f.User)
            .WithMany(u => u.Folders)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // NEW: Global query filters for soft delete
        modelBuilder.Entity<FileEntity>().HasQueryFilter(f => !f.IsDeleted);
        modelBuilder.Entity<Folder>().HasQueryFilter(f => !f.IsDeleted);

        // NEW: Unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // NEW: Seed data for roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Full access to all resources" },
            new Role { Id = 2, Name = "User", Description = "Limited access to own resources only" }
        );

        base.OnModelCreating(modelBuilder);
    }
}