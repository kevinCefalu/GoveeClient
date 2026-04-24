using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoveeClient.Api.Transport.Capabilities;

internal sealed record CapabilityParameterContract(
  [property: JsonPropertyName("dataType")] string? DataType,
  [property: JsonPropertyName("unit")] string? Unit,
  [property: JsonPropertyName("range")] NumericRangeContract? Range,
  [property: JsonPropertyName("options")] CapabilityOptionContract[]? Options,
  [property: JsonPropertyName("fields")] CapabilityFieldContract[]? Fields);

internal sealed record NumericRangeContract(
  [property: JsonPropertyName("min")] int? Min,
  [property: JsonPropertyName("max")] int? Max,
  [property: JsonPropertyName("precision")] int? Precision);

internal sealed record CapabilityFieldContract(
  [property: JsonPropertyName("fieldName")] string FieldName,
  [property: JsonPropertyName("dataType")] string? DataType,
  [property: JsonPropertyName("required")] bool? Required,
  [property: JsonPropertyName("unit")] string? Unit,
  [property: JsonPropertyName("range")] NumericRangeContract? Range,
  [property: JsonPropertyName("size")] NumericRangeContract? Size,
  [property: JsonPropertyName("elementRange")] NumericRangeContract? ElementRange,
  [property: JsonPropertyName("elementType")] string? ElementType,
  [property: JsonPropertyName("options")] CapabilityOptionContract[]? Options);

internal sealed record CapabilityOptionContract(
  [property: JsonPropertyName("name")] string? Name,
  [property: JsonPropertyName("value")] JsonElement Value);

internal sealed record CapabilityStateContract(
  [property: JsonPropertyName("value")] JsonElement Value);

internal sealed record SceneParameterContract(
  [property: JsonPropertyName("options")] SceneOptionContract[] Options);

internal sealed record SceneOptionContract(
  [property: JsonPropertyName("name")] string? Name,
  [property: JsonPropertyName("value")] JsonElement Value);