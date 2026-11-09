# Configuration

## Defaults

The package ships no config file. `SdkConfig` carries the defaults, so the SDK works without any setup:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `apiBaseUrl` | string | `https://api.prod.technyx.ar/api/v1` | Base URL for all API requests. Absolute http or https URL, no trailing slash. |
| `tokenRefreshMarginSeconds` | int | `300` | Seconds before token expiry to trigger auto-refresh. |
| `encryptionSalt` | string | `TnxSdkSalt2026` | Salt mixed into the key that protects the stored session. Change it per project. Must not be empty. |
| `requestTimeoutSeconds` | int | `30` | Timeout of a normal request. |
| `streamTimeoutSeconds` | int | `300` | Timeout of requests that wait for a whole AI answer: `ai/chat` and `ai/chat/stream`. |
| `uploadTimeoutSeconds` | int | `120` | Timeout of multipart uploads (photos, organization logos). |
| `maxRetries` | int | `2` | How many times a failed request is repeated. `0` turns retries off. See [HTTP Client](05-http-client.md). |
| `retryBaseDelaySeconds` | float | `1` | First backoff delay; doubles with every retry. |
| `maxRetryDelaySeconds` | int | `30` | Upper bound of a backoff delay and of an accepted `Retry-After`. |
| `refreshRetryBaseSeconds` | float | `10` | First delay before the background token refresh is tried again after a network or server failure. |
| `refreshRetryMaxSeconds` | int | `120` | Upper bound of that delay. |

A value outside its range makes loading throw `InvalidOperationException` with every problem listed.

## Project Config File

To change values, create `Assets/Resources/TechnyxConfig.json` with only the fields you want to change. Missing fields keep their default:

```json
{
    "apiBaseUrl": "https://staging.technyx.tools/api/v1",
    "encryptionSalt": "my-project-salt"
}
```

**Edit > Project Settings > Technyx SDK** edits the same file and creates it on **Save**. It only reads and writes `Assets/Resources`, never the package folder, because a package installed from git is read-only.

## Override Before Initialization

`SdkConfigLoader.Override` replaces the loaded config. It has to run before the SDK initializes, and it throws `InvalidOperationException` afterwards:

```csharp
using Technyx.Sdk.Config;
using UnityEngine;

public static class SdkBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Configure()
    {
        SdkConfigLoader.Override(new SdkConfig
        {
            apiBaseUrl = "https://staging.technyx.tools/api/v1",
            encryptionSalt = "my-custom-salt",
        });
    }
}
```

The SDK initializes in `BeforeSceneLoad`; `SubsystemRegistration` runs earlier.

## Reconfigure at Runtime

```csharp
TechnyxSdk.Configure(new SdkConfig { apiBaseUrl = "https://staging.technyx.tools/api/v1" });
```

`Configure` validates the config, stops the running auth timers, rebuilds every service and restores the stored session. Consequences:

- References taken earlier (`var auth = TechnyxSdk.Auth;`) point at the old, shut-down services. Read the static properties again.
- `OnAuthStateChanged` handlers are attached to the old `AuthService`; subscribe again.
- A different `encryptionSalt` makes the stored session unreadable, so the user has to log in again.
