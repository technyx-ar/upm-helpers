# HTTP Client

`ApiClient` provides typed async HTTP methods. Access it via `TechnyxSdk.Http`. It attaches the Bearer token, unwraps the `{ "data": ... }` response envelope and turns every failure into an `ApiResponse<T>` – the methods do not throw for network or server errors.

## Making Requests

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Http;

var get    = await TechnyxSdk.Http.GetAsync<MyModel>("some/endpoint");
var post   = await TechnyxSdk.Http.PostAsync<MyModel>("some/endpoint", new { name = "test" });
var put    = await TechnyxSdk.Http.PutAsync<MyModel>("some/endpoint", requestBody);
var patch  = await TechnyxSdk.Http.PatchAsync<MyModel>("some/endpoint", partialData);
var delete = await TechnyxSdk.Http.DeleteAsync("some/endpoint");
```

Every method takes an optional `CancellationToken` as its last argument.

Request bodies are serialized with Newtonsoft.Json. Path segments you build yourself must be escaped; the service classes do this for you.

## Response Handling

```csharp
var response = await TechnyxSdk.Http.GetAsync<List<ItemData>>("items");

if (response.IsSuccess)
{
    foreach (var item in response.Data)
        Debug.Log(item.Name);
}
else
{
    Debug.LogError(response.Error);
}
```

### `ApiResponse<T>`

| Property | Type | Description |
|----------|------|-------------|
| `IsSuccess` | bool | `true` if status code is 2xx |
| `StatusCode` | int | HTTP status code; `0` when no response arrived or the SDK rejected the call before sending it |
| `Data` | T | Deserialized response data (from the `data` envelope); `default` for 204 |
| `Error` | ApiError | Error details when `IsSuccess` is `false` |
| `RawBody` | string | Raw response body for debugging |
| `IsCancelled` | bool | The call was cancelled through its `CancellationToken` |
| `IsNetworkError` | bool | No response arrived |

### `ApiError`

| Member | Type | Description |
|--------|------|-------------|
| `Code` | string | Backend code such as `AI_RATE_LIMIT`, or one of the codes below |
| `Message` | string | Human-readable message |
| `Details` | JToken | The backend's `details` object, if any (for example the budget of `AI_BUDGET_EXCEEDED`) |
| `RetryAfterSeconds` | int? | From `retry_after` in the body or the `Retry-After` header (seconds form only) |
| `ValidationErrors` | Dictionary<string, List<string>> | Field errors of a `422` answer |
| `HasValidationErrors` | bool | |
| `GetFieldErrors(field)` | IReadOnlyList<string> | Messages for one field; empty when there are none |

```csharp
if (response.Error.Code == ApiErrorCodes.ValidationError)
{
    foreach (var message in response.Error.GetFieldErrors("email"))
        ShowFieldError("email", message);
}
```

The backend answers errors in three shapes (`{error:{code,message,...}}`, `{success:false,message}` and the Laravel default `{message,errors}`); the SDK reads all of them. When the body is not JSON (for example an HTML page from a proxy) the code is derived from the status:

| Status | Code |
|--------|------|
| 400 | `BAD_REQUEST` |
| 401 | `UNAUTHENTICATED` |
| 403 | `FORBIDDEN` |
| 404 | `NOT_FOUND` |
| 409 | `CONFLICT` |
| 413 | `PAYLOAD_TOO_LARGE` |
| 422 | `VALIDATION_ERROR` |
| 429 | `RATE_LIMITED` |
| 5xx | `SERVER_ERROR` |
| other | `UNKNOWN` |

Codes produced by the client itself (see `ApiErrorCodes`):

| Code | Meaning |
|------|---------|
| `NETWORK_ERROR` | No response arrived (DNS, TLS, timeout, connection reset). `StatusCode` is `0`. |
| `PARSE_ERROR` | The server answered, but the body could not be read (a login response without a valid token or expiry, a malformed stream event). |
| `CANCELLED` | The `CancellationToken` was cancelled. |
| `NO_SESSION` | A refresh was requested without a stored session. No request was sent. |
| `STREAM_ERROR` | The backend reported an error inside an AI stream. |
| `STREAM_INTERRUPTED` | An AI stream ended before the backend sent its closing event. |
| `VALIDATION_ERROR` | Also used when the SDK rejects an upload before sending it (empty file, too large, wrong type). `StatusCode` is `0`. |

## Retries

A failed request is repeated up to `maxRetries` times (default 2) with exponential backoff (`retryBaseDelaySeconds`, doubling, capped at `maxRetryDelaySeconds`, plus up to 20 % jitter):

| Failure | Repeated for |
|---------|--------------|
| `429` | every method; waits for `Retry-After` when the backend sent it, and gives up at once if that is longer than `maxRetryDelaySeconds` |
| Transport failure, `502`, `503`, `504` | `GET`, `HEAD`, `PUT`, `PATCH`, `DELETE` only – a `POST` may already have been handled, so it is not repeated |

A stream (`PostStreamAsync`) is only repeated after a `429`, because its body may already have been delivered.

The retry delay uses `Task.Delay`, which needs threads. The SDK therefore targets standalone, mobile and XR players (Quest, Pico, HoloLens, Vision Pro) and does not support WebGL.

## Cancellation

Pass a `CancellationToken` to stop a call, including a pending retry delay:

```csharp
using System.Threading;

var cts = new CancellationTokenSource();
var task = TechnyxSdk.Photos.ListAsync(1, null, cts.Token);
cts.Cancel();

var response = await task;
if (response.IsCancelled) { /* the user left the screen */ }
```

Cancelling a request that was already sent does not undo it on the server.

## Timeouts

| Config field | Used for |
|--------------|----------|
| `requestTimeoutSeconds` | normal requests |
| `uploadTimeoutSeconds` | `PostMultipartAsync` (photos, organization logos) |
| `streamTimeoutSeconds` | `PostLongRunningAsync` (`ai/chat`) and `PostStreamAsync` (`ai/chat/stream`) |

The values are passed to `UnityWebRequest.timeout`. How Unity reports an expired timeout has not been verified in a real player; a request that fails this way is a transport failure (`NETWORK_ERROR`) only if Unity sets `ConnectionError` or `DataProcessingError`.

## Automatic 401 Retry

If a request that carried a Bearer token receives a `401 Unauthorized`:

1. The client attempts a token refresh (concurrent callers share one refresh request).
2. If the refresh succeeds, the original request is repeated once with the new token.
3. If the server rejects the token, the session is cleared and auth state changes to `Error`.

`auth/login`, `auth/refresh` and `auth/logout` never trigger this: a 401 from them is returned as-is. Requests sent without a stored token are not retried either.

## Request Paths

Paths are relative to `apiBaseUrl`. No leading slash needed:

```csharp
// apiBaseUrl = "https://api.prod.technyx.ar/api/v1"
// This calls: https://api.prod.technyx.ar/api/v1/teams/abc123/members
await TechnyxSdk.Http.GetAsync<List<Member>>("teams/abc123/members");
```

## Pagination

`PaginatedResponse<T>` has `Data`, `Links`, `Meta` and the shortcuts `CurrentPage`, `LastPage`, `PerPage`, `Total`, `HasMorePages`. The client accepts both shapes the backend uses: `{data: {data: [...], meta}}` (devices, photos, AI conversations) and the top-level `{data: [...], meta}` of teams and organizations.

## Uploads and Streams

- `PostMultipartAsync<T>(path, MultipartFormBuilder, ct)` sends `multipart/form-data`. The whole body is built in memory first, because `UnityWebRequest` cannot stream a multipart body and the backend has no raw-upload endpoint.
- `PostStreamAsync(path, body, onBody, ct)` calls `onBody(buffer, count)` for every piece of a successful response while it downloads. Treat `buffer` as valid only during the call and do not throw from `onBody`. The AI module builds the SSE parsing on top of it – see [AI Chat](10-ai.md).

## Tests

The client talks to the network through `IHttpTransport`. Pass a fake to `new ApiClient(config, tokenStorage, transport)` to test code that depends on SDK responses without a server.
