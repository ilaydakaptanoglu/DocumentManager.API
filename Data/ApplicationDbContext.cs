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

    // Authentication DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Folder parent-child relationship
        modelBuilder.Entity<Folder>()
            .HasOne(f => f.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(f => f.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // File-folder relationship
        modelBuilder.Entity<FileEntity>()
            .HasOne(f => f.Folder)
            .WithMany(p => p.Files)
            .HasForeignKey(f => f.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        // File-User relationship
        modelBuilder.Entity<FileEntity>()
            .HasOne(f => f.User)
            .WithMany(u => u.Files)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Folder-User relationship
        modelBuilder.Entity<Folder>()
            .HasOne(f => f.User)
            .WithMany(u => u.Folders)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-many User-Role relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity<Dictionary<string, object>>(
                "UserRole",
                j => j.HasOne<Role>().WithMany().HasForeignKey("RoleId").HasConstraintName("FK_UserRole_Role").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<User>().WithMany().HasForeignKey("UserId").HasConstraintName("FK_UserRole_User").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("UserId", "RoleId");
                    j.HasIndex("RoleId");
                    j.ToTable("UserRoles");
                });

        // Global query filters for soft delete
        modelBuilder.Entity<FileEntity>().HasQueryFilter(f => !f.IsDeleted);
        modelBuilder.Entity<Folder>().HasQueryFilter(f => !f.IsDeleted);

        // Unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // Seed data for roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Full access to all resources" },
            new Role { Id = 2, Name = "User", Description = "Limited access to own resources only" }
        );

        base.OnModelCreating(modelBuilder);
    }
}
