using System.ComponentModel.DataAnnotations;

namespace PersistencePostgres.Entities;

public class ClickhouseMigration
{
    [Key]
    public required string Id { get; set; }
}