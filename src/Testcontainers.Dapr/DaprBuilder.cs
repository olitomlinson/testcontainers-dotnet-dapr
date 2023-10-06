namespace Testcontainers.Dapr;

[PublicAPI]
public sealed class DaprBuilder : ContainerBuilder<DaprBuilder, DaprContainer, DaprConfiguration>
{
    public const string DaprImage = "daprio/daprd";
    public string Tag;
    public const int DaprHttpPort = 3500;
    public const int DaprGrpcPort = 50001;
    public const string LogLevel = "info";

    public DaprBuilder(string tag = "latest")
        : this(new DaprConfiguration())
    {
        Tag = tag;
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
        if (!string.IsNullOrEmpty(DockerResourceConfiguration.AppId))
            throw new ArgumentException($"'AppId' has already been set. It can only be set once", "appId");

        return Merge(DockerResourceConfiguration, new DaprConfiguration(appId: appId))
            .WithCommand("--app-id", appId);
    }

    public DaprBuilder WithAppPort(int appPort)
    {
        if ((DockerResourceConfiguration.AppPort.HasValue))
            throw new ArgumentException($"'AppPort' has already been set. It can only be set once", "appPort");

        return Merge(DockerResourceConfiguration, new DaprConfiguration(appPort: appPort))
            .WithCommand("--app-port", appPort.ToString());
    }
    
    public DaprBuilder WithLogLevel(string logLevel)
    {
        if (!string.IsNullOrEmpty(DockerResourceConfiguration.LogLevel))
            throw new ArgumentException("'LogLevel' has already been set. It can only be set once", "logLevel");

        return Merge(DockerResourceConfiguration, new DaprConfiguration(logLevel: logLevel))
            .WithCommand("--log-level", logLevel);
    }

    public DaprBuilder WithAppChannelAddress(string appChannelAddress)
    {
        if (!string.IsNullOrEmpty(DockerResourceConfiguration.AppChannelAddress))
            throw new ArgumentException("'AppChannelAddress' has already been set. It can only be set once", "appChannelAddress");

        return Merge(DockerResourceConfiguration, new DaprConfiguration(appChannelAddress: appChannelAddress))
            .WithCommand("--app-channel-address", appChannelAddress);
    }

    public DaprBuilder WithResourcesPath(string resourcesPath, bool useLegacyComponentPath = false){
        if (!string.IsNullOrEmpty(DockerResourceConfiguration.ResourcesPath))
            throw new ArgumentException("'ResourcePath' has already been set. It can only be set once", "resourcePath");
        string command = useLegacyComponentPath ? "--components-path" : "--resources-path";

        return Merge(DockerResourceConfiguration, new DaprConfiguration(resourcesPath: resourcesPath))
            .WithCommand(command, resourcesPath)
            .WithResourceMapping(resourcesPath, $"/{resourcesPath}/");
    }

    public override DaprContainer Build()
    {
        Validate();

        return new DaprContainer(DockerResourceConfiguration, TestcontainersSettings.Logger);
    }

    protected override DaprBuilder Init()
    {
        return base.Init()
            .WithImage($"{DaprImage}:{Tag}")
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