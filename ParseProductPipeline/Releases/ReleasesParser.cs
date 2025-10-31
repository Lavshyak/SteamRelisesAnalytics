using System.Text.Json.Nodes;
using HtmlAgilityPack;
using Shared.PipelineModels;

namespace ParseProductPipeline.Releases;

internal class ReleasesParser
{
    private readonly Func<CollectedProductRelease, Task> _onParsedOne;
    private readonly HttpClient _httpClient;

    public ReleasesParser(Func<CollectedProductRelease, Task> onParsedOne, HttpClient httpClient)
    {
        _onParsedOne = onParsedOne;
        _httpClient = httpClient;
    }

    public async Task Parse(int correctYear = 2025, int fromMonth = 9, int toMonth = 11)
    {
        /*int correctYear = 2025;
        int fromMonth = 9;
        int toMonth = 11;*/

        var towardsTheFutureIdx = 5900; // future <-
        var towardsThePastIdx = 5001; // -> past

        async Task SaveReleaseItem(string appId, string appTitle, int year, int month, int? dayOfMonth)
        {
            await _onParsedOne(new CollectedProductRelease(int.Parse(appId), appTitle, year, month, dayOfMonth,
                DateTime.Now));
        }

        // towardsThePast
        while (true)
        {
            var jn = await QueryReleases(towardsThePastIdx, 100);
            var hnc = GetReleaseItems(jn);
            var count = hnc.Count;

            if (count == 0)
            {
                // либо нет приложений, либо код с багами
                throw new InvalidOperationException();
            }

            bool isTooFuture = false;
            bool isTooPast = false;
            for (int i = 0; i < count; i++)
            {
                var releaseItem = ParseReleaseItem(hnc[i]);
                var checkReleaseDateResult = ReleasesParserTools.Check(releaseItem.ReleaseDate, correctYear, fromMonth, toMonth);
                if (checkReleaseDateResult.PositionCheckResult == ReleasesParserTools.PositionCheckResult.TooFuture)
                {
                    isTooFuture = true;
                    break;
                }

                if (checkReleaseDateResult.PositionCheckResult == ReleasesParserTools.PositionCheckResult.TooPast)
                {
                    isTooPast = false;
                    break;
                }

                if (checkReleaseDateResult.PositionCheckResult == ReleasesParserTools.PositionCheckResult.SkipThis)
                {
                    continue;
                }

                if (checkReleaseDateResult.PositionCheckResult != ReleasesParserTools.PositionCheckResult.Correct ||
                    checkReleaseDateResult.Month.HasValue == false)
                {
                    throw new InvalidOperationException("такого быть не должно");
                }

                await SaveReleaseItem(releaseItem.Appid, releaseItem.Appid, correctYear,
                    checkReleaseDateResult.Month.Value, checkReleaseDateResult.DayOfMonth);
            }

            if (isTooPast)
            {
                break;
            }

            if (isTooFuture)
            {
                // пока без оптимизаций
            }

            // можно еще проверки добавить, связанные с тем что приложений ограниченное количество

            towardsThePastIdx += count;
        }


        // towards the future
        var appsCountToRequest = 100;
        while (true)
        {
            var jn = await QueryReleases(towardsTheFutureIdx, appsCountToRequest);
            var hnc = GetReleaseItems(jn);
            var count = hnc.Count;

            if (count == 0)
            {
                // либо нет приложений, либо код с багами
                throw new InvalidOperationException();
            }

            bool isTooFuture = false;
            bool isTooPast = false;
            for (int i = count - 1; i >= 0; i--)
            {
                var releaseItem = ParseReleaseItem(hnc[i]);
                var checkReleaseDateResult = ReleasesParserTools.Check(releaseItem.ReleaseDate, correctYear, fromMonth, toMonth);
                if (checkReleaseDateResult.PositionCheckResult == ReleasesParserTools.PositionCheckResult.TooFuture)
                {
                    isTooFuture = true;
                    break;
                }

                if (checkReleaseDateResult.PositionCheckResult == ReleasesParserTools.PositionCheckResult.TooPast)
                {
                    isTooPast = false;
                    break;
                }

                if (checkReleaseDateResult.PositionCheckResult == ReleasesParserTools.PositionCheckResult.SkipThis)
                {
                    continue;
                }

                if (checkReleaseDateResult.PositionCheckResult != ReleasesParserTools.PositionCheckResult.Correct ||
                    checkReleaseDateResult.Month.HasValue == false)
                {
                    throw new InvalidOperationException("такого быть не должно");
                }

                await SaveReleaseItem(releaseItem.Appid, releaseItem.Appid, correctYear,
                    checkReleaseDateResult.Month.Value, checkReleaseDateResult.DayOfMonth);
            }

            if (isTooPast)
            {
                // пока без оптимизаций
            }

            if (isTooFuture)
            {
                break;
            }

            if (towardsTheFutureIdx - count < 0)
            {
                appsCountToRequest = towardsTheFutureIdx;
                towardsTheFutureIdx = 0;
            }
            else
            {
                towardsTheFutureIdx -= count;
            }
        }
    }

    async Task<JsonNode> QueryReleases(int start, int count = 25)
    {
        string url =
            $"https://store.steampowered.com/search/results/?query=" +
            $"&start={start}" +
            $"&count=25" /*там возвращается минимум 25 и максимум 100*/ +
            $"&dynamic_data=&sort_by=Released_DESC&force_infinite=1" +
            $"&filter=comingsoon&ndl=1&snr=1_7_7_comingsoon_7&infinite=1";

        using var resp = await _httpClient.GetAsync(url);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException();
        await using var contentStream = await resp.Content.ReadAsStreamAsync();
        var jsonNode = await JsonNode.ParseAsync(contentStream);
        if (jsonNode == null)
            throw new InvalidOperationException();
        return jsonNode;
    }

    HtmlNodeCollection GetReleaseItems(JsonNode jsonNode)
    {
        var resultsHtml = jsonNode["results_html"]?.GetValue<string>() ?? throw new InvalidOperationException();

        var hd = new HtmlDocument();

        hd.LoadHtml(resultsHtml);

        var items = hd.DocumentNode.SelectNodes("/a");

        return items;
    }

    record AppReleaseData(string ReleaseDate, string Appid, string Title);

    AppReleaseData ParseReleaseItem(HtmlNode item)
    {
        var releaseData = item.SelectSingleNode(".//div[contains(@class, 'search_released')]").InnerHtml.Trim();
        var appid = item.Attributes.First(attr => attr.Name == "data-ds-appid").Value;
        var title = item.SelectSingleNode(".//span[contains(@class, 'title')]").InnerHtml.Trim();
        return new AppReleaseData(releaseData, appid, title);
    }
}