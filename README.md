# Technyx SDK

Unity Package Manager (UPM) package for Technyx AR/VR backend integration. Provides authentication, device management, token management, and a typed HTTP client.

## Installation

In Unity, go to **Window > Package Manager > + > Add package from git URL**:

```
git@github.com:technyx-ar/upm-helpers.git
```

Requires Unity 2022.3 LTS or newer.

## Quick Start

The SDK auto-initializes – no prefab or setup code needed.

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Auth;
using Technyx.Sdk.Devices;
using Technyx.Sdk.Models;

// Login
var result = await TechnyxSdk.Auth.LoginAsync("user@example.com", "password");
if (result.IsSuccess)
    Debug.Log($"Welcome, {result.Data.User.Name}!");

// Check auth state
if (TechnyxSdk.Auth.IsAuthenticated)
    Debug.Log("Logged in");

// Register a device
var device = await TechnyxSdk.Devices.RegisterAsync(new RegisterDeviceRequest
{
    Name = "Quest 3 - Lab",
    Type = DeviceType.Quest3,
    SerialNumber = "SN-123",
});

// Send heartbeat
await TechnyxSdk.Devices.SendHeartbeatAsync(device.Data.Id, new HeartbeatRequest
{
    BatteryLevel = 85,
    ConnectivityStatus = "wifi",
});

// Make authenticated API calls (Bearer token attached automatically)
var data = await TechnyxSdk.Http.GetAsync<MyModel>("some/endpoint");

// Listen for auth state changes
TechnyxSdk.Auth.OnAuthStateChanged += (oldState, newState) =>
{
    if (newState == AuthState.Error)
        ShowLoginScreen();
};

// Get current user
var user = await TechnyxSdk.Auth.GetCurrentUserAsync();

// Logout
await TechnyxSdk.Auth.LogoutAsync();
```

## Configuration

Edit `Resources/TechnyxConfig.json` or use **Edit > Project Settings > Technyx SDK**:

```json
{
    "apiBaseUrl": "https://api.technyx.tools/api/v1",
    "tokenRefreshMarginSeconds": 300,
    "encryptionSalt": "TnxSdkSalt2026",
    "requestTimeoutSeconds": 30
}
```

To override the package default, create your own `Assets/Resources/TechnyxConfig.json`.

## Features

- **Authentication** – Login, logout, token refresh matching the Laravel Passport backend
- **Device management** – Register, list, update, delete AR/VR devices with heartbeat telemetry and MDM-lite config
- **Auto-refresh** – Tokens refresh automatically before expiry
- **401 retry** – Failed requests trigger a token refresh and retry once
- **Encrypted storage** – AES-256 encrypted tokens in PlayerPrefs
- **Session restore** – Stored tokens are restored on app start
- **State events** – Subscribe to `OnAuthStateChanged` for auth transitions
- **Typed HTTP client** – `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `PatchAsync<T>`, `DeleteAsync` with automatic `{ "data": ... }` envelope unwrapping
- **Pagination** – `PaginatedResponse<T>` for paginated API endpoints

## Architecture

```
TechnyxSdk (singleton, auto-init)
├── Auth         – AuthService (login, logout, refresh, state management)
├── Devices      – DeviceService (CRUD, heartbeats, config)
├── Http         – ApiClient (typed async requests, Bearer token, 401 retry)
├── Config       – SdkConfig + SdkConfigLoader (JSON from Resources)
└── TokenStorage – AES-256 encrypted PlayerPrefs
```

## Documentation

Detailed docs in the [`docs/`](docs/) folder:

1. [Installation](docs/01-installation.md)
2. [Configuration](docs/02-configuration.md)
3. [Authentication](docs/03-authentication.md)
4. [Auth State & Events](docs/04-auth-state-events.md)
5. [HTTP Client](docs/05-http-client.md)
6. [Token Storage & Security](docs/06-token-storage.md)
7. [Devices](docs/07-devices.md)

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/login` | No | Login with email + password |
| POST | `/auth/logout` | Yes | Revoke current token |
| POST | `/auth/refresh` | Yes | Get a new token |
| GET | `/auth/user` | Yes | Get current user profile |
| GET | `/devices` | Yes | List user's devices (paginated) |
| POST | `/devices` | Yes | Register a new device |
| GET | `/devices/{id}` | Yes | Get device detail |
| PATCH | `/devices/{id}` | Yes | Update device |
| DELETE | `/devices/{id}` | Yes | Soft-delete device |
| POST | `/devices/{id}/heartbeat` | Yes | Send heartbeat (rate-limited) |
| GET | `/devices/{id}/config` | Yes | Get device config |
| PATCH | `/devices/{id}/config` | Yes | Partial config update |
