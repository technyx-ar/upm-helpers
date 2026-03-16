# Devices

The `DeviceService` manages AR/VR device registration, heartbeats, and configuration. Access it via `TechnyxSdk.Devices`.

## Device Types

Available device types (use `DeviceType` constants):

| Constant | Value |
|----------|-------|
| `DeviceType.Quest3` | `quest_3` |
| `DeviceType.QuestPro` | `quest_pro` |
| `DeviceType.VisionPro` | `vision_pro` |
| `DeviceType.Hololens2` | `hololens_2` |
| `DeviceType.Pico4` | `pico_4` |
| `DeviceType.Other` | `other` |

## Device Statuses

| Constant | Value | Description |
|----------|-------|-------------|
| `DeviceStatus.Registered` | `registered` | Just registered, no heartbeat yet |
| `DeviceStatus.Active` | `active` | Sending heartbeats |
| `DeviceStatus.Inactive` | `inactive` | Manually deactivated |
| `DeviceStatus.Decommissioned` | `decommissioned` | Retired from service |

## Register a Device

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Devices;
using Technyx.Sdk.Models;

var result = await TechnyxSdk.Devices.RegisterAsync(new RegisterDeviceRequest
{
    Name = "Quest 3 - Sklad A",
    Type = DeviceType.Quest3,
    SerialNumber = "SN-ABC123",
    FirmwareVersion = "1.0.0",
    OsVersion = "14.0",
    AppVersion = "2.1.0",
});

if (result.IsSuccess)
    Debug.Log($"Registered: {result.Data.Id}");
```

### RegisterDeviceRequest Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Name` | string | Yes | Display name |
| `Type` | string | Yes | One of `DeviceType` constants |
| `SerialNumber` | string | Yes | Unique per user |
| `FirmwareVersion` | string | No | Hardware firmware version |
| `OsVersion` | string | No | Operating system version |
| `AppVersion` | string | No | Application version |

## List Devices

Returns a paginated list of the authenticated user's devices.

```csharp
var result = await TechnyxSdk.Devices.ListAsync(
    type: DeviceType.Quest3,     // optional filter
    status: DeviceStatus.Active, // optional filter
    search: "Sklad",             // optional name/serial search
    page: 1                      // page number
);

if (result.IsSuccess)
{
    var page = result.Data;
    Debug.Log($"Showing {page.Data.Count} of {page.Total} devices");

    foreach (var device in page.Data)
        Debug.Log($"{device.Name} ({device.Status})");

    if (page.HasMorePages)
        Debug.Log($"Page {page.CurrentPage} of {page.LastPage}");
}
```

### PaginatedResponse Properties

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `List<T>` | Items on current page |
| `CurrentPage` | int | Current page number |
| `LastPage` | int | Total number of pages |
| `PerPage` | int | Items per page |
| `Total` | int | Total item count |
| `HasMorePages` | bool | `true` if more pages exist |

## Get Device Detail

```csharp
var result = await TechnyxSdk.Devices.GetAsync("01JQDEVICEID00000000000");

if (result.IsSuccess)
{
    var device = result.Data;
    Debug.Log($"{device.Name} - {device.Status}");
    Debug.Log($"Config: {device.Config?.LoggingLevel}");

    if (device.LatestHeartbeat != null)
        Debug.Log($"Battery: {device.LatestHeartbeat.BatteryLevel}%");
}
```

The detail response includes `Config` and `LatestHeartbeat` (the list response omits `Config`).

## Update a Device

```csharp
var result = await TechnyxSdk.Devices.UpdateAsync("01JQDEVICEID00000000000", new UpdateDeviceRequest
{
    Name = "Quest 3 - Lab B",
    Status = DeviceStatus.Inactive,
});
```

To reassign a device to another user, pass the user's ULID:

```csharp
var result = await TechnyxSdk.Devices.UpdateAsync(deviceId, new UpdateDeviceRequest
{
    UserId = "01JQUSERID0000000000000",
});
```

## Delete a Device

Soft-deletes the device. Returns 204 on success.

```csharp
var result = await TechnyxSdk.Devices.DeleteAsync("01JQDEVICEID00000000000");

if (result.IsSuccess)
    Debug.Log("Device deleted.");
```

## Send Heartbeat

Heartbeats update the device's `last_seen_at`, set status to `active`, and store telemetry data.

```csharp
var result = await TechnyxSdk.Devices.SendHeartbeatAsync("01JQDEVICEID00000000000", new HeartbeatRequest
{
    BatteryLevel = 85,
    GpsLat = 48.1486,
    GpsLng = 17.1077,
    ConnectivityStatus = "wifi",  // wifi, cellular, ethernet, offline
    AppVersion = "2.1.0",
    Meta = new Dictionary<string, object>
    {
        { "cpu_temp", 42.5 },
        { "memory_free_mb", 1024 },
    },
});
```

All heartbeat fields are optional. The endpoint is rate-limited to 12 requests per minute.

### HeartbeatRequest Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `BatteryLevel` | int? | No | 0 - 100 |
| `GpsLat` | double? | No | -90 to 90 |
| `GpsLng` | double? | No | -180 to 180 |
| `ConnectivityStatus` | string | No | `wifi`, `cellular`, `ethernet`, `offline` |
| `AppVersion` | string | No | Also updates device's app_version |
| `Meta` | Dictionary | No | Arbitrary key-value telemetry |

## Device Config

Each device has an MDM-lite configuration with defaults assigned on registration.

### Get Config

```csharp
var result = await TechnyxSdk.Devices.GetConfigAsync("01JQDEVICEID00000000000");

if (result.IsSuccess)
{
    var config = result.Data;
    Debug.Log($"Heartbeat interval: {config.HeartbeatIntervalSeconds}s");
    Debug.Log($"GPS tracking: {config.Features.GpsTracking}");
}
```

### Update Config (Partial)

Only set the fields you want to change. Unset fields are preserved on the server.

```csharp
var result = await TechnyxSdk.Devices.UpdateConfigAsync("01JQDEVICEID00000000000",
    new UpdateDeviceConfigRequest
    {
        LoggingLevel = "debug",
        HeartbeatIntervalSeconds = 120,
        Features = new UpdateDeviceFeaturesRequest
        {
            GpsTracking = false,
        },
    });
```

### Config Fields

| Field | Type | Values / Range |
|-------|------|---------------|
| `LoggingLevel` | string | `debug`, `info`, `warning`, `error` |
| `UpdateChannel` | string | `stable`, `beta`, `nightly` |
| `HeartbeatIntervalSeconds` | int | 60 - 3600 |
| `MaxOfflineHours` | int | 1 - 720 |
| `Features.VoiceCommands` | bool | |
| `Features.OfflineMode` | bool | |
| `Features.CameraCapture` | bool | |
| `Features.GpsTracking` | bool | |

## DeviceData Model

```csharp
public class DeviceData
{
    public string Id;               // ULID
    public string Name;
    public string Type;             // DeviceType constant
    public string SerialNumber;
    public string FirmwareVersion;
    public string OsVersion;
    public string AppVersion;
    public string Status;           // DeviceStatus constant
    public DeviceConfig Config;     // null in list responses
    public string LastSeenAt;       // ISO 8601
    public DeviceHeartbeatData LatestHeartbeat;
    public string CreatedAt;        // ISO 8601
    public string UpdatedAt;        // ISO 8601
}
```
