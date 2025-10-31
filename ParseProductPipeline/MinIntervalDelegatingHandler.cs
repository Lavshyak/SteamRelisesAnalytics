namespace ParseProductPipeline;

public class MinIntervalDelegatingHandler : DelegatingHandler
{
    public static void CreateHttpClientWithMinDelay(TimeSpan delay)
    {
        var handler = new MinIntervalDelegatingHandler(delay)
        {
            InnerHandler = new HttpClientHandler()
        };

        var client = new HttpClient(handler);
    }
    
    private readonly TimeSpan _minInterval;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly object _lock = new();

    public MinIntervalDelegatingHandler(TimeSpan minInterval)
    {
        _minInterval = minInterval;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        DateTime waitUntil;

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var nextAllowed = _lastRequestTime + _minInterval;
            waitUntil = nextAllowed > now ? nextAllowed : now;
        }

        var delay = waitUntil - DateTime.UtcNow;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);

        lock (_lock)
        {
            _lastRequestTime = DateTime.UtcNow;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}