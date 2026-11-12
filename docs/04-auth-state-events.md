# Auth State & Events

## Auth States

| State | Description |
|-------|-------------|
| `Anonymous` | No token stored. User must log in. |
| `Authenticated` | Valid token in storage. API calls are authorized. |
| `Refreshing` | Token refresh is in progress. |
| `Error` | The server rejected the token (or the stored session is gone). User must re-authenticate. |

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
            // The server rejected the session, prompt re-login
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

**Refresh rejected (the server answered 401):**
`Authenticated` -> `Refreshing` -> `Error`. The stored session is cleared.

**Refresh failed for another reason (no network, server error, timeout):**
`Authenticated` -> `Refreshing` -> `Authenticated`. The user stays logged in and the refresh is tried again after `refreshRetryBaseSeconds`, doubling up to `refreshRetryMaxSeconds`. Only a rejection by the server ends the session.

**Concurrent refreshes:** `RefreshAsync` called while a refresh runs returns the result of the running one.

**Logout:**
`Authenticated` -> `Anonymous`

**App restart with valid stored token:**
`Anonymous` -> `Authenticated` (instant, during initialization)
