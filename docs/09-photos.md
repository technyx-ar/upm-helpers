# Photos

`TechnyxSdk.Photos` (`PhotoService`). Photo ids are ULID strings. Every call returns `ApiResponse<T>` (see [HTTP Client](05-http-client.md)) and takes an optional `CancellationToken`.

> **Backend requirement:** this page describes the contract of technyx-backend PR #51 (photo access rules, ULID filters, merge-style update), stacked on PR #9 (media API rework). Against a backend that has not deployed them, the `Task` / `User` filters are ignored and return unfiltered lists, and an update that carries no `Extra` replaces the stored `meta` with an empty one. Do not ship an app built on this SDK version before the backend deploy.

## Who Sees What

- A photo is visible to the user who uploaded it and to every user of the same organization. A `super_admin` sees all photos. A user without an organization sees only their own.
- A photo the caller cannot see answers `404` (`NOT_FOUND`) on get, update and delete, so its existence is not revealed.
- Only the uploader (or a `super_admin`) can update or delete a photo. A colleague of the same organization gets `403` (`FORBIDDEN`).

## Upload

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Models;

var jpeg = File.ReadAllBytes(path);

var result = await TechnyxSdk.Photos.UploadAsync(jpeg, "inspection-42.jpg", "image/jpeg", new PhotoMetadata
{
    Name = "Inspection 42",
    Description = "Left wing, leading edge",
    Task = "01JQ6Z8M3K4N5P7R9S0T1V2W4Y",
    Timestamp = DateTimeOffset.UtcNow,
    GpsLat = 48.1486,
    GpsLng = 17.1077,
    Tags = new List<string> { "wing", "inspection" },
    Extra = new Dictionary<string, object> { { "shift", "night" } },
});

if (result.IsSuccess)
    Debug.Log(result.Data.Url);
```

- `metadata` is optional; unset fields are not sent. `Extra` keys land in the photo's `meta` and cannot reuse the names of the fields above.
- `Task` is the ULID of a task of the caller's organization. Any other task (unknown, deleted, another organization) is answered with `422` and a field error on `metadata.task_ulid`. A `super_admin` may use a task of any organization.
- The file must be an image: the content type has to start with `image/`. Empty files, a missing file name or content type, a content type that is not an image, and files over `PhotoService.MaxPhotoBytes` (50 MB) are rejected before anything is sent with a `VALIDATION_ERROR` (`StatusCode` `0`, error on the `file` field).
- The upload is a `multipart/form-data` POST under `uploadTimeoutSeconds`. The whole image is held in memory while it is built and sent, because `UnityWebRequest` cannot stream a multipart body and the backend has no raw-upload endpoint. Downscale or compress camera frames first, especially on Quest and Pico.
- `Timestamp` is sent as a UTC instant.

> **Server limit:** the production proxy accepts request bodies up to 60 MB (raised on 2026-11-09, it used to reject anything above 2 MB with `413` `PAYLOAD_TOO_LARGE`). The backend itself rejects images above 50 MB.

## List and Filter

```csharp
var page = await TechnyxSdk.Photos.ListAsync(1, new PhotoFilter
{
    Task = "01JQ6Z8M3K4N5P7R9S0T1V2W4Y",
    TimestampFrom = DateTimeOffset.UtcNow.AddDays(-7),
    Meta = new Dictionary<string, string> { { "shift", "night" } },
});

foreach (var photo in page.Data.Data)
    Debug.Log($"{photo.Name}: {photo.Preview}");
```

The backend returns 30 photos per page, newest first; there is no `perPage`. `PhotoFilter` fields:

| Field | Meaning |
|-------|---------|
| `Name`, `Description` | Substring match |
| `Task`, `User` | ULID of the task / of the uploading user. Anything that is not a 26 character string (a numeric id, for example) is answered with `422` and a field error on `task` / `user`. An unknown ULID returns an empty list. |
| `GpsLat`, `GpsLng` | Exact match on the photo's coordinates. With PR #9 both together become the centre of a radius search (1000 m unless `Radius` is set). |
| `Radius` | Meters around `GpsLat` / `GpsLng`. Only PR #9 reads it, older backends ignore it. |
| `Timestamp`, `TimestampFrom`, `TimestampTo` | Exact instant / inclusive range, sent as UTC |
| `Meta` | Exact match on `meta` keys |

Unset fields are not sent. `GetAsync(photoId)` returns one photo.

### `PhotoData`

`Id`, `Name`, `User` (ULID of the uploader, always present), `Task` (ULID or `null` when the photo belongs to no task), `Tags` (`PhotoTagData`: `Id`, `Name`), `Url`, `Preview`, `Description`, `GpsLat`, `GpsLng`, `Timestamp`, `Meta` (the `Extra` keys from the upload), `CreatedAt`, `UpdatedAt`. Dates are the backend's ISO 8601 strings. The backend sends an empty `meta` as `[]`; the SDK reads that as an empty dictionary. The backend never sends numeric ids.

## Update

```csharp
var request = new UpdatePhotoRequest
{
    Name = "Inspection 42 (rechecked)",
    Description = null,                      // clears the description
    GpsLat = 48.15,
    Tags = new List<string> { "wing" },
    Extra = new Dictionary<string, object>
    {
        { "floor", 4 },                      // changes this key
        { "room", null },                    // removes this key
    },
};

var updated = await TechnyxSdk.Photos.UpdateAsync(photo.Id, request);
```

The request sends only what you changed:

- **`Name`**: `null` or empty keeps the current name; the backend cannot clear it.
- **`Description`, `GpsLat`, `GpsLng`**: sent once assigned. Assigning `null` clears the stored value, so `new UpdatePhotoRequest { Description = null }` is not the same as `new UpdatePhotoRequest()`. Leave them unassigned to keep the value.
- **`Tags`**: `null` keeps the current tags, an empty list removes them all, any other list replaces them.
- **`Extra`**: merged into the stored `meta`. Keys you do not send stay, a `null` value removes that key. Reserved names (`name`, `description`, `tags`, `timestamp`, `gps_lat`, `gps_lng`, `task_ulid`) throw `ArgumentException`.

The timestamp, the task and the image itself cannot be changed by an update.

## Delete

```csharp
var deleted = await TechnyxSdk.Photos.DeleteAsync(photo.Id);
```
