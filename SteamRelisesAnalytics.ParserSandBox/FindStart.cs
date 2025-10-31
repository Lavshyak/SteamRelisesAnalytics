using System.Text.Json.Nodes;

namespace SteamRelisesAnalytics.ParserSandBox;

public class FindRange
{
    private HttpClient _httpClient;
    public FindRange(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void Find()
    {
        int start = 5000;
    }
    
    private async Task Query(int skip, int take = 10)
    {
        string url =
            $"https://store.steampowered.com/search/results/?query=" +
            $"&start={skip}" +
            $"&count={take}" +
            $"&dynamic_data=&sort_by=Released_DESC&force_infinite=1" +
            $"&filter=comingsoon&ndl=1&snr=1_7_7_comingsoon_7&infinite=1";

        var resp = await _httpClient.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException();
        var contentStream = await resp.Content.ReadAsStreamAsync();
        var jsonNode = await JsonNode.ParseAsync(contentStream);
        if(jsonNode == null)
            throw new InvalidOperationException();
        var resultsHtml = jsonNode["results_html"]?.GetValue<string>() ?? throw new InvalidOperationException();
    }
}