using GoveeClient.Api;
using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services.Api;
using ClientImplementation = GoveeClient.GoveeClient;

namespace GoveeClient.Test;

[TestClass]
public sealed class GoveeClientBehaviorTests
{
  [TestMethod]
  public void ApiKey_GetterFallsBackToCloudServiceValue()
  {
    Guid expectedApiKey = Guid.NewGuid();
    FakeCloudApiService cloudApiService = new() { ApiKey = expectedApiKey };
    ClientImplementation client = new(cloudApiService, new LanApiService());

    Guid? apiKey = client.ApiKey;

    Assert.AreEqual(expectedApiKey, apiKey);
  }

  [TestMethod]
  public void ApiKey_SetterUpdatesCloudService()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Guid expectedApiKey = Guid.NewGuid();

    client.ApiKey = expectedApiKey;

    Assert.AreEqual(expectedApiKey, cloudApiService.ApiKey);
    Assert.AreEqual(expectedApiKey, client.ApiKey);
  }

  [TestMethod]
  public async Task GetDevices_DelegatesToCloudApiService()
  {
    Device[] expectedDevices =
    [
      new("device-1", "H6001", "Desk Lamp", "192.168.0.10"),
      new("device-2", "H6159", "Light Strip", "192.168.0.11")
    ];

    FakeCloudApiService cloudApiService = new()
    {
      DevicesToReturn = expectedDevices
    };

    ClientImplementation client = new(cloudApiService, new LanApiService());

    IReadOnlyList<Device?> devices = (await client.GetDevices()).ToArray();

    CollectionAssert.AreEqual(expectedDevices, devices.Cast<Device>().ToArray());
    Assert.AreEqual(1, cloudApiService.GetDevicesCallCount);
  }

  [TestMethod]
  public async Task GetDeviceState_DelegatesToCloudApiService()
  {
    Device device = new("device-1", "H6001", "Desk Lamp", "192.168.0.10");
    DeviceState expectedState = new(PowerState.On, 75, new RgbColor(1, 2, 3), 4200);

    FakeCloudApiService cloudApiService = new()
    {
      DeviceStateToReturn = expectedState
    };

    ClientImplementation client = new(cloudApiService, new LanApiService());

    DeviceState? state = await client.GetDeviceState(device);

    Assert.AreSame(expectedState, state);
    Assert.AreEqual(device, cloudApiService.LastDeviceStateRequest);
  }

  [TestMethod]
  public async Task GetDeviceState_WithNullDevice_Throws()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());

    await Assert.ThrowsAsync<ArgumentNullException>(() => client.GetDeviceState(null!));
  }

  [TestMethod]
  public async Task SetPowerState_DelegatesToCloudApiService()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = CreateControllableDevice();

    await client.SetPowerState(device, PowerState.On);

    Assert.AreEqual(device, cloudApiService.LastControlDevice);
    Assert.AreEqual("devices.capabilities.on_off", cloudApiService.LastCapabilityType);
    Assert.AreEqual("powerSwitch", cloudApiService.LastCapabilityInstance);
    Assert.AreEqual(1, cloudApiService.LastControlValue);
  }

  [TestMethod]
  public async Task SetBrightness_DelegatesToCloudApiService()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = CreateControllableDevice();

    await client.SetBrightness(device, 65);

    Assert.AreEqual(device, cloudApiService.LastControlDevice);
    Assert.AreEqual("devices.capabilities.range", cloudApiService.LastCapabilityType);
    Assert.AreEqual("brightness", cloudApiService.LastCapabilityInstance);
    Assert.AreEqual(65, cloudApiService.LastControlValue);
  }

  [TestMethod]
  public async Task SetColor_DelegatesRgbValueToCloudApiService()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = CreateControllableDevice();

    await client.SetColor(device, new RgbColor(1, 2, 3));

    Assert.AreEqual(device, cloudApiService.LastControlDevice);
    Assert.AreEqual("devices.capabilities.color_setting", cloudApiService.LastCapabilityType);
    Assert.AreEqual("colorRgb", cloudApiService.LastCapabilityInstance);
    Assert.AreEqual(66051, cloudApiService.LastControlValue);
  }

  [TestMethod]
  public async Task GetScenes_IncludesDiyScenes_WhenRequested()
  {
    DeviceScene[] scenes = [new("Relax", Value: 1)];
    DeviceScene[] diyScenes = [new("DIY", Id: 2, ParamId: 3)];

    FakeCloudApiService cloudApiService = new()
    {
      ScenesToReturn = scenes,
      DiyScenesToReturn = diyScenes
    };

    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = CreateControllableDevice();

    IReadOnlyList<DeviceScene> results = await client.GetScenes(device);

    Assert.HasCount(2, results);
    CollectionAssert.AreEqual(new[] { scenes[0], diyScenes[0] }, results.ToArray());
  }

  [TestMethod]
  public async Task ActivateScene_DelegatesStructuredSceneToCloudApiService()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = CreateControllableDevice();

    await client.ActivateScene(device, new DeviceScene("DIY", Id: 12, ParamId: 34));

    Assert.AreEqual(device, cloudApiService.LastControlDevice);
    Assert.AreEqual("devices.capabilities.dynamic_scene", cloudApiService.LastCapabilityType);
    Assert.AreEqual("lightScene", cloudApiService.LastCapabilityInstance);
    Assert.IsNotNull(cloudApiService.LastControlValue);
  }

  [TestMethod]
  public async Task SetBrightness_WithUnsupportedCapability_Throws()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = new("device-1", "H6001", "Desk Lamp", "192.168.0.10", Capabilities: []);

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SetBrightness(device, 65));

    StringAssert.Contains(exception.Message, "capability metadata");
  }

  [TestMethod]
  public async Task SetBrightness_OutsideDeviceRange_Throws()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = CreateControllableDevice(brightnessMax: 50);

    ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.SetBrightness(device, 65));

    StringAssert.Contains(exception.Message, "at most 50");
  }

  [TestMethod]
  public async Task ActivateScene_WithUnsupportedCapability_Throws()
  {
    FakeCloudApiService cloudApiService = new();
    ClientImplementation client = new(cloudApiService, new LanApiService());
    Device device = new(
      "device-1",
      "H6001",
      "Desk Lamp",
      "192.168.0.10",
      Capabilities:
      [
        new DeviceCapability("devices.capabilities.on_off", "powerSwitch")
      ]);

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      client.ActivateScene(device, new DeviceScene("Relax", Value: 1)));

    StringAssert.Contains(exception.Message, "does not support 'lightScene'");
  }

  private static Device CreateControllableDevice(int brightnessMin = 1, int brightnessMax = 100)
    => new(
      "device-1",
      "H6001",
      "Desk Lamp",
      "192.168.0.10",
      Capabilities:
      [
        new DeviceCapability("devices.capabilities.on_off", "powerSwitch"),
        new DeviceCapability(
          "devices.capabilities.range",
          "brightness",
          new DeviceCapabilityParameters(Range: new DeviceCapabilityRange(brightnessMin, brightnessMax, 1))),
        new DeviceCapability(
          "devices.capabilities.color_setting",
          "colorRgb",
          new DeviceCapabilityParameters(Range: new DeviceCapabilityRange(0, 16777215, 1))),
        new DeviceCapability("devices.capabilities.dynamic_scene", "lightScene")
      ]);

  private sealed class FakeCloudApiService : ICloudApiService
  {
    public Guid? ApiKey { get; set; }

    public IReadOnlyList<Device> DevicesToReturn { get; init; } = [];

    public DeviceState? DeviceStateToReturn { get; init; }

    public IReadOnlyList<DeviceScene> ScenesToReturn { get; init; } = [];

    public IReadOnlyList<DeviceScene> DiyScenesToReturn { get; init; } = [];

    public int GetDevicesCallCount { get; private set; }

    public Device? LastDeviceStateRequest { get; private set; }

    public Device? LastControlDevice { get; private set; }

    public string? LastCapabilityType { get; private set; }

    public string? LastCapabilityInstance { get; private set; }

    public object? LastControlValue { get; private set; }

    public Task ControlDevice(Device device, string capabilityType, string capabilityInstance, object? value, CancellationToken cancellationToken = default)
    {
      LastControlDevice = device;
      LastCapabilityType = capabilityType;
      LastCapabilityInstance = capabilityInstance;
      LastControlValue = value;
      return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeviceScene>> GetDiyScenes(Device device, CancellationToken cancellationToken = default)
      => Task.FromResult(DiyScenesToReturn);

    public Task<IReadOnlyList<Device>> GetDevices(CancellationToken cancellationToken = default)
    {
      GetDevicesCallCount++;
      return Task.FromResult(DevicesToReturn);
    }

    public Task<DeviceState?> GetDeviceState(Device device, CancellationToken cancellationToken = default)
    {
      LastDeviceStateRequest = device;
      return Task.FromResult(DeviceStateToReturn);
    }

    public Task<IReadOnlyList<DeviceScene>> GetScenes(Device device, CancellationToken cancellationToken = default)
      => Task.FromResult(ScenesToReturn);
  }
}
