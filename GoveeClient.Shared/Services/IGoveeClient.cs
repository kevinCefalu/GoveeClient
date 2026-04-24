using GoveeClient.Shared.Models;

namespace GoveeClient.Shared.Services;

public interface IGoveeClient
{
  /// <summary>
  /// Govee Api Key
  /// </summary>
  Guid? ApiKey { get; set; }

  /// <summary>
  /// Gets a List of Govee Devices
  /// </summary>
  /// <param name="onlyLan">If true returns that are available on Api and Lan</param>
  /// <returns>List of Govee Devices</returns>
  Task<IEnumerable<Device?>> GetDevices(bool onlyLan = true);

  /// <summary>
  /// Gets the State of a Device
  /// </summary>
  /// <param name="device">Device</param>
  /// <param name="useUdp">Use Udp Connection instead of the Api</param>
  /// <returns></returns>
  Task<DeviceState?> GetDeviceState(Device device, bool useUdp = true);

  Task SetPowerState(Device device, PowerState powerState, bool useUdp = true);

  Task SetBrightness(Device device, int brightness, bool useUdp = true);

  Task SetColor(Device device, RgbColor color, bool useUdp = true);

  Task<IReadOnlyList<DeviceScene>> GetScenes(Device device, bool includeDiyScenes = true, bool useUdp = true);

  Task ActivateScene(Device device, DeviceScene scene, bool useUdp = true);
}
