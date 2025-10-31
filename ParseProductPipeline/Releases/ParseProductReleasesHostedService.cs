using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.PipelineModels;

namespace ParseProductPipeline.Releases;

public class ParseProductReleasesHostedService : IHostedService
{
    private readonly IConnection _connection;
    private readonly HttpClient _httpClient;


    public ParseProductReleasesHostedService([FromKeyedServices("RabbitMqConnection")] IConnection connection,
        [FromKeyedServices("HttpClientForParseProductReleasesHostedService")]
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
        await channelInput.QueueDeclareAsync(queue: "CollectProductInfosTasks", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        await channelInput.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channelInput);

        var channelOutput = await _connection.CreateChannelAsync(null, _cancellationTokenSource.Token);
        await channelOutput.QueueDeclareAsync("CollectedProductReleasesAndData", true, false, false, null,
            cancellationToken: cancellationToken);

        async Task OnParsedOne(CollectedProductRelease collectedProductRelease)
        {
            var json = JsonSerializer.Serialize(collectedProductRelease);
            var body = Encoding.UTF8.GetBytes(json);
            await channelOutput.BasicPublishAsync("", "CollectedProductReleasesAndData", true,
                new BasicProperties() { Persistent = true }, body, cancellationToken);
        }

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var parsingTask = JsonSerializer.Deserialize<CollectProductInfosTask>(ea.Body.Span);
                if (parsingTask is null)
                    throw new InvalidOperationException();

                var releasesParser = new ReleasesParser(OnParsedOne, _httpClient);
                await releasesParser.Parse(parsingTask.Year, parsingTask.FromMonth, parsingTask.ToMonth);
                await channelInput.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        };

        await channelInput.BasicConsumeAsync("CollectProductInfosTasks", autoAck: false, consumer: consumer,
            cancellationToken);
        Console.WriteLine("Listening started.");
    }
}