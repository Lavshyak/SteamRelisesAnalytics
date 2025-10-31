using ClickHouse.Driver.ADO;

namespace PersistenceClickhouse;

public interface ICustomClickHouseConnectionFactory
{
    public ClickHouseConnection CreateConnection();
}

/// <summary>
/// connection pool, etc
/// </summary>
public class CustomClickHouseConnectionFactory : ICustomClickHouseConnectionFactory
{
    private readonly string _connectionString;

    public CustomClickHouseConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ClickHouseConnection CreateConnection()
    {
        var connection = new ClickHouseConnection(_connectionString);
        return connection;
    }
}