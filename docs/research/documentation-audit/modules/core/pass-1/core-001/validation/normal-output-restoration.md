# Ordinary output restoration before final SDK tests

The final `build-pack-final` invocation ran the ordinary Release clean solution build with zero
warnings. All ten compile-only test contexts are members of that42-project solution. Before starting
SDK tests, Astra checked each current test DLL as bytes: the unique injected namespace marker
`Audit20261111.DocExamples` was absent. It was present in that same context's preserved injected
runtime DLL, providing a positive control for the check. No assembly was loaded or executed by this check.

`DirectoryBuildTargetsPath`, `DocExamplesInjectionFiles`, `DocExamplesCapture` and
`DocExamplesSelectedProject` were unset. The normal project files and their imports retain their
accepted source identities; injection sources live outside those project trees.

| Normal Release/net10.0 test DLL | SHA-256 at restoration check |
| --- | --- |
| Core.IntegrationTests | `5fa3a5205f78ef62bcf33b7b2dcf972a7ce2caa8a1367f517823ccd9e4fce039` |
| Core.UnitTests | `7fe2ef471cd9c23adaafa9a3c205d797a137fbb360713c1a95085033a34bb806` |
| Ctap2.IntegrationTests | `1a03acf8dc401dc6d4e3238f7cda2a3d4292f44b64bdd32d1f921878d5270907` |
| Ctap2.UnitTests | `9f8dc3ec0a361a8162e9fa81ce679b5d76454c877c75a370ef50da88e3971284` |
| Management.IntegrationTests | `a3878378e2cabe0d7edf8c6707fec5d5c772e576363922cce303c0f21b072031` |
| Management.UnitTests | `d1e1cf9bf64d8210cc59ebb2b4a3e8da7f917f443b3e7288b257fbaee468bbce` |
| Oath.IntegrationTests | `ba121a0ec06c6011a697ae38e2cc32489a4cc38895c1f2e7d83394aaceba8bd6` |
| Piv.IntegrationTests | `655e89b44550f6ea617170006dbd6921c82b98abf1ba34a893af7d45fb5584b8` |
| SecurityDomain.IntegrationTests | `0b40578dcf303ce4340fc8944f57d26c9dcee9710959224053fe49089719eb45` |
| WebAuthn.IntegrationTests | `85a4decfa07f17860768fa6456690a349f5381ebbad853348022f718cf2b959f` |

Each DLL is `bin/Release/net10.0/<project-stem>.dll` under the actual context's project directory.
Its earlier injected counterpart is located through that context's accepted compilation receipt and
`mutable-output-preservation.json`, not assumed to remain at its former live path. This proves the
specific restoration check before ordinary tests, not general binary equivalence or device behavior.
