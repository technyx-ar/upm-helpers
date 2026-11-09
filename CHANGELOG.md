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
