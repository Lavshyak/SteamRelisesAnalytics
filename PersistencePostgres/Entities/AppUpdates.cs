using Microsoft.EntityFrameworkCore;

namespace PersistencePostgres.Entities;

[PrimaryKey(nameof(DateTime), nameof(AppId))]
public class AppUpdates
{
    public DateTime DateTime { get; set; }
    public long AppId { get; set; }
}