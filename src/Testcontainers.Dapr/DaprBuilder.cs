namespace Testcontainers.Dapr;

[PublicAPI]
public sealed class DaprBuilder : ContainerBuilder<DaprBuilder, DaprContainer, DaprConfiguration>
{
    public const string DaprImage = "daprio/daprd:nightly-2023-04-28";
    public const int DaprHttpPort = 3500;
    public const int DaprGrpcPort = 50001;
    public const string LogLevel = "info";

    public DaprBuilder()
        : this(new DaprConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    private DaprBuilder(DaprConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }

    protected override DaprConfiguration DockerResourceConfiguration { get; }

    public DaprBuilder WithAppId(string appId)
    {
        // TODO: What happens if developers call WithAppId(string) multiple times?
        return Merge(DockerResourceConfiguration, new DaprConfiguration(appId: appId))
            .WithCommand("--app-id", appId);
    }

    public DaprBuilder WithAppPort(int appPort)
    {
        // TODO: What happens if developers call WithAppId(string) multiple times?
        return Merge(DockerResourceConfiguration, new DaprConfiguration())
            .WithCommand("--app-port", appPort.ToString());
    }
    
    public DaprBuilder WithLogLevel(string logLevel)
    {
        // TODO : Introduce an Enum for logLevel values.
        return Merge(DockerResourceConfiguration, new DaprConfiguration(logLevel: logLevel))
            .WithCommand("--log-level", logLevel);
    }

    public DaprBuilder WithAppChannelAddress(string appChannelHost)
    {
        // TODO : Introduce an Enum for logLevel values.
        return Merge(DockerResourceConfiguration, new DaprConfiguration(appChannelAddress: appChannelHost))
            .WithCommand("--app-channel-address", appChannelHost);
    }

    public DaprBuilder WithResourcesPath(string resourcesPath){
        return Merge(DockerResourceConfiguration, new DaprConfiguration(resourcesPath: resourcesPath))
            .WithCommand("--resources-path", resourcesPath);
    }

    public override DaprContainer Build()
    {
        Validate();

        return new DaprContainer(DockerResourceConfiguration, TestcontainersSettings.Logger);
    }

    protected override DaprBuilder Init()
    {
        return base.Init()
            .WithImage(DaprImage)
            .WithEntrypoint("./daprd")
            .WithCommand("-dapr-http-port", DaprHttpPort.ToString())
            .WithCommand("-dapr-grpc-port", DaprGrpcPort.ToString())
            .WithPortBinding(DaprHttpPort, true)
            .WithPortBinding(DaprGrpcPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                request.ForPath("/v1.0/healthz").ForPort(DaprHttpPort).ForStatusCode(HttpStatusCode.NoContent)));
    }

    protected override void Validate()
    {
        base.Validate();

        _ = Guard.Argument(DockerResourceConfiguration.AppId, nameof(DockerResourceConfiguration.AppId))
            .NotNull()
            .NotEmpty();
    }

    protected override DaprBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new DaprConfiguration(resourceConfiguration));
    }

    protected override DaprBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new DaprConfiguration(resourceConfiguration));
    }

    protected override DaprBuilder Merge(DaprConfiguration oldValue, DaprConfiguration newValue)
    {
        return new DaprBuilder(new DaprConfiguration(oldValue, newValue));
    }
}