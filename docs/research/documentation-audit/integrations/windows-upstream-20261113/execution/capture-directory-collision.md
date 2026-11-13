# Validation capture directory correction

The first attempt to capture verifier compilation used the label `verifier-compile`, which already
names the prepared aggregate project directory. The capture helper stopped at `mkdir(exist_ok=False)`
with `FileExistsError` before launching any .NET command. No project or evidence file was overwritten.

The coordinator changed the capture label to `verifier-build`. The project selector stays
`Yubico.YubiKit.WindowsMerge20261113.VerifierCompile`; this correction does not change validation scope.
