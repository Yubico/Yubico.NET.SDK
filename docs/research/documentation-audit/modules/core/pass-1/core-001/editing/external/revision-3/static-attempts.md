# Revision-3 receipt preparation

The first receipt run passed the protected-line, XML and frozen-history checks, then stopped at its
new-example assertion for `scripts/toolchain/Process.cs`. The initial scan searched whole source text
for `<code>` and matched the regex named capture `(?<code>...)` in executable source, not XML documentation.
Its exact failure was `AssertionError: scripts/toolchain/Process.cs` at the new-example assertion.

The successor limits that scan to actual `///` documentation lines. No runtime source, earlier patch,
frozen initial/revision-2 receipt, test, profile or validation input was changed to satisfy the check.
This was a static receipt-extraction error, not an SDK/compiler/test failure or an executed example.
