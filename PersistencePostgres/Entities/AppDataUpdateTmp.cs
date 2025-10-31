using Microsoft.EntityFrameworkCore;

namespace PersistencePostgres.Entities;

[PrimaryKey(nameof(UpdateId), nameof(AppId))]
public class AppReleaseDataTmp
{
    public required Guid UpdateId { get; set; }
    public required long AppId { get; set; }
    public required UInt16 Year { get; set; }
    public required UInt16 Month { get; set; }
    public required UInt16 Day { get; set; }
    public required DateTime FetchedAt { get; set; }
}

[PrimaryKey(nameof(UpdateId), nameof(AppId))]
public class AppDataTmp
{
    public required Guid UpdateId { get; set; }
    public required long AppId { get; set; }
    public required string Title { get; set; }
    public required string[] Tags { get; set; }
    public required string StoreUrl { get; set; }
    public required string ImageUrl { get; set; }
    public required string ShortDescription { get; set; }
    public required string[] Platforms { get; set; }
    public DateTime FetchedAt { get; set; }
}

[PrimaryKey(nameof(UpdateId), nameof(AppId))]
public class AppFollowersDataTmp
{
    public required Guid UpdateId { get; set; }
    public required long AppId { get; set; }

    public long FollowersCount { get; set; }
}