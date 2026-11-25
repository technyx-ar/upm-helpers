# Teams & Organizations

`TechnyxSdk.Teams` (`TeamService`) and `TechnyxSdk.Organizations` (`OrganizationService`). All ids are ULID strings. Every call returns `ApiResponse<T>` (see [HTTP Client](05-http-client.md)) and takes an optional `CancellationToken`. What the signed-in user may read or change is decided by the backend; a refusal arrives as an `ApiError`.

## Teams

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Models;

// List (paginated)
var list = await TechnyxSdk.Teams.ListAsync(page: 1, perPage: 20);
if (list.IsSuccess)
    foreach (var team in list.Data.Data)
        Debug.Log($"{team.Name} ({team.MembersCount} members)");

// Single team – the only call that fills Members
var one = await TechnyxSdk.Teams.GetAsync(teamId);

// Create
var created = await TechnyxSdk.Teams.CreateAsync(new CreateTeamRequest
{
    Name = "Line maintenance",
    Description = "Hangar 2",
    ManagerId = managerUserId,   // ULID of a user of the same organization
});

// Update
var updated = await TechnyxSdk.Teams.UpdateAsync(teamId, new UpdateTeamRequest { Name = "Base maintenance" }
    .ClearDescription()
    .SetManager(newManagerId));

// Delete
await TechnyxSdk.Teams.DeleteAsync(teamId);
```

### `TeamData`

| Field | Type | Notes |
|-------|------|-------|
| `Id`, `Name`, `Description`, `CreatedAt` | string | `CreatedAt` is the backend's ISO 8601 string |
| `Manager` | `TeamManagerData` | `Id`, `Name`; `null` when the team has no manager |
| `Members` | `List<TeamMemberData>` | `Id`, `Name`, `Role`, `JoinedAt`. Only `GetAsync` fills it; list and write calls leave it `null` |
| `MembersCount` | int? | |

### Partial update

The backend treats a missing `description` or `manager_id` as "unchanged" and an explicit `null` as "clear". A plain C# `null` cannot tell those apart, so `UpdateTeamRequest` has `Name` as a field and the other two as methods:

| Call | Effect |
|------|--------|
| `Name = "x"` | rename; `null` leaves the name |
| `SetDescription("x")` / `ClearDescription()` | set / remove the description |
| `SetManager(userId)` / `ClearManager()` | set / remove the manager |
| not called | unchanged |

## Organizations

```csharp
// List, optionally by status
var orgs = await TechnyxSdk.Organizations.ListAsync(OrganizationStatus.Active);

var org = await TechnyxSdk.Organizations.GetAsync(organizationId);

var created = await TechnyxSdk.Organizations.CreateAsync(new CreateOrganizationRequest
{
    Name = "Acme Aviation",
    Settings = new OrganizationSettings
    {
        InvoicePrefix = "ACM",
        DefaultLanguage = LanguageCodes.English,
        InactivityTimeout = 30,   // minutes, 5 to 480
    },
});

// Only the settings keys that are set change; the rest stay as stored
await TechnyxSdk.Organizations.UpdateAsync(organizationId, new UpdateOrganizationRequest
{
    Settings = new OrganizationSettings { InactivityTimeout = 60 },
});

await TechnyxSdk.Organizations.DeleteAsync(organizationId);
```

`OrganizationStatus` has `Active`, `Inactive` and `Suspended`. `LanguageCodes` has `Slovak` (`sk`), `English` (`en`) and `Czech` (`cs`).

### `OrganizationData`

`Id`, `Name`, `Slug`, `Status`, `Settings` (`OrganizationSettings`), `Logo` (public URL, `null` without a logo), `UsersCount`, `TeamsCount`, `CreatedAt`, `UpdatedAt`.

`OrganizationSettings.Extra` holds stored settings keys this SDK version does not know, so they survive a read and write round trip. The backend sends an empty settings object as `[]`; the SDK reads that as empty settings.

### Logo

```csharp
var bytes = File.ReadAllBytes(path);
var result = await TechnyxSdk.Organizations.UploadLogoAsync(organizationId, bytes, "logo.png", "image/png");
```

The logo is limited to `OrganizationService.MaxLogoBytes` (2 MB). An empty file, a missing name or content type, or a file over the limit is rejected before anything is sent: the result is a `VALIDATION_ERROR` with `StatusCode` `0` and an error on the `logo` field. The upload is a `multipart/form-data` POST with `_method=PATCH`, because PHP parses multipart bodies only on POST, and runs under `uploadTimeoutSeconds`. The file is held in memory while it is sent.

> **Server limit:** the production nginx rejects request bodies of about 1 MB with `413` (`PAYLOAD_TOO_LARGE`) while PHP accepts more. Logos and photos above that size fail until the proxy limit is raised on the server.
