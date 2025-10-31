namespace Shared.PipelineModels;

public record CollectProductInfosTask(int Year, int FromMonth, int ToMonth);
public record CollectedProductRelease(long AppId, string Title, int Year, int Month, int? DateOfMonth, DateTime FetchedAt);
public record CollectedProductReleasesAndData(CollectedProductRelease ProductRelease, string ShortDescription, string[] Genres, string[] Categories, string StoreUrl, string ImageUrl, string[] Platforms);
public record CollectedProductReleaseAndDataAndFollowersCount(CollectedProductReleasesAndData CollectedProductReleasesAndData, long FollowersCount);