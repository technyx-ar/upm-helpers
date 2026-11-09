# Changelog

## 0.3.0

### Do not release before the backend deploy

Photos need technyx-backend PR #51 (stacked on PR #9), admin AI needs PR #29. Against a backend that has not deployed them:

- the photo `Task` / `User` filters are ignored and `ListAsync` returns unfiltered lists;
- `UpdatePhotoRequest` no longer carries the stored `meta`, and the old backend replaces `meta` with the extra keys of an update, so **every update without `Extra` wipes the photo's `meta`**.

### Photos

- `PhotoFilter.Task`, `PhotoFilter.User` (ULIDs) and `PhotoFilter.Radius`.
- `PhotoData.User` (always set) and `PhotoData.Task` (`null` for a photo without a task). No numeric ids.
- `PhotoMetadata.Task` links an uploaded photo to a task (`task_ulid`).
- `UpdatePhotoRequest` sends only what was assigned. `Description`, `GpsLat` and `GpsLng` can be updated and cleared by assigning `null`; `Extra` is merged into the stored `meta` and a `null` value removes the key.
- Removed `UpdatePhotoRequest.ForPhoto`, which only existed to work around the old overwrite of `meta`.
- Documented the access rules: colleagues see each other's photos but get `403` on update and delete, photos outside the organization answer `404`.

### Automatic heartbeat

- `TechnyxSdk.Heartbeat` (`HeartbeatService`), opt-in through the new `autoHeartbeat` setting (default `false`; Project Settings toggle). `heartbeatIntervalSeconds` (default 300, 60 to 3600) sets the cadence.
- `Start(deviceId)` sends a heartbeat at once and then one per interval, with a `TelemetryProvider` callback for the payload. With `autoHeartbeat` on, a successful `Devices.RegisterAsync` starts it for the new device.
- Runs only while the user is logged in; stops on logout, on a lost session, on `403` / `404` for the device, on `TechnyxSdk.Configure` and when the app quits.
- `HeartbeatFailed` is raised for every failed attempt (`ApiError`, status, consecutive failures, retry delay, whether the loop stopped); retries back off from 15 s up to the interval, with jitter and `Retry-After`.
- `DeviceService.RegisterAsync` keeps its signature and result; after a successful registration it additionally notifies the heartbeat service (internal event).
- Not built: configuration sync. The backend has no device-side configuration endpoint with versions and no commands in the heartbeat answer; `GetConfigAsync` stays manual, and `HeartbeatService.IntervalSeconds` applies a changed `heartbeat_interval_seconds`.

### Admin AI

- `TechnyxSdk.AiAdmin` (`AiAdminService`): `GetUsageAsync`, `ListAuditAsync`, `GetAuditAsync` for `/api/v1/admin/ai/*`, with typed models (`AiUsageStats`, `AiAuditLogData`, ...), `AiUsageFilter`, `AiAuditFilter` and the `AiAuditAction` constants.
- The `from` / `to` filters go out as UTC calendar dates (`yyyy-MM-dd`), the format these endpoints expect.
- Needs the `view-ai-audit` permission, which the backend does not seed yet: every call answers `403` until a role has it.
- The audit list of a user without an organization has no `last_page` in its `meta` and is reported as `PARSE_ERROR` (backend defect).
