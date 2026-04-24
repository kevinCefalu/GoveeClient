
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoveeClient.Shared.Models;

public record Device(
  [property: JsonPropertyName("device")] string DeviceId,
  string Model,
  string DeviceName,
  string Address,
  string DeviceType = "",
  IReadOnlyList<DeviceCapability>? Capabilities = null);

public record DeviceCapability(
  string Type,
  string Instance,
  DeviceCapabilityParameters? Parameters = null);

public record DeviceCapabilityParameters(
  string? DataType = null,
  string? Unit = null,
  DeviceCapabilityRange? Range = null,
  IReadOnlyList<DeviceCapabilityOption>? Options = null,
  IReadOnlyList<DeviceCapabilityField>? Fields = null);

public record DeviceCapabilityRange(
  int? Min = null,
  int? Max = null,
  int? Precision = null);

public record DeviceCapabilityField(
  string FieldName,
  string? DataType = null,
  bool? Required = null,
  string? Unit = null,
  DeviceCapabilityRange? Range = null,
  DeviceCapabilityRange? Size = null,
  DeviceCapabilityRange? ElementRange = null,
  string? ElementType = null,
  IReadOnlyList<DeviceCapabilityOption>? Options = null);

public record DeviceCapabilityOption(
  string Name,
  JsonElement Value);

public class GoveeUdpDevice
{
  public string ip { get; set; } = string.Empty;
  public string device { get; set; } = string.Empty;
  public string sku { get; set; } = string.Empty;
  public string bleVersionHard { get; set; } = string.Empty;
  public string bleVersionSoft { get; set; } = string.Empty;
  public string wifiVersionHard { get; set; } = string.Empty;
  public string wifiVersionSoft { get; set; } = string.Empty;
}

public enum PowerState
{
  Off = 0,
  On = 1
}

public class RgbColor(int r, int g, int b)
{
  public short R { get; set; } = Convert.ToInt16(r);
  public short G { get; set; } = Convert.ToInt16(g);
  public short B { get; set; } = Convert.ToInt16(b);
}

public record DeviceScene(
  string Name,
  int? Value = null,
  int? Id = null,
  int? ParamId = null);

public record DeviceState(
  PowerState State, int Brightness,
  RgbColor? Color, int ColorTempInKelvin);
