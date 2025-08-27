using DocumentManager.API.Domain.Entities;

namespace DocumentManager.API.Infrastructure.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IGenericRepository<Folder> Folders { get; }
    IGenericRepository<FileEntity> Files { get; }
    Task<int> SaveChangesAsync();
}