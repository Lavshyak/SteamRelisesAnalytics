using System.Text.Json.Nodes;
using HtmlAgilityPack;
using Shared.PipelineModels;

namespace ParseProductPipeline.Followers;

internal class FollowersParser(Func<CollectedProductReleaseAndDataAndFollowersCount, Task> onParsedOne, HttpClient httpClient)
{
    public async Task Parse(CollectedProductReleasesAndData collectedProductReleasesAndData, string sessionId)
    {
        var url =
            $"https://steamcommunity.com/search/SearchCommunityAjax?text={collectedProductReleasesAndData.ProductRelease.Title}&filter=groups&sessionid={sessionId}&steamid_user=false&page=1";
        using var resp = await httpClient.GetAsync(url);
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var rootJsonNode = await JsonNode.ParseAsync(stream);
        var html = rootJsonNode["html"].GetValue<string>();
        var hd = new HtmlDocument();
        hd.LoadHtml(html);
        
        var node = hd.DocumentNode
            .SelectSingleNode($"//a[contains(@href, '{collectedProductReleasesAndData.ProductRelease.AppId}')]/ancestor::div[contains(@class, 'search_row')]//span[@style='color: whitesmoke']");

        var membersStr = node?.InnerText.Trim();
        var members = long.Parse(membersStr);
        await onParsedOne(new CollectedProductReleaseAndDataAndFollowersCount(collectedProductReleasesAndData, members));
    }
}