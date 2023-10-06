namespace Testcontainers.Dapr;

[PublicAPI]
public sealed class DaprConfiguration : ContainerConfiguration
{
    public DaprConfiguration(string appId = null, string logLevel = null, string appChannelAddress = null, string resourcesPath = null, int? appPort = null)
    {
        AppId = appId;
        AppPort = appPort;
        LogLevel = logLevel;
        AppChannelAddress = appChannelAddress;
        ResourcesPath = resourcesPath;
    }

    public DaprConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public DaprConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public DaprConfiguration(DaprConfiguration resourceConfiguration)
        : this(new DaprConfiguration(), resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public DaprConfiguration(DaprConfiguration oldValue, DaprConfiguration newValue)
        : base(oldValue, newValue)
    {
        AppId = BuildConfiguration.Combine(oldValue.AppId, newValue.AppId);
        AppPort = BuildConfiguration.Combine(oldValue.AppPort, newValue.AppPort);
        LogLevel = BuildConfiguration.Combine(oldValue.LogLevel, newValue.LogLevel);
        AppChannelAddress = BuildConfiguration.Combine(oldValue.AppChannelAddress, newValue.AppChannelAddress);
        ResourcesPath = BuildConfiguration.Combine(oldValue.ResourcesPath, newValue.ResourcesPath);
    }

    public string AppId { get; }
    public int? AppPort {get; }
    public string LogLevel { get; }
    public string AppChannelAddress { get;set; }
    public string ResourcesPath { get; set; }
}