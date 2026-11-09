# Technyx SDK

Unity Package Manager (UPM) package for Technyx AR/VR backend integration. Provides authentication, device, team, organization and photo management, AI chat with streaming, token management, and a typed HTTP client.

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

// Or let the SDK send it every few minutes (opt-in: "autoHeartbeat": true in the config)
TechnyxSdk.Heartbeat.HeartbeatFailed += failure => Debug.LogWarning(failure.Error);
TechnyxSdk.Heartbeat.Start(device.Data.Id);

// Upload a photo
var photo = await TechnyxSdk.Photos.UploadAsync(jpegBytes, "wing.jpg", "image/jpeg",
    new PhotoMetadata { Name = "Left wing", Tags = new List<string> { "inspection" } });

// Ask the AI assistant and show the answer while it streams
var answer = await TechnyxSdk.Ai.ChatStreamAsync(
    new AiChatRequest { Message = "What is the propeller bolt torque?" },
    chunk => label.text += chunk.Text);

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

The SDK works with its built-in defaults (`https://api.prod.technyx.ar/api/v1`). To change them, create `Assets/Resources/TechnyxConfig.json` or use **Edit > Project Settings > Technyx SDK**; fields you leave out keep their default:

```json
{
    "apiBaseUrl": "https://api.prod.technyx.ar/api/v1",
    "encryptionSalt": "change-me-per-project"
}
```

All fields (timeouts, retries, refresh back-off) are listed in [Configuration](docs/02-configuration.md).

## Features

- **Authentication** – Login, logout, token refresh matching the Laravel Passport backend
- **Device management** – Register, list, update, delete AR/VR devices with heartbeat telemetry and MDM-lite config
- **Automatic heartbeat** – Opt-in loop with a telemetry callback, bounded back-off and a `HeartbeatFailed` event; stops on logout and quit
- **Teams and organizations** – CRUD, partial team updates, organization settings and logo upload
- **Photos** – Upload with metadata, filtered listing, update, delete
- **AI chat** – One-shot and SSE-streamed answers, knowledge items with grounding and sources, stored conversations
- **AI admin** – Usage statistics and audit log of the organization (`view-ai-audit` permission)
- **Auto-refresh** – Tokens refresh automatically before expiry; a network or server failure keeps the session and retries with back-off, only a rejection by the server logs the user out
- **401 retry** – Failed requests trigger a token refresh and retry once
- **Retries and cancellation** – Bounded retries with back-off and `Retry-After`; every call accepts a `CancellationToken`
- **Typed errors** – `ApiError` with backend code, validation errors per field, `Retry-After`, details
- **Protected storage** – Tokens in PlayerPrefs, AES-encrypted and authenticated with HMAC (tamper detection, not a keystore – see [Token Storage](docs/06-token-storage.md))
- **Session restore** – Stored tokens are restored on app start
- **State events** – Subscribe to `OnAuthStateChanged` for auth transitions
- **Typed HTTP client** – `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `PatchAsync<T>`, `DeleteAsync` with automatic `{ "data": ... }` envelope unwrapping, multipart uploads and streaming
- **Pagination** – `PaginatedResponse<T>` for paginated API endpoints

## Architecture

```
TechnyxSdk (singleton, auto-init)
├── Auth         – AuthService (login, logout, refresh, state management)
├── Devices      – DeviceService (CRUD, heartbeats, config)
├── Heartbeat    – HeartbeatService (automatic heartbeat loop, opt-in)
├── Teams        – TeamService (CRUD)
├── Organizations – OrganizationService (CRUD, settings, logo)
├── Photos       – PhotoService (upload, list, update, delete)
├── Ai           – AiService (chat, SSE stream, conversations)
├── AiAdmin      – AiAdminService (usage statistics, audit log)
├── Http         – ApiClient (typed async requests, Bearer token, 401 retry, retries, uploads, streams)
├── Config       – SdkConfig + SdkConfigLoader (code defaults, optional JSON from Resources)
└── TokenStorage – encrypted and authenticated PlayerPrefs
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
8. [Teams & Organizations](docs/08-teams.md)
9. [Photos](docs/09-photos.md)
10. [AI Chat](docs/10-ai.md)

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
| GET, POST | `/teams` | Yes | List teams, create a team |
| GET, PATCH, DELETE | `/teams/{id}` | Yes | Get (with members), update, delete a team |
| GET, POST | `/organizations` | Yes | List organizations, create one |
| GET, PATCH, DELETE | `/organizations/{id}` | Yes | Get, update, delete an organization |
| POST (`_method=PATCH`) | `/organizations/{id}` | Yes | Upload the organization logo (multipart) |
| GET, POST | `/photos` | Yes | List and filter photos, upload a photo (multipart) |
| GET, PATCH, DELETE | `/photos/{id}` | Yes | Get, update, delete a photo |
| POST | `/ai/chat` | Yes | One-shot AI answer |
| POST | `/ai/chat/stream` | Yes | Streamed AI answer (SSE) |
| GET | `/ai/conversations` | Yes | List AI conversations |
| GET, PATCH, DELETE | `/ai/conversations/{id}` | Yes | Get (with messages), update, delete a conversation |
| GET | `/admin/ai/usage` | Yes | AI usage statistics of the organization (`view-ai-audit`) |
| GET | `/admin/ai/audit` | Yes | List and filter the AI audit log (`view-ai-audit`) |
| GET | `/admin/ai/audit/{id}` | Yes | One audit entry with its sources (`view-ai-audit`) |
