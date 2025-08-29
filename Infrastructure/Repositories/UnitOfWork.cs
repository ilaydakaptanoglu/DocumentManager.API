// Infrastructure/Repositories/UnitOfWork.cs - UPDATED
using DocumentManager.API.Data;
using DocumentManager.API.Domain.Entities;

namespace DocumentManager.API.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    // Existing repositories
    private IGenericRepository<FileEntity>? _files;
    private IGenericRepository<Folder>? _folders;

    // NEW: Auth repositories
    private IGenericRepository<User>? _users;
    private IGenericRepository<Role>? _roles;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Existing properties
    public IGenericRepository<FileEntity> Files =>
        _files ??= new GenericRepository<FileEntity>(_context);

    public IGenericRepository<Folder> Folders =>
        _folders ??= new GenericRepository<Folder>(_context);

    // NEW: Auth properties
    public IGenericRepository<User> Users =>
        _users ??= new GenericRepository<User>(_context);

    public IGenericRepository<Role> Roles =>
        _roles ??= new GenericRepository<Role>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}