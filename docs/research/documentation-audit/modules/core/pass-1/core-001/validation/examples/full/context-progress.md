# Full-example compilation progress

All entries are **compile-only**. No example or injected test assembly has been executed.
Each successful runner retained its mutable compiler/reference/output files before later builds.

| Context | Accepted bodies | Attempt | Acceptance receipt SHA-256 |
| --- | ---: | --- | --- |
| Consumer | 244 | `examples-full-consumer-fourth` | `e044ba23cd4a0a55caa6cf0679716c6aba98c15f82adc292e05361bdf186c357` |
| Core.UnitTests | 6 | `examples-full-core-unit-first` | `9ff6235c1c8c077d7767f14f2a53f125b9b591a074eca42ace60725652d77e8a` |
| Ctap2.UnitTests | 3 | `examples-full-ctap-unit-first` | `097d83207ee0bd098cd42fb862b2f91fa318f955ff762e24d1f8880c08d637c8` |
| Ctap2.IntegrationTests | 3 | `examples-full-ctap-integration-second` | `b71ca5bf04c566fc4d556b9ff935489bd194094557124777d7b4d207da774a58` |
| Management.UnitTests | 1 | `examples-full-management-unit-first` | `90e664c4a4f15dfdd732280616a341a405c9e6d59e558e87fbd12d610f15d17f` |
| Management.IntegrationTests | 34 | `examples-full-management-second` | `768ce039c766a566c306cb4b64139ab9a1efa4ee0187d34d8677d06373140355` |

These three contexts passed with zero warnings under profile `3e40cb44…`. The three preceding
consumer failures retain their original labels, profiles and diagnostics.

`examples-full-ctap-integration-first` stopped **before Csc** because the Full-local capture guard did
not admit the actual package-provided `Microsoft.NET.Test.Sdk.Program.cs` from test SDK18.0.1.
This is a capture-input admission failure, not a documentation-body compiler failure. The validation
owner is preparing an exact package/producer-bound correction. The six later contexts were not started.

The reviewed package-source admission passed on the second CTAP2 integration attempt under profile
`fa8db15a…`: actual GenerateProgramFile/GeneratedProgramFile observations, source membership and
19 retained local output files were verified. The first capture failure remains preserved. The six
remaining integration contexts were started serially. Management's first attempt then stopped at
Csc: FULL-061 uses xUnit v3 `TestContext`, while the assigned integration project uses xUnit v2.
The validation owner is checking the original runner context and preparing a genuine compatible
project assignment. No shim, body edit or SDK-project change is authorized. The five later contexts
were not started. Preserve successful checkpoint identities
across later profile changes. Before
any subsequent ordinary SDK test lane, restore uninjected outputs through the final clean solution build.

The reviewed FULL-061 reassignment passed in the genuine Management unit project, and the remaining
Management integration bodies also passed, both with zero warnings under `d7502629…`. There are now
291 accepted additional body compilations across six contexts. Security Domain's first attempt then
failed at Csc on five missing module-local test-extension resolutions in FULL-302/303/306. The real
helper namespace exists in that project; a generated-import correction is being prepared. The four
later contexts remain unrun. No accepted body was changed and no injected assembly was executed.
