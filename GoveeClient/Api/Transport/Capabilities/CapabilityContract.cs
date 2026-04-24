namespace GoveeClient.Api.Transport.Capabilities;

internal abstract record CapabilityContract(
  string Type,
  string Instance);

internal sealed record DeviceCapabilityContract(
  string Type,
  string Instance,
  CapabilityParameterContract? Parameters = null) : CapabilityContract(Type, Instance);

internal sealed record SceneCapabilityContract(
  string Type,
  string Instance,
  SceneParameterContract? Parameters = null) : CapabilityContract(Type, Instance);

internal sealed record StateCapabilityContract(
  string Type,
  string Instance,
  CapabilityStateContract? State = null) : CapabilityContract(Type, Instance);