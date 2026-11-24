# Solutions Weekly demos

Two single-file demos of the YubiKit .NET SDK v2. They share a small helper project, so each demo file only
shows the code that matters.

| Demo | Shows | Run |
|---|---|---|
| `passkey-demo.cs` | Device detection and info, passkey registration and sign-in, touch prompts with app intent, server-side verification | `dotnet run passkey-demo.cs` |
| `previewsign-demo.cs` | **Experimental** previewSign: a signing key bound to a passkey, ARKG public key derivation without the YubiKey, raw signature verified with plain ECDSA | `dotnet run previewsign-demo.cs` |

## Requirements

- .NET 10 SDK.
- A YubiKey with firmware 5 or later, with "Always Require UV" off. A FIDO PIN being set is fine; check with
  `ykman fido info`.
- previewSign needs firmware that supports it (5.8 or later).

Touch the key twice when prompted, once to register and once to sign in. Touch promptly. A missed touch
fails with "Operation denied".

Run each demo once before presenting. The first `dotnet run` compiles it, and later runs reuse that build.

## Layout

- `DemoSupport/`: shared helper project, referenced with `#:project`. It holds device discovery, client
  setup, default WebAuthn options, the console touch prompt, the pretend server and ARKG derivation.
- `nuget.config`: adds the public v2 alpha feed. After the beta reaches nuget.org, remove the alpha feed and
  bump the versions in `DemoSupport/DemoSupport.csproj`.
- `Directory.Build.*`, `Directory.Packages.props`: empty on purpose. They stop the repo-wide build settings
  from applying here.
- `DECK.md`: the Solutions Weekly slide outline.

## Notes

- `PretendServer` stands in for the relying party. The SDK is client-side only, so a real server verifies
  with a maintained WebAuthn server library.
- previewSign isn't transaction confirmation. The key never shows the user the data it signs.
- ARKG derivation in `DemoSupport` uses reflection, because this alpha doesn't expose a public way to derive
  ARKG public keys from the WebAuthn layer. It mirrors the SDK's own integration test. Demo only.
- Each run stores a test passkey for `example.com`. Clean up with `ykman fido credentials list` / `delete`.
