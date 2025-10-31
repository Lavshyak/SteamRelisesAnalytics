using System.Text.Json.Nodes;
using Shared.PipelineModels;

namespace ParseProductPipeline.AppData;

internal class AppDetailsParser(Func<CollectedProductReleasesAndData, Task> onParsedOne, HttpClient httpClient)
{
    public async Task Parse(CollectedProductRelease collectedProductRelease)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={collectedProductRelease.AppId}";
        using var resp = await httpClient.GetAsync(url);

        await using var stream = await resp.Content.ReadAsStreamAsync();
        var jsonNode = await JsonNode.ParseAsync(stream);
        var appNode = jsonNode[0];
        var dataNode = appNode["data"];

        var shortDescription = dataNode["short_description"].GetValue<string>();
        var platformsNode = dataNode["platforms"];
        string[] platforms = ((string?[])
        [
            platformsNode?["windows"]?.GetValue<bool>() is true ? "windows" : null,
            platformsNode?["linux"]?.GetValue<bool>() is true ? "linux" : null,
            platformsNode?["mac"]?.GetValue<bool>() is true ? "mac" : null,
        ]).Where(s => s != null).ToArray()!;
        var categories = dataNode["categories"].AsArray().Select(node =>
            node["description"].GetValue<string>()
        ).ToArray();
        var genres = dataNode["genres"].AsArray().Select(node =>
            node["description"].GetValue<string>()
        ).ToArray();

        var result = new CollectedProductReleasesAndData(collectedProductRelease, shortDescription, genres, categories,
            $"https://store.steampowered.com/app/{collectedProductRelease.AppId}",
            $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{collectedProductRelease.AppId}/header.jpg",
            platforms);

        await onParsedOne(result);
    }
}