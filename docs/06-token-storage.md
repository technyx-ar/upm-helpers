# Token Storage & Security

## How Tokens Are Stored

Auth tokens are encrypted with AES-256-CBC before being saved to `PlayerPrefs`. This prevents trivial token theft from device storage.

### Encryption Details

- **Algorithm:** AES-256-CBC
- **Key derivation:** SHA-256 hash of `SystemInfo.deviceUniqueIdentifier` + config `encryptionSalt`
- **IV:** Random per encryption, stored alongside ciphertext
- **Storage:** Base64-encoded ciphertext in `PlayerPrefs`

### What This Means

- Tokens are encrypted at rest
- Each device has a unique encryption key
- If the device ID changes (factory reset, different device), stored tokens become unreadable and the user must re-authenticate
- Changing the `encryptionSalt` in config invalidates all stored tokens

## Manual Token Operations

In most cases you won't need these – `AuthService` handles storage automatically.

```csharp
using Technyx.One;

// Check if a token exists
var token = OneServices.TokenStorage.Load();
if (token != null)
{
    Debug.Log($"Token expires: {token.ExpiresAt}");
}

// Clear stored token (without server logout)
OneServices.TokenStorage.Clear();
```

## Security Considerations

- The encryption salt should be unique per project. Change it from the default in `OneConfig.json`.
- `PlayerPrefs` on some platforms (Windows) stores data in the registry or plain files. The AES encryption adds a layer of protection, but it is not equivalent to a hardware-backed keystore.
- For highest security on Android, consider extending `TokenStorage` to use the Android Keystore. The current implementation is a practical default that works across all Unity platforms including Quest.
