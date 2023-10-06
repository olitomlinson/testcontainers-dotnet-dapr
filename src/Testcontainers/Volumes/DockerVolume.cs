namespace DotNet.Testcontainers.Volumes
{
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using Docker.DotNet.Models;
  using DotNet.Testcontainers.Clients;
  using DotNet.Testcontainers.Configurations;
  using JetBrains.Annotations;
  using Microsoft.Extensions.Logging;

  /// <inheritdoc cref="IVolume" />
  [PublicAPI]
  internal sealed class DockerVolume : Resource, IVolume
  {
    private readonly ITestcontainersClient _client;

    private readonly IVolumeConfiguration _configuration;

    private VolumeResponse _volume = new VolumeResponse();

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerVolume" /> class.
    /// </summary>
    /// <param name="configuration">The volume configuration.</param>
    /// <param name="logger">The logger.</param>
    public DockerVolume(IVolumeConfiguration configuration, ILogger logger)
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
        return _volume.Name;
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
      return !string.IsNullOrEmpty(_volume.Name);
    }

    /// <inheritdoc />
    protected override async Task UnsafeCreateAsync(CancellationToken ct = default)
    {
      ThrowIfLockNotAcquired();

      if (Exists())
      {
        return;
      }

      var name = await _client.Volume.CreateAsync(_configuration, ct)
        .ConfigureAwait(false);

      _volume = await _client.Volume.ByNameAsync(name, ct)
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

      await _client.Volume.DeleteAsync(Name, ct)
        .ConfigureAwait(false);

      _volume = new VolumeResponse();
    }
  }
}
