using DocumentManager.API.Data;
using DocumentManager.API.Domain.Entities;

namespace DocumentManager.API.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    public IGenericRepository<Folder> Folders { get; }
    public IGenericRepository<FileEntity> Files { get; }

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        Folders = new GenericRepository<Folder>(db);
        Files = new GenericRepository<FileEntity>(db);
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}