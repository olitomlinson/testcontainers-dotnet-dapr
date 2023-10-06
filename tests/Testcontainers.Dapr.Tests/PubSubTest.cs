using System.Net.Http;
using System.Threading;
using Testcontainers.Redis;

namespace Testcontainers.Dapr;

public sealed class PubSubTests : IAsyncLifetime
{
    private DaprContainer _subscriberDaprContainer;
    private DaprContainer _publisherDaprContainer;
    private IContainer _subscriberAppContainer;
    private RedisContainer _redisContainer;
    private INetwork _network;
    private string _daprAppId = "subscriber";
    public async Task InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
        await _network.CreateAsync().ConfigureAwait(false);

        _redisContainer = new RedisBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases("redis")
            .Build();
        await _redisContainer.StartAsync();

        const ushort receiverAppPort = 80;
        string subscriberAppNetworkAlias = "subscriber-app";
        _subscriberAppContainer = new ContainerBuilder()
            .WithName("subscriber-app")
            .WithNetwork(_network)
            .WithNetworkAliases(subscriberAppNetworkAlias)
            .WithImage("nginx")
            .WithExposedPort(receiverAppPort)
            .WithResourceMapping($"nginx", "/etc/nginx/")
            .Build();
        await _subscriberAppContainer.StartAsync().ConfigureAwait(false);

        _subscriberDaprContainer = new DaprBuilder()
            .WithName("subscriber-dapr")
            .WithAppId(_daprAppId)
            .WithAppChannelAddress(subscriberAppNetworkAlias)
            .WithAppPort(receiverAppPort)
            .WithResourcesPath("components")
            .WithLogLevel("debug")
            .WithNetwork(_network)
            .DependsOn(_subscriberAppContainer)
            .Build();
        await _subscriberDaprContainer.StartAsync().ConfigureAwait(false);

        _publisherDaprContainer = new DaprBuilder()
            .WithName("publisher-dapr")
            .WithAppId("publisher")
            .WithNetwork(_network)
            .WithResourcesPath("components")
            .DependsOn(_subscriberDaprContainer)
            .Build();
        await _publisherDaprContainer.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _publisherDaprContainer.DisposeAsync().ConfigureAwait(false);
        await _subscriberDaprContainer.DisposeAsync().ConfigureAwait(false);
        await _subscriberAppContainer.DisposeAsync().ConfigureAwait(false);
        await _redisContainer.DisposeAsync().AsTask();
        await _network.DeleteAsync().ConfigureAwait(false);
    }

    [Fact]
    [Trait(nameof(DockerCli.DockerPlatform), nameof(DockerCli.DockerPlatform.Linux))]
    public async Task MessageCanBePublishedAndReceived()
    {            
        // Given
        using var client = new DaprClientBuilder()
            .UseHttpEndpoint(_publisherDaprContainer.GetHttpAddress())
            .UseGrpcEndpoint(_publisherDaprContainer.GetGrpcAddress())
            .Build();

        // When
        var healthy = await client.CheckHealthAsync();
        var cts = new CancellationTokenSource();
        await client.PublishEventAsync("redis-pubsub", "test-topic", Guid.NewGuid().ToString(), cts.Token);

        // Then
        var found = false;
        foreach(var i in Enumerable.Range(1, 30))
        {
            var (stdout, stderr) = await _subscriberDaprContainer.GetLogsAsync()
                .ConfigureAwait(false);

            if (stdout.Contains("Processing Redis message"))
            {
                found = true;
                break;
            }

            await Task.Delay(1000);
        }

        Assert.Equal(true, found);
    }
}