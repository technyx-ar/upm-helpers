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

Codes produced by the client itself (not by the backend):

| Code | Meaning |
|------|---------|
| `NETWORK_ERROR` | The request never reached the server or no response arrived (DNS, TLS, timeout, connection reset). `StatusCode` is `0`. |
| `PARSE_ERROR` | The server answered, but the body could not be read (for example an HTML error page from a proxy, or a login response without a valid token or expiry). |

## Automatic 401 Retry

If a request that carried a Bearer token receives a `401 Unauthorized`:

1. The client automatically attempts a token refresh
2. If refresh succeeds, the original request is retried with the new token
3. If refresh fails, the response is returned as-is and auth state changes to `Error`

`auth/login` and `auth/refresh` never trigger this retry: a 401 from either one is a credential failure and is returned as-is. Requests sent without a stored token are not retried either.

This happens transparently – no extra code needed.

## Request Paths

Paths are relative to the `apiBaseUrl` from config. No leading slash needed:

```csharp
// Config: apiBaseUrl = "https://api.prod.technyx.ar/api/v1"
// This calls: https://api.prod.technyx.ar/api/v1/teams/abc123/members
await TechnyxSdk.Http.GetAsync<List<Member>>("teams/abc123/members");
```
