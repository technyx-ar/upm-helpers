# Token Storage & Security

## How the Session Is Stored

The token and its expiry are stored in `PlayerPrefs` under the keys `_tnx_tk` and `_tnx_te`. Each value is encrypted and then authenticated (encrypt-then-MAC):

- **Key derivation:** `SHA-256(SystemInfo.deviceUniqueIdentifier + encryptionSalt)` is the master secret; the encryption key and the MAC key are derived from it with HMAC-SHA256 (labels `enc` and `mac`).
- **Encryption:** AES (256-bit key), random IV per value, CBC mode with PKCS7 padding.
- **Integrity:** HMAC-SHA256 over a purpose label (`token` or `expiry`), the format version, the IV and the ciphertext. It is compared in constant time before anything is decrypted, so a token value cannot be swapped into the expiry slot.
- **Format:** Base64 of `version byte | IV | ciphertext | MAC`.

## What This Protects – and What It Does Not

The key material is the device identifier and the salt. The identifier is readable by any code running on the device, and the salt ships inside your app. So:

- The MAC and the encryption detect a damaged or hand-edited value and keep the token from being readable in a casual look at the stored files.
- They do **not** protect the token from someone who controls the device or can read your app's files and binary. This is tamper detection and obfuscation, not a keystore.
- For hardware-backed protection extend `TokenStorage` to use the platform keystore (Android Keystore, iOS Keychain). The current implementation works on every Unity platform including Quest.

## Behavior to Know

- A stored value that fails its integrity check, is in an unknown format or cannot be parsed is discarded with a warning in the log, and the user has to log in again.
- A session stored by an earlier version of the SDK (which used a different format) is discarded the same way once, after the update.
- If the device identifier changes (another device, factory reset), the stored session becomes unreadable.
- Changing `encryptionSalt` invalidates every stored session. Use a different salt per project.
- An expired token is cleared on load.

## Manual Token Operations

In most cases you won't need these – `AuthService` handles storage automatically.

```csharp
using Technyx.Sdk;

var token = TechnyxSdk.TokenStorage.Load();
if (token != null)
    Debug.Log($"Token expires: {token.ExpiresAt}");

// Clear the stored session without calling the server
TechnyxSdk.TokenStorage.Clear();
```

`Load()` returns `null` when nothing valid is stored.
