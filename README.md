# Technyx One SDK

Unity Package Manager (UPM) package for Technyx AR/VR backend integration. Provides authentication, token management, and a typed HTTP client.

## Installation

In Unity, go to **Window > Package Manager > + > Add package from git URL**:

```
git@github.com:technyx-ar/upm-helpers.git
```

Requires Unity 2022.3 LTS or newer.

## Quick Start

The SDK auto-initializes – no prefab or setup code needed.

```csharp
using Technyx.One;
using Technyx.One.Auth;

// Login
var result = await OneServices.Auth.LoginAsync("user@example.com", "password");
if (result.IsSuccess)
    Debug.Log($"Welcome, {result.Data.User.Name}!");

// Check auth state
if (OneServices.Auth.IsAuthenticated)
    Debug.Log("Logged in");

// Make authenticated API calls (Bearer token attached automatically)
var data = await OneServices.Http.GetAsync<MyModel>("some/endpoint");

// Listen for auth state changes
OneServices.Auth.OnAuthStateChanged += (oldState, newState) =>
{
    if (newState == AuthState.Error)
        ShowLoginScreen();
};

// Get current user
var user = await OneServices.Auth.GetCurrentUserAsync();

// Logout
await OneServices.Auth.LogoutAsync();
```

## Configuration

Edit `Resources/OneConfig.json` or use **Edit > Project Settings > Technyx One**:

```json
{
    "apiBaseUrl": "https://api.technyx.tools/api/v1",
    "tokenRefreshMarginSeconds": 300,
    "encryptionSalt": "TnxOneSdkSalt2026",
    "requestTimeoutSeconds": 30
}
```

To override the package default, create your own `Assets/Resources/OneConfig.json`.

## Features

- **Authentication** – Login, logout, token refresh matching the Laravel Passport backend
- **Auto-refresh** – Tokens refresh automatically before expiry
- **401 retry** – Failed requests trigger a token refresh and retry once
- **Encrypted storage** – AES-256 encrypted tokens in PlayerPrefs
- **Session restore** – Stored tokens are restored on app start
- **State events** – Subscribe to `OnAuthStateChanged` for auth transitions
- **Typed HTTP client** – `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `PatchAsync<T>`, `DeleteAsync` with automatic `{ "data": ... }` envelope unwrapping

## Architecture

```
OneServices (singleton, auto-init)
├── Auth        – AuthService (login, logout, refresh, state management)
├── Http        – ApiClient (typed async requests, Bearer token, 401 retry)
├── Config      – OneConfig + OneConfigLoader (JSON from Resources)
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

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/login` | No | Login with email + password |
| POST | `/auth/logout` | Yes | Revoke current token |
| POST | `/auth/refresh` | Yes | Get a new token |
| GET | `/auth/user` | Yes | Get current user profile |
