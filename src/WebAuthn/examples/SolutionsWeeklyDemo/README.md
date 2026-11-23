# Solutions Weekly demo

A single-file demo of the YubiKit .NET SDK v2. It shows:
- device detection
- device info
- passkey registration and sign-in
- touch prompts that show the app's intent
- an experimental previewSign document signature

## Run

Requires the .NET 10 SDK and a YubiKey with firmware 5 or later. The previewSign step only runs on a key that supports it.

```sh
cd src/WebAuthn/examples/SolutionsWeeklyDemo
dotnet run solutions-weekly-demo.cs
```

Plug in the key when prompted and touch it twice: once to register, once to sign in.

Run it once before presenting. The first `dotnet run` compiles the file, and later runs reuse that build.

## Notes

- Packages come from the public v2 alpha feed, pinned in the `#:package` lines and configured in `nuget.config`.
  After the beta reaches nuget.org, remove the alpha feed and bump the versions.
- The local `Directory.Build.*` and `Directory.Packages.props` files are empty on purpose. They stop the repo-wide
  build settings from applying to this standalone demo.
- `PretendServer` stands in for the relying party. The SDK is client-side only, so a real server verifies with a
  maintained WebAuthn server library.
- `ArkgBridge` uses reflection because this alpha doesn't expose a public way to derive ARKG public keys from the
  WebAuthn layer. It mirrors the SDK's own previewSign integration test. Demo only.
