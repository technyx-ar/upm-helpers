# Authentication

The `AuthService` handles login, logout, token refresh, and session restoration. Access it via `OneServices.Auth`.

## Login

```csharp
using Technyx.One;

var result = await OneServices.Auth.LoginAsync("user@example.com", "password123");

if (result.IsSuccess)
{
    Debug.Log($"Welcome, {result.Data.User.Name}!");
    Debug.Log($"Token expires at: {result.Data.ExpiresAt}");
}
else
{
    Debug.LogError($"Login failed: {result.Error.Message}");
}
```

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `email` | string | Yes | User's email address |
| `password` | string | Yes | User's password (min 8 chars) |
| `deviceName` | string | No | Defaults to `SystemInfo.deviceModel` |

### Response

On success, `result.Data` is a `LoginResponse`:

```csharp
public class LoginResponse
{
    public string Token;
    public string TokenType;    // "Bearer"
    public string ExpiresAt;    // ISO 8601
    public UserData User;
}
```

## Logout

```csharp
bool success = await OneServices.Auth.LogoutAsync();
```

Revokes the token on the server, clears local storage, and sets state to `Anonymous`.

## Token Refresh

Tokens refresh automatically before expiry. You can also trigger it manually:

```csharp
var result = await OneServices.Auth.RefreshAsync();

if (result.IsSuccess)
    Debug.Log("Token refreshed successfully.");
```

## Get Current User

```csharp
var result = await OneServices.Auth.GetCurrentUserAsync();

if (result.IsSuccess)
{
    UserData user = result.Data;
    Debug.Log($"{user.Name} ({user.Email})");
}
```

The `UserData` model:

```csharp
public class UserData
{
    public string Id;        // ULID
    public string Name;
    public string Email;
    public string Phone;
    public string Language;  // e.g. "sk"
    public string Timezone;  // e.g. "Europe/Bratislava"
    public string CreatedAt; // ISO 8601
}
```

## Check Auth State

```csharp
// Quick check
if (OneServices.Auth.IsAuthenticated)
{
    // User is logged in
}

// Current state enum
AuthState state = OneServices.Auth.CurrentState;

// Cached user (from last login or GetCurrentUserAsync)
UserData user = OneServices.Auth.CurrentUser;
```

## Session Restoration

On app start, the SDK automatically checks for a stored (encrypted) token. If found and not expired, the state is set to `Authenticated` and the refresh timer starts. No code needed.
