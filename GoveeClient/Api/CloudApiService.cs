using GoveeClient.Api.Transport.Capabilities;
using GoveeClient.Api.Transport.Client;
using GoveeClient.Api.Transport.Common;
using GoveeClient.Api.Transport.Requests;
using GoveeClient.Api.Transport.Responses;
using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services.Api;
using Microsoft.Extensions.Options;
using Refit;
using System.Text.Json;

namespace GoveeClient.Api;

public sealed class CloudApiService : ICloudApiService
{
  private readonly ClientOptions _clientOptions;
  private readonly HttpClient _httpClient;
  private readonly IGoveeCloudApiClient _apiClient;

  public CloudApiService(IOptions<ClientOptions> clientOptions)
    : this(clientOptions, CreateHttpClient(clientOptions))
  {
  }

  internal CloudApiService(IOptions<ClientOptions> clientOptions, HttpClient httpClient)
  {
    ArgumentNullException.ThrowIfNull(clientOptions);
    ArgumentNullException.ThrowIfNull(httpClient);

    _clientOptions = clientOptions.Value;
    _httpClient = httpClient;
    ApiKey = _clientOptions.ApiKey;

    _apiClient = CreateApiClient(httpClient);
  }

  public Guid? ApiKey { get; set; }

  public async Task<IReadOnlyList<Device>> GetDevices(CancellationToken cancellationToken = default)
  {
    ApplyApiKeyHeader();

    DeviceCatalogResponse response = await _apiClient.GetDevices(cancellationToken);
    EnsureSuccess(response);

    return [.. response.Data
          .Select(device => new Device(
            device.DeviceId,
            device.Sku,
            device.Name ?? device.Sku,
            string.Empty,
            device.DeviceType ?? string.Empty,
            MapDeviceCapabilities(device.Capabilities)))];
  }

  public async Task<DeviceState?> GetDeviceState(Device device, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(device);

    ApplyApiKeyHeader();

    DeviceStatusResponse response = await _apiClient.GetDeviceState(
      CreateDeviceRequest(device),
      cancellationToken);

    EnsureSuccess(response);

    StateCapabilityContract[] capabilities = response.Payload?.Capabilities ?? [];
    if (capabilities.Length == 0)
    {
      return null;
    }

    PowerState powerState = GetEnumValue(capabilities, "devices.capabilities.on_off", "powerSwitch") == 1
      ? PowerState.On
      : PowerState.Off;

    int brightness = GetEnumValue(capabilities, "devices.capabilities.range", "brightness");
    int colorValue = GetEnumValue(capabilities, "devices.capabilities.color_setting", "colorRgb");
    int colorTemp = GetEnumValue(capabilities, "devices.capabilities.color_setting", "colorTemperatureK");

    return new DeviceState(
      powerState,
      brightness,
      colorValue > 0 ? ToRgbColor(colorValue) : null,
      colorTemp);
  }

  public async Task ControlDevice(
    Device device,
    string capabilityType,
    string capabilityInstance,
    object? value,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentException.ThrowIfNullOrWhiteSpace(capabilityType);
    ArgumentException.ThrowIfNullOrWhiteSpace(capabilityInstance);

    ApplyApiKeyHeader();

    OperationResponse response = await _apiClient.ControlDevice(
      new DeviceCommandRequest(
        Guid.NewGuid().ToString(),
        new DeviceCommandBody(
          device.Model,
          device.DeviceId,
          new CommandCapability(capabilityType, capabilityInstance, value))),
      cancellationToken);

    EnsureSuccess(response);
  }

  public async Task<IReadOnlyList<DeviceScene>> GetScenes(Device device, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(device);

    ApplyApiKeyHeader();

    SceneCatalogResponse response = await _apiClient.GetScenes(
      CreateDeviceRequest(device),
      cancellationToken);

    EnsureSuccess(response);

    return MapScenes(response.Payload?.Capabilities);
  }

  public async Task<IReadOnlyList<DeviceScene>> GetDiyScenes(Device device, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(device);

    ApplyApiKeyHeader();

    SceneCatalogResponse response = await _apiClient.GetDiyScenes(
      CreateDeviceRequest(device),
      cancellationToken);

    EnsureSuccess(response);

    return MapScenes(response.Payload?.Capabilities);
  }

  private void ApplyApiKeyHeader()
  {
    Guid? apiKey = (ApiKey ?? _clientOptions.ApiKey) ?? throw new InvalidOperationException("A Govee Cloud API key must be configured before calling the cloud API.");

    _httpClient.DefaultRequestHeaders.Authorization = null;
    _httpClient.DefaultRequestHeaders.Remove("Govee-API-Key");
    _httpClient.DefaultRequestHeaders.Add("Govee-API-Key", apiKey.Value.ToString());
  }

  private static DeviceLookupRequest CreateDeviceRequest(Device device)
    => new(
      Guid.NewGuid().ToString(),
      new DeviceLookupTarget(device.Model, device.DeviceId));

  private static void EnsureSuccess(ITransportResponse response)
  {
    if (response.Code != 200)
    {
      throw new InvalidOperationException($"Govee Cloud API returned code {response.Code}: {response.EffectiveMessage ?? "unknown error"}");
    }
  }

  private static int GetEnumValue(IEnumerable<StateCapabilityContract> capabilities, string type, string instance)
  {
    StateCapabilityContract? capability = capabilities.FirstOrDefault(candidate =>
      string.Equals(candidate.Type, type, StringComparison.OrdinalIgnoreCase)
      && string.Equals(candidate.Instance, instance, StringComparison.OrdinalIgnoreCase));

    if (capability?.State?.Value is not JsonElement value)
    {
      return 0;
    }

    return value.ValueKind switch
    {
      JsonValueKind.Number when value.TryGetInt32(out int number) => number,
      JsonValueKind.String when int.TryParse(value.GetString(), out int number) => number,
      _ => 0
    };
  }

  private static RgbColor ToRgbColor(int rgb)
    => new(
      (rgb >> 16) & 0xFF,
      (rgb >> 8) & 0xFF,
      rgb & 0xFF);

  private static IReadOnlyList<DeviceScene> MapScenes(IEnumerable<SceneCapabilityContract>? capabilities)
  {
    if (capabilities is null)
    {
      return [];
    }

    return [.. capabilities
      .SelectMany(capability => capability.Parameters?.Options ?? [])
      .Select(option =>
      {
        if (option.Value.ValueKind == JsonValueKind.Number && option.Value.TryGetInt32(out int value))
        {
          return new DeviceScene(option.Name ?? string.Empty, Value: value);
        }

        if (option.Value.ValueKind == JsonValueKind.Object)
        {
          int? id = option.Value.TryGetProperty("id", out JsonElement idElement) && idElement.TryGetInt32(out int parsedId)
            ? parsedId
            : null;

          int? paramId = option.Value.TryGetProperty("paramId", out JsonElement paramIdElement) && paramIdElement.TryGetInt32(out int parsedParamId)
            ? parsedParamId
            : null;

          return new DeviceScene(option.Name ?? string.Empty, Id: id, ParamId: paramId);
        }

        return new DeviceScene(option.Name ?? string.Empty);
      })];
  }

  private static IReadOnlyList<DeviceCapability> MapDeviceCapabilities(IEnumerable<DeviceCapabilityContract>? capabilities)
  {
    return capabilities is null
      ? []
      : [.. capabilities.Select(capability => new DeviceCapability(
      capability.Type,
      capability.Instance,
      MapParameters(capability.Parameters)))];
  }

  private static DeviceCapabilityParameters? MapParameters(CapabilityParameterContract? parameters)
    => parameters is null
      ? null
      : new DeviceCapabilityParameters(
        parameters.DataType,
        parameters.Unit,
        MapRange(parameters.Range),
        MapOptions(parameters.Options),
        MapFields(parameters.Fields));

  private static IReadOnlyList<DeviceCapabilityField> MapFields(IEnumerable<CapabilityFieldContract>? fields)
  {
    if (fields is null)
    {
      return [];
    }

    return [.. fields.Select(field => new DeviceCapabilityField(
      field.FieldName,
      field.DataType,
      field.Required,
      field.Unit,
      MapRange(field.Range),
      MapRange(field.Size),
      MapRange(field.ElementRange),
      field.ElementType,
      MapOptions(field.Options)))];
  }

  private static IReadOnlyList<DeviceCapabilityOption> MapOptions(IEnumerable<CapabilityOptionContract>? options)
  {
    if (options is null)
    {
      return [];
    }

    return [.. options.Select(option => new DeviceCapabilityOption(option.Name ?? string.Empty, option.Value))];
  }

  private static DeviceCapabilityRange? MapRange(NumericRangeContract? range)
    => range is null ? null : new DeviceCapabilityRange(range.Min, range.Max, range.Precision);

  private static HttpClient CreateHttpClient(IOptions<ClientOptions> clientOptions)
  {
    ArgumentNullException.ThrowIfNull(clientOptions);

    if (!Uri.TryCreate(clientOptions.Value.BaseUrl, UriKind.Absolute, out Uri? baseUri))
    {
      throw new InvalidOperationException($"Invalid Govee Cloud API base URL '{clientOptions.Value.BaseUrl}'.");
    }

    return new HttpClient { BaseAddress = baseUri };
  }

  private static IGoveeCloudApiClient CreateApiClient(HttpClient httpClient)
    => RestService.For<IGoveeCloudApiClient>(
      httpClient,
      new RefitSettings
      {
        ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web))
      });

}
