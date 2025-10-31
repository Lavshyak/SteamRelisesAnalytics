using ClickHouse.Driver.ADO;
using ClickHouse.Driver.Utility;
using PersistencePostgres;
using PersistencePostgres.Entities;

namespace PersistenceClickhouse;

public class ClickHouseMigrations
{
    private readonly MainDbContext _mainDbContext;
    private readonly ClickHouseConnection _clickHouseConnection;

    public ClickHouseMigrations(MainDbContext mainDbContext, ClickHouseConnection clickHouseConnection)
    {
        _mainDbContext = mainDbContext;
        _clickHouseConnection = clickHouseConnection;
    }
    
    public async Task Migrate()
    {
        var files = Directory.EnumerateFiles("./ClickHouseMigrations").Order();
        var appliedMigrations = _mainDbContext.ClickhouseMigrations.ToArray();

        foreach (var file in files.Where(f => appliedMigrations.Any(m => m.Id == Path.GetFileNameWithoutExtension(f))))
        {
            var sql = await File.ReadAllTextAsync(file);

            await _clickHouseConnection.ExecuteStatementAsync(sql);
            _mainDbContext.ClickhouseMigrations.Add(new ClickhouseMigration()
            {
                Id = Path.GetFileNameWithoutExtension(file)
            });
            await _mainDbContext.SaveChangesAsync();
        }
    }
}