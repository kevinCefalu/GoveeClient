using GoveeClient.Api;
using GoveeClient.Shared.Models;
using GoveeClient.Test.Helpers;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GoveeClient.Test.Api;

[TestClass]
public sealed class CloudApiServiceTests
{
  [TestMethod]
  public void Constructor_WithInvalidBaseUrl_Throws()
  {
    IOptions<ClientOptions> options = Options.Create(new ClientOptions { BaseUrl = "not-a-valid-url" });

    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new CloudApiService(options));

    StringAssert.Contains(exception.Message, "Invalid Govee Cloud API base URL");
  }

  [TestMethod]
  public void Constructor_UsesApiKeyFromOptions()
  {
    Guid expectedApiKey = Guid.NewGuid();

    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = expectedApiKey },
      _ => JsonResponse("""
        { "code": 200, "message": "ok", "data": [] }
        """));

    Assert.AreEqual(expectedApiKey, service.ApiKey);
  }

  [TestMethod]
  public async Task GetDevices_WithoutApiKey_Throws()
  {
    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = null },
      _ => JsonResponse("""
        { "code": 200, "message": "ok", "data": [] }
        """));

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDevices());

    StringAssert.Contains(exception.Message, "API key must be configured");
  }

  [TestMethod]
  public async Task GetDevices_MapsResponseAndSendsApiKeyHeader()
  {
    Guid apiKey = Guid.NewGuid();

    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = apiKey },
      request =>
      {
        Assert.AreEqual("/user/devices", request.RequestUri?.AbsolutePath);
        Assert.AreEqual(apiKey.ToString(), request.Headers.GetValues("Govee-API-Key").Single());

        return JsonResponse("""
          {
            "code": 200,
            "message": "ok",
            "data": [
              {
                "device": "device-1",
                "sku": "H6001",
                "deviceName": "Desk Lamp",
                "type": "devices.types.light",
                "capabilities": [
                  {
                    "type": "devices.capabilities.range",
                    "instance": "brightness",
                    "parameters": {
                      "dataType": "INTEGER",
                      "unit": "unit.percent",
                      "range": {
                        "min": 1,
                        "max": 100,
                        "precision": 1
                      }
                    }
                  },
                  {
                    "type": "devices.capabilities.music_setting",
                    "instance": "musicMode",
                    "parameters": {
                      "dataType": "STRUCT",
                      "fields": [
                        {
                          "fieldName": "musicMode",
                          "dataType": "ENUM",
                          "required": true,
                          "options": [
                            { "name": "Energic", "value": 1 }
                          ]
                        }
                      ]
                    }
                  }
                ]
              }
            ]
          }
          """);
      });

    IReadOnlyList<Device> devices = await service.GetDevices();

    Assert.HasCount(1, devices);
    Assert.AreEqual("device-1", devices[0].DeviceId);
    Assert.AreEqual("H6001", devices[0].Model);
    Assert.AreEqual("Desk Lamp", devices[0].DeviceName);
    Assert.AreEqual(string.Empty, devices[0].Address);
    Assert.AreEqual("devices.types.light", devices[0].DeviceType);
    IReadOnlyList<DeviceCapability>? capabilities = devices[0].Capabilities;
    Assert.IsNotNull(capabilities);
    Assert.HasCount(2, capabilities!);

    DeviceCapability brightnessCapability = capabilities[0];
    Assert.AreEqual("devices.capabilities.range", brightnessCapability.Type);
    Assert.AreEqual("brightness", brightnessCapability.Instance);
    Assert.IsNotNull(brightnessCapability.Parameters);
    Assert.AreEqual("INTEGER", brightnessCapability.Parameters!.DataType);
    Assert.IsNotNull(brightnessCapability.Parameters.Range);
    Assert.AreEqual(1, brightnessCapability.Parameters.Range!.Min);

    DeviceCapability musicCapability = capabilities[1];
    Assert.IsNotNull(musicCapability.Parameters);
    Assert.IsNotNull(musicCapability.Parameters!.Fields);
    Assert.HasCount(1, musicCapability.Parameters.Fields!);

    DeviceCapabilityField musicField = musicCapability.Parameters.Fields[0];
    Assert.AreEqual("musicMode", musicField.FieldName);
    Assert.IsNotNull(musicField.Options);
    Assert.HasCount(1, musicField.Options!);
    Assert.AreEqual("Energic", musicField.Options[0].Name);
    Assert.AreEqual(1, musicField.Options[0].Value.GetInt32());
  }

  [TestMethod]
  public async Task GetDeviceState_MapsCapabilitiesToDeviceState()
  {
    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = Guid.NewGuid() },
      request =>
      {
        Assert.AreEqual("/device/state", request.RequestUri?.AbsolutePath);

        return JsonResponse("""
          {
            "code": 200,
            "msg": "ok",
            "payload": {
              "capabilities": [
                { "type": "devices.capabilities.on_off", "instance": "powerSwitch", "state": { "value": 1 } },
                { "type": "devices.capabilities.range", "instance": "brightness", "state": { "value": "75" } },
                { "type": "devices.capabilities.color_setting", "instance": "colorRgb", "state": { "value": 66051 } },
                { "type": "devices.capabilities.color_setting", "instance": "colorTemperatureK", "state": { "value": 4200 } }
              ]
            }
          }
          """);
      });

    DeviceState? state = await service.GetDeviceState(new Device("device-1", "H6001", "Desk Lamp", "192.168.0.10"));

    Assert.IsNotNull(state);
    Assert.AreEqual(PowerState.On, state.State);
    Assert.AreEqual(75, state.Brightness);
    Assert.IsNotNull(state.Color);
    Assert.AreEqual(1, state.Color.R);
    Assert.AreEqual(2, state.Color.G);
    Assert.AreEqual(3, state.Color.B);
    Assert.AreEqual(4200, state.ColorTempInKelvin);
  }

  [TestMethod]
  public async Task ControlDevice_SendsExpectedPayload()
  {
    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = Guid.NewGuid() },
      async request =>
      {
        Assert.AreEqual("/device/control", request.RequestUri?.AbsolutePath);

        string requestBody = await request.Content!.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(requestBody);

        JsonElement payload = document.RootElement.GetProperty("payload");
        Assert.AreEqual("H6001", payload.GetProperty("sku").GetString());
        Assert.AreEqual("device-1", payload.GetProperty("device").GetString());

        JsonElement capability = payload.GetProperty("capability");
        Assert.AreEqual("devices.capabilities.on_off", capability.GetProperty("type").GetString());
        Assert.AreEqual("powerSwitch", capability.GetProperty("instance").GetString());
        Assert.AreEqual(1, capability.GetProperty("value").GetInt32());

        return await JsonResponse("""
          { "code": 200, "message": "ok", "msg": "ok" }
          """);
      });

    await service.ControlDevice(
      new Device("device-1", "H6001", "Desk Lamp", "192.168.0.10"),
      "devices.capabilities.on_off",
      "powerSwitch",
      1);
  }

  [TestMethod]
  public async Task GetScenes_MapsNumericAndObjectSceneValues()
  {
    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = Guid.NewGuid() },
      _ => JsonResponse("""
        {
          "code": 200,
          "msg": "ok",
          "payload": {
            "capabilities": [
              {
                "type": "devices.capabilities.dynamic_scene",
                "instance": "lightScene",
                "parameters": {
                  "options": [
                    { "name": "Relax", "value": 12 },
                    { "name": "DIY", "value": { "id": 4, "paramId": 9 } },
                    { "name": "Plain", "value": "ignored" }
                  ]
                }
              }
            ]
          }
        }
        """));

    IReadOnlyList<DeviceScene> scenes = await service.GetScenes(new Device("device-1", "H6001", "Desk Lamp", "192.168.0.10"));

    Assert.HasCount(3, scenes);
    Assert.AreEqual(new DeviceScene("Relax", Value: 12), scenes[0]);
    Assert.AreEqual(new DeviceScene("DIY", Id: 4, ParamId: 9), scenes[1]);
    Assert.AreEqual(new DeviceScene("Plain"), scenes[2]);
  }

  [TestMethod]
  public async Task GetDiyScenes_WithApiFailure_Throws()
  {
    CloudApiService service = CreateService(
      new ClientOptions { BaseUrl = "https://example.com", ApiKey = Guid.NewGuid() },
      _ => JsonResponse("""
        { "code": 500, "msg": "server error", "payload": { "capabilities": [] } }
        """));

    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      service.GetDiyScenes(new Device("device-1", "H6001", "Desk Lamp", "192.168.0.10")));

    StringAssert.Contains(exception.Message, "Govee Cloud API returned code 500");
  }

  private static CloudApiService CreateService(
    ClientOptions options,
    Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
  {
    HttpClient httpClient = new(new StubHttpMessageHandler((request, _) => responseFactory(request)))
    {
      BaseAddress = new Uri("https://localhost")
    };

    return new CloudApiService(Options.Create(options), httpClient);
  }

  private static Task<HttpResponseMessage> JsonResponse(string json)
    => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(json, Encoding.UTF8, "application/json")
    });
}
