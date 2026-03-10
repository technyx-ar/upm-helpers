# Auth State & Events

## Auth States

| State | Description |
|-------|-------------|
| `Anonymous` | No token stored. User must log in. |
| `Authenticated` | Valid token in storage. API calls are authorized. |
| `Refreshing` | Token refresh is in progress. |
| `Error` | Token refresh failed. User must re-authenticate. |

## State Change Event

Subscribe to `OnAuthStateChanged` to react to auth transitions:

```csharp
using Technyx.Sdk;
using Technyx.Sdk.Auth;

void Start()
{
    TechnyxSdk.Auth.OnAuthStateChanged += HandleAuthStateChanged;
}

void OnDestroy()
{
    TechnyxSdk.Auth.OnAuthStateChanged -= HandleAuthStateChanged;
}

private void HandleAuthStateChanged(AuthState oldState, AuthState newState)
{
    Debug.Log($"Auth: {oldState} -> {newState}");

    switch (newState)
    {
        case AuthState.Authenticated:
            // Show main UI
            break;

        case AuthState.Anonymous:
            // Show login screen
            break;

        case AuthState.Error:
            // Token expired / refresh failed, prompt re-login
            ShowLoginScreen();
            break;

        case AuthState.Refreshing:
            // Optionally show a loading indicator
            break;
    }
}
```

## Typical State Flows

**First login:**
`Anonymous` -> `Authenticated`

**Auto-refresh (background):**
`Authenticated` -> `Refreshing` -> `Authenticated`

**Refresh failure (token expired on server):**
`Authenticated` -> `Refreshing` -> `Error`

**Logout:**
`Authenticated` -> `Anonymous`

**App restart with valid stored token:**
`Anonymous` -> `Authenticated` (instant, during initialization)
