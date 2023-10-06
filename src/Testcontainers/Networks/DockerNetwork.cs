namespace DotNet.Testcontainers.Networks
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Docker.DotNet.Models;
  using DotNet.Testcontainers.Clients;
  using DotNet.Testcontainers.Configurations;
  using JetBrains.Annotations;
  using Microsoft.Extensions.Logging;

  /// <inheritdoc cref="INetwork" />
  [PublicAPI]
  internal sealed class DockerNetwork : Resource, INetwork
  {
    private readonly ITestcontainersClient _client;

    private readonly INetworkConfiguration _configuration;

    private NetworkResponse _network = new NetworkResponse();

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerNetwork" /> class.
    /// </summary>
    /// <param name="configuration">The network configuration.</param>
    /// <param name="logger">The logger.</param>
    public DockerNetwork(INetworkConfiguration configuration, ILogger logger)
    {
      _client = new TestcontainersClient(configuration.SessionId, configuration.DockerEndpointAuthConfig, logger);
      _configuration = configuration;
    }

    /// <inheritdoc />
    public string Name
    {
      get
      {
        ThrowIfResourceNotFound();
        return _network.Name;
      }
    }

    /// <inheritdoc />
    public async Task CreateAsync(CancellationToken ct = default)
    {
      using (_ = AcquireLock())
      {
        await UnsafeCreateAsync(ct)
          .ConfigureAwait(false);
      }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken ct = default)
    {
      using (_ = AcquireLock())
      {
        await UnsafeDeleteAsync(ct)
          .ConfigureAwait(false);
      }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
      if (Disposed)
      {
        return;
      }

      if (!Guid.Empty.Equals(_configuration.SessionId))
      {
        await DeleteAsync()
          .ConfigureAwait(false);
      }

      await base.DisposeAsyncCore()
        .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override bool Exists()
    {
      return !string.IsNullOrEmpty(_network.ID);
    }

    /// <inheritdoc />
    protected override async Task UnsafeCreateAsync(CancellationToken ct = default)
    {
      ThrowIfLockNotAcquired();

      if (Exists())
      {
        return;
      }

      var id = await _client.Network.CreateAsync(_configuration, ct)
        .ConfigureAwait(false);

      _network = await _client.Network.ByIdAsync(id, ct)
        .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task UnsafeDeleteAsync(CancellationToken ct = default)
    {
      ThrowIfLockNotAcquired();

      if (!Exists())
      {
        return;
      }

      await _client.Network.DeleteAsync(_network.ID, ct)
        .ConfigureAwait(false);

      _network = new NetworkResponse();
    }
  }
}
