// Infrastructure/Repositories/IUnitOfWork.cs - UPDATED
using DocumentManager.API.Data;
using DocumentManager.API.Domain.Entities;

namespace DocumentManager.API.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<FileEntity> Files { get; }
    IGenericRepository<Folder> Folders { get; }
    // NEW: User and Role repositories
    IGenericRepository<User> Users { get; }
    IGenericRepository<Role> Roles { get; }

    Task<int> SaveChangesAsync();
    int SaveChanges();
}