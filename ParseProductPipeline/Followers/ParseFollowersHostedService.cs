using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParseProductPipeline.Releases;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.PipelineModels;

namespace ParseProductPipeline.Followers;

public class ParseFollowersHostedService : IHostedService
{
    private readonly IConnection _connection;
    private readonly HttpClient _httpClient;


    public ParseFollowersHostedService([FromKeyedServices("RabbitMqConnection")] IConnection connection,
        [FromKeyedServices("HttpClientForParseFollowersHostedService")]
        HttpClient httpClient)
    {
        _connection = connection;
        _httpClient = httpClient;
    }

    private Task _listenerTask = null!;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listenerTask = InitAndListeningAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellationTokenSource.CancelAsync();
        await _listenerTask.WaitAsync(cancellationToken);
    }

    private async Task InitAndListeningAsync(CancellationToken cancellationToken)
    {
        var channelInput = await _connection.CreateChannelAsync(null, _cancellationTokenSource.Token);
        await channelInput.QueueDeclareAsync(queue: "CollectedProductReleasesAndData", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        await channelInput.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channelInput);

        var channelOutput = await _connection.CreateChannelAsync(null, _cancellationTokenSource.Token);
        await channelOutput.QueueDeclareAsync("CollectedProductReleasesAndDataAndFollowersCount", true, false, false,
            null,
            cancellationToken: cancellationToken);

        async Task OnParsedOne(CollectedProductReleaseAndDataAndFollowersCount value)
        {
            var json = JsonSerializer.Serialize(value);
            var body = Encoding.UTF8.GetBytes(json);
            await channelOutput.BasicPublishAsync("", "CollectedProductReleasesAndDataAndFollowersCount", true,
                new BasicProperties() { Persistent = true }, body, cancellationToken);
        }


        string? TryGetSessionId(IEnumerable<string> cookies)
        {
            foreach (var cookie in cookies)
            {
                var parts = cookie.Split(';');
                foreach (var part in parts)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2 && kv[0].Trim() == "sessionid")
                    {
                        return kv[1].Trim();
                    }
                }
            }

            return null;
        }

        string? sessionId = null;
        {
            var resp = await _httpClient.GetAsync(
                "https://steamcommunity.com/search/groups/?text=Resident%20Evil%20Requiem");
            if (resp.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                sessionId = TryGetSessionId(cookies);
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", [$"sessionid={sessionId}"]);
            }
        }
        if (sessionId == null)
        {
            throw new InvalidOperationException("Без sessionId в url будет 401");
        }


        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var collectedPRAD = JsonSerializer.Deserialize<CollectedProductReleasesAndData>(ea.Body.Span);
                if (collectedPRAD is null)
                    throw new InvalidOperationException();

                var parser = new FollowersParser(OnParsedOne, _httpClient);
                await parser.Parse(collectedPRAD, sessionId);
                await channelInput.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        };

        await channelInput.BasicConsumeAsync("CollectedProductReleasesAndData", autoAck: false, consumer: consumer,
            cancellationToken);
        Console.WriteLine("Listening started.");
    }
}