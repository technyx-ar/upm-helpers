# HTTP Client

The `ApiClient` provides typed async HTTP methods. Access it via `TechnyxSdk.Http`. It automatically attaches the Bearer token and handles the `{ "data": ... }` response envelope.

## Making Requests

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Http;

// GET
var response = await TechnyxSdk.Http.GetAsync<MyModel>("some/endpoint");

// POST
var response = await TechnyxSdk.Http.PostAsync<MyModel>("some/endpoint", new { name = "test" });

// PUT
var response = await TechnyxSdk.Http.PutAsync<MyModel>("some/endpoint", requestBody);

// PATCH
var response = await TechnyxSdk.Http.PatchAsync<MyModel>("some/endpoint", partialData);

// DELETE
var response = await TechnyxSdk.Http.DeleteAsync("some/endpoint");
```

## Response Handling

Every request returns `ApiResponse<T>`:

```csharp
var response = await TechnyxSdk.Http.GetAsync<List<ItemData>>("items");

if (response.IsSuccess)
{
    foreach (var item in response.Data)
        Debug.Log(item.Name);
}
else
{
    Debug.LogError($"Error {response.StatusCode}: {response.Error}");
}
```

### `ApiResponse<T>` Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsSuccess` | bool | `true` if status code is 2xx |
| `StatusCode` | int | HTTP status code |
| `Data` | T | Deserialized response data (from `data` envelope) |
| `Error` | ApiError | Error details (from `error` envelope) |
| `RawBody` | string | Raw response body for debugging |

### `ApiError` Properties

| Property | Type | Description |
|----------|------|-------------|
| `Code` | string | Error code, e.g. `INVALID_CREDENTIALS` |
| `Message` | string | Human-readable error message |

## Automatic 401 Retry

If a request receives a `401 Unauthorized`:

1. The client automatically attempts a token refresh
2. If refresh succeeds, the original request is retried with the new token
3. If refresh fails, the response is returned as-is and auth state changes to `Error`

This happens transparently – no extra code needed.

## Request Paths

Paths are relative to the `apiBaseUrl` from config. No leading slash needed:

```csharp
// Config: apiBaseUrl = "https://api.technyx.tools/api/v1"
// This calls: https://api.technyx.tools/api/v1/teams/abc123/members
await TechnyxSdk.Http.GetAsync<List<Member>>("teams/abc123/members");
```
