using GoveeClient.Api.Transport.Requests;
using GoveeClient.Api.Transport.Responses;
using Refit;

namespace GoveeClient.Api.Transport.Client;

internal interface IGoveeCloudApiClient
{
  [Get("/user/devices")]
  Task<DeviceCatalogResponse> GetDevices(CancellationToken cancellationToken = default);

  [Post("/device/state")]
  Task<DeviceStatusResponse> GetDeviceState(
    [Body] DeviceLookupRequest request,
    CancellationToken cancellationToken = default);

  [Post("/device/control")]
  Task<OperationResponse> ControlDevice(
    [Body] DeviceCommandRequest request,
    CancellationToken cancellationToken = default);

  [Post("/device/scenes")]
  Task<SceneCatalogResponse> GetScenes(
    [Body] DeviceLookupRequest request,
    CancellationToken cancellationToken = default);

  [Post("/device/diy-scenes")]
  Task<SceneCatalogResponse> GetDiyScenes(
    [Body] DeviceLookupRequest request,
    CancellationToken cancellationToken = default);
}
