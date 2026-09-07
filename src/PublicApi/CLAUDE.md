# CLAUDE.md - public API convention tests

This directory is test-only. It contains cross-module reflection conventions and has no shipping assembly.

Keep applet-session conventions limited to `ApplicationSession` applets. Higher-level facades such as WebAuthn
may be referenced for conventions that genuinely match their public role, but must not be forced into applet
session or interface-parity lists.

Run focused verification with `dotnet toolchain.cs -- test --project PublicApi`.
