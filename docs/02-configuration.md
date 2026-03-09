# Configuration

## Config File

The SDK reads configuration from `Resources/OneConfig.json`. A default config is shipped with the package.

```json
{
    "apiBaseUrl": "https://api.technyx.tools/api/v1",
    "tokenRefreshMarginSeconds": 300,
    "encryptionSalt": "TnxOneSdkSalt2026",
    "requestTimeoutSeconds": 30
}
```

### Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `apiBaseUrl` | string | `https://api.technyx.tools/api/v1` | Base URL for all API requests. No trailing slash. |
| `tokenRefreshMarginSeconds` | int | `300` | Seconds before token expiry to trigger auto-refresh. |
| `encryptionSalt` | string | `TnxOneSdkSalt2026` | Salt used for encrypting stored tokens. Change per project. |
| `requestTimeoutSeconds` | int | `30` | HTTP request timeout. |

## Editor Settings

Go to **Edit > Project Settings > Technyx One** to edit the config visually. Click **Save** to write changes to disk.

## Override at Runtime

```csharp
using Technyx.One.Config;

OneConfigLoader.Override(new OneConfig
{
    apiBaseUrl = "https://staging.technyx.tools/api/v1",
    tokenRefreshMarginSeconds = 60,
    encryptionSalt = "my-custom-salt",
    requestTimeoutSeconds = 15,
});
```

> **Note:** Runtime overrides must be applied before `OneServices` initializes (before the first scene loads), or you must reinitialize manually.

## Per-Project Config

To override the package default, create your own `Assets/Resources/OneConfig.json`. Unity will load it instead of the package's built-in version.
