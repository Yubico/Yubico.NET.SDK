# Public API convention tests

`PublicApi` is a test-only cross-module convention project. It references shipping SDK assemblies to verify
shared applet and facade API shapes, but it produces no shipping assembly or package.

Run it with:

```bash
dotnet toolchain.cs -- test --project PublicApi
```
