
using GoveeClient.Shared.Models;

namespace GoveeClient.Shared.Services.Api;

public interface ICloudApiService
{
  Guid? ApiKey { get; set; }

  Task<IReadOnlyList<Device>> GetDevices(CancellationToken cancellationToken = default);

  Task<DeviceState?> GetDeviceState(Device device, CancellationToken cancellationToken = default);

  Task ControlDevice(
      Device device,
      string capabilityType,
      string capabilityInstance,
      object? value,
      CancellationToken cancellationToken = default);

  Task<IReadOnlyList<DeviceScene>> GetScenes(Device device, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<DeviceScene>> GetDiyScenes(Device device, CancellationToken cancellationToken = default);
}
