using System.Text;
using System.Threading;
using Testcontainers.Redis;

namespace Testcontainers.Dapr;

public sealed class PubSubTests : IAsyncLifetime
{
    private RedisContainer _redisContainer;
    private DaprContainer _receiverDaprContainer;
    private DaprContainer _senderDaprContainer;
    private IContainer _receiverAppContainer;
    private INetwork _network;
    private const string _pubsubName = "mypubsub";
    private string _pubsubComponent = @"---
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
         name: {0}
        spec:
         type: pubsub.redis
         version: v1
         metadata:
         - name: redisHost
           value: ""{1}""
         - name: redisPassword
           value: ""{2}""
         - name: enableTLS
           value: ""false""";

    private const string _stateStoreName = "mystatestore";
    private string _stateStoreComponent = @"---
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
         name: {0}
        spec:
         type: state.redis
         version: v1
         metadata:
         - name: redisHost
           value: ""{1}""
         - name: redisPassword
           value: ""{2}""
         - name: keyPrefix
           value: name";

    public async Task InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
        await _network.CreateAsync().ConfigureAwait(false);

        var redisAlias = "redis";
        _redisContainer = new RedisBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases(redisAlias)
            .Build();
        await _redisContainer.StartAsync().ConfigureAwait(false);

        var pubsubComponent = string.Format(_pubsubComponent, _pubsubName, $"{redisAlias}:{RedisBuilder.RedisPort}", "");
        var stateComponent = string.Format(_stateStoreComponent, _stateStoreName, $"{redisAlias}:{RedisBuilder.RedisPort}", "");


        const ushort receiverAppPort = 80;
        string receiverAppNetworkAlias = "receiver-app";
        _receiverAppContainer = new ContainerBuilder()
            .WithNetwork(_network)
            .WithNetworkAliases(receiverAppNetworkAlias)
            .WithImage("nginx")
            .WithExposedPort(receiverAppPort)
            .WithResourceMapping($"nginx/nginx-pubsub.conf", "/etc/nginx/nginx.conf")
            .Build();
        await _receiverAppContainer.StartAsync().ConfigureAwait(false);

        _receiverDaprContainer = new DaprBuilder()
            .WithLogLevel("debug")
            .WithAppId("receiver-app")
            .WithNetwork(_network)
            .WithNetworkAliases("receiver-dapr")
            .WithAppChannelAddress(receiverAppNetworkAlias)
            .WithAppPort(receiverAppPort)
            .WithResourcesPath("/DaprComponents")
            .WithResourceMapping(Encoding.Default.GetBytes(pubsubComponent), "/DaprComponents/pubsub.yaml")
            .WithResourceMapping(Encoding.Default.GetBytes(stateComponent), "/DaprComponents/statestore.yaml")
            .WithPortBinding(DaprBuilder.DaprHttpPort, true)
            .DependsOn(_redisContainer)
            .Build();
        await _receiverDaprContainer.StartAsync().ConfigureAwait(false);

        _senderDaprContainer = new DaprBuilder()
            .WithName("sender-dapr")
            .WithAppId("sender")
            .WithNetwork(_network)
            .DependsOn(_receiverDaprContainer)
            .Build();
        await _senderDaprContainer.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _senderDaprContainer.DisposeAsync().ConfigureAwait(false);
        await _receiverDaprContainer.DisposeAsync().ConfigureAwait(false);
        await _receiverAppContainer.DisposeAsync().ConfigureAwait(false);
        await _network.DeleteAsync().ConfigureAwait(false);
    }

    [Fact]
    [Trait(nameof(DockerCli.DockerPlatform), nameof(DockerCli.DockerPlatform.Linux))]
    public async Task StateCanBeSet()
    {            
        // Given
        using var client = new DaprClientBuilder()
            .UseHttpEndpoint(_senderDaprContainer.GetHttpAddress())
            .UseGrpcEndpoint(_senderDaprContainer.GetGrpcAddress())
            .Build();

        // When
        var healthy = await client.CheckHealthAsync();
        var cts = new CancellationTokenSource();

        var key = Guid.NewGuid().ToString();
        var setValue = $"Chicken {key}";

        var metadata = new Dictionary<string, string>
        {
            ["rawPayload"] = "true"
        };

        var events = new List<PubSubEvent>();
        events.Add(new PubSubEvent(){ Key = "ping", Value = "pong" });
        await client.PublishEventAsync<List<PubSubEvent>>(_pubsubName, "some-topic", events, metadata);


        // // Then
        // Assert.Equal(setValue, getResult);
    }
}

public class PubSubEvent
{
    public string Key { get; set; }

    public string Value { get; set; }
}