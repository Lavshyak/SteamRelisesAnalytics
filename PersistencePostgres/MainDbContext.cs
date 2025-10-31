using Microsoft.EntityFrameworkCore;
using PersistencePostgres.Entities;

namespace PersistencePostgres;

public class MainDbContext : DbContext
{
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Updates> Updates { get; private set; } = null!;
    public DbSet<ClickhouseMigration> ClickhouseMigrations { get; private set; } = null!;
}