# Photos

`TechnyxSdk.Photos` (`PhotoService`). Photo ids are ULID strings. Every call returns `ApiResponse<T>` (see [HTTP Client](05-http-client.md)) and takes an optional `CancellationToken`.

## Upload

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Models;

var jpeg = File.ReadAllBytes(path);

var result = await TechnyxSdk.Photos.UploadAsync(jpeg, "inspection-42.jpg", "image/jpeg", new PhotoMetadata
{
    Name = "Inspection 42",
    Description = "Left wing, leading edge",
    Timestamp = DateTimeOffset.UtcNow,
    GpsLat = 48.1486,
    GpsLng = 17.1077,
    Tags = new List<string> { "wing", "inspection" },
    Extra = new Dictionary<string, object> { { "task", "T-1042" } },
});

if (result.IsSuccess)
    Debug.Log(result.Data.Url);
```

- `metadata` is optional; unset fields are not sent. `Extra` keys land in the photo's `meta` and cannot reuse the names of the fields above.
- The file must be an image: the content type has to start with `image/`. Empty files, a missing file name or content type, a content type that is not an image, and files over `PhotoService.MaxPhotoBytes` (50 MB) are rejected before anything is sent with a `VALIDATION_ERROR` (`StatusCode` `0`, error on the `file` field).
- The upload is a `multipart/form-data` POST under `uploadTimeoutSeconds`. The whole image is held in memory while it is built and sent, because `UnityWebRequest` cannot stream a multipart body and the backend has no raw-upload endpoint. Downscale or compress camera frames first, especially on Quest and Pico.
- `Timestamp` is sent as a UTC instant.

> **Server limit:** the production nginx rejects request bodies of about 1 MB with `413` (`PAYLOAD_TOO_LARGE`) while PHP accepts more. Larger photos fail until the proxy limit is raised on the server.

## List and Filter

```csharp
var page = await TechnyxSdk.Photos.ListAsync(1, new PhotoFilter
{
    Name = "inspection",
    TimestampFrom = DateTimeOffset.UtcNow.AddDays(-7),
    Meta = new Dictionary<string, string> { { "task", "T-1042" } },
});

foreach (var photo in page.Data.Data)
    Debug.Log($"{photo.Name}: {photo.Preview}");
```

The backend returns 30 photos per page, newest first; there is no `perPage`. `PhotoFilter` fields: `Name` and `Description` (substring), `GpsLat`, `GpsLng`, `Timestamp`, `TimestampFrom`, `TimestampTo`, `Meta` (exact match on `meta` keys). Unset fields are not sent.

`GetAsync(photoId)` returns one photo.

### `PhotoData`

`Id`, `Name`, `Tags` (`PhotoTagData`: `Id`, `Name`), `Url`, `Preview`, `Description`, `GpsLat`, `GpsLng`, `Timestamp`, `Meta` (the `Extra` keys from the upload), `CreatedAt`, `UpdatedAt`. Dates are the backend's ISO 8601 strings. The backend sends an empty `meta` as `[]`; the SDK reads that as an empty dictionary.

## Update

```csharp
var request = UpdatePhotoRequest.ForPhoto(photo);   // carries the current meta
request.Name = "Inspection 42 (rechecked)";
request.Tags = new List<string> { "wing" };

var updated = await TechnyxSdk.Photos.UpdateAsync(photo.Id, request);
```

Three behaviors of the backend shape this API:

1. **`meta` is overwritten, not merged.** The stored `meta` is replaced by the extra keys of the update. `new UpdatePhotoRequest { Name = "x" }` therefore empties it. Start from `UpdatePhotoRequest.ForPhoto(photo)` unless you want that.
2. **The description cannot be cleared.** An empty `Description` leaves the current one in place.
3. **Tags:** `Tags = null` keeps the current tags, an empty list removes them all.

GPS coordinates cannot be changed with an update; the backend ignores them, so `UpdatePhotoRequest` does not have those fields.

## Delete

```csharp
var deleted = await TechnyxSdk.Photos.DeleteAsync(photo.Id);
```
