# WA-SAMPLE-EXPAND — external WebAuthn consumer snippets

## Outcome and boundary

Baseline: `e353b49afa15d1d104fce70bbf1e1abc59ddb132` (`e353b49a`). This is an independent, targeted expansion of the consumer-snippet category after Astra found a factual mismatch in a file previously marked clean. It is not a second review of WA-A/WA-B implementation documentation or of unrelated applet examples.

**One required correction in packet C's external file set is confirmed:** `docs/usage/user-interaction.md:98` supplies a cancellation token in the PIN parameter's position. **One identical companion defect was found in a directly relevant Core XML example:** `src/Core/src/Credentials/UserPresenceIntent.cs:29`. The latter is outside C's enumerated file list and needs explicit coordinator assignment; it has not been edited.

All 25 external active-discovery files and four supplemental migration files listed in `packet-c.md` were screened for this category. Supplemental filenames resolve under `docs/migration/`, not the repository root. The screening included fenced block contents and inline API examples, receiver names such as `client` without a `WebAuthn` keyword on the invocation line, and `MakeCredentialAsync`, `GetAssertionAsync`, construction/options, and selection symbols. Relevant surrounding sections were then read. There is only one fenced invocation of the WebAuthn consumer ceremony API in this external set. Migration files also contain constructor/factory and result-migration sketches, reviewed below.

Repository evidence was read from the baseline with `git show e353b49a:<path>`, not an editor's in-flight diff. Root and WebAuthn instructions and the approved `style.md` were read; Core/Ctap2/PublicApi/Tests.Shared instructions were consulted where relevant. No repository files, tests, builds, device operations, generated artifacts, commits, or delegated work were performed. Only this report was written.

## Required fix: WA-SAMPLE-EXPAND-001

**Location:** `docs/usage/user-interaction.md:88–118`, especially the complete scope example at **95–100**, invocation **98**.

Current example:

```csharp
using (UserPresenceIntent.BeginScope("approve the transfer of 500 EUR"))
{
    await client.GetAssertionAsync(options, cancellationToken);
}
```

**Exact signature evidence:** `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:47–50` declares:

```csharp
public async Task<IReadOnlyList<MatchedCredential>> GetAssertionAsync(
    PublicKeyCredentialRequestOptions options,
    ReadOnlyMemory<byte>? pinBytes = null,
    CancellationToken cancellationToken = default)
```

The second positional argument binds to `pinBytes`; it cannot skip that parameter based on the argument's type. A `CancellationToken` is not a `ReadOnlyMemory<byte>?`. This is a source-level compile mismatch, not an editorial preference and not an observed test failure. The one public ceremony signature is the relevant contract; there is no cancellation-token-as-second-argument overload to rescue the example.

**Minimum required correction:**

```csharp
await client.GetAssertionAsync(options, cancellationToken: cancellationToken);
```

**Recommended small local clarification while editing this block:** identify `client` as an already configured `WebAuthnClient` and use `requestOptions` for its `PublicKeyCredentialRequestOptions`. The earlier example at lines 20–23 defines `options` as `SessionCreationOptions`; those session-creation options are not valid ceremony request options. These blocks can reasonably be read as independent fragments, so this is not a second proven compile defect, but naming the prerequisite prevents an avoidable ambiguity. For example:

> With an existing `WebAuthnClient` and authentication request options, wrap the ceremony call in an intent scope. Configure its underlying CTAP2 session with `SessionCreationOptions.UserPresencePrompt` when creating the client or session.

```csharp
using (UserPresenceIntent.BeginScope("approve the transfer of 500 EUR"))
{
    await client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken);
}
```

Do not turn this scoped intent example into another full authentication tutorial. Ignoring the returned list is acceptable here because the example teaches notification scoping, not assertion submission. If the editor instead expands it to consume results, handle the count before indexing and obtain the chosen response with `SelectAsync`; do not present a touch notification itself as transaction authorization or RP verification.

### Surrounding-block review

- **Actor/responsibility distinction is sound:** lines 90–93 and 137–142 assign intent wording to the application, not the SDK, and distinguish the application call from its underlying device operations. Lines 186–188 explicitly prohibit using presence notification to authorize an operation or infer that touch occurred. No additional authorization-warning section is necessary for this fix.
- **Scope lifetime is sound:** `using` keeps the intent active while the ceremony is awaited, then disposes it on success or exception. `UserPresenceIntent.BeginScope` returns `IDisposable` (`src/Core/src/Credentials/UserPresenceIntent.cs:73–79`); disposal removes the scope (`105–115`). This is not an asynchronously disposable owner.
- **No deferred-device-I/O scope bug:** baseline `WebAuthnClient.Authentication.cs:104–116` builds the authentication responses before returning matches; `MatchedCredential` receives `Task.FromResult(authenticationResponse)`. Therefore the awaited `GetAssertionAsync` is the correct operation to place inside this intent scope. Moving the scope only around later `SelectAsync` would miss the ceremony's device work.
- **Options and prompt lifetimes:** this fragment owns only the intent scope; it neither constructs nor disposes a client/session/options object. The page already states at lines 197–200 that the caller owns the retained user-presence prompt and must keep it alive until sessions end. Do not suggest placing the prompt in `WebAuthnClientOptions`: the factory's `sessionOptions` configures the underlying session (`src/WebAuthn/src/IYubiKeyExtensions.cs:58–70`), while client options are forwarded separately (`72–75`).
- **PIN ownership:** the corrected call omits `pinBytes`; it neither allocates a PIN nor transfers ownership of a secret buffer. A needed PIN must come from the client's configured credential prompt, otherwise the ceremony can fail. The guide's introduction at lines 6–13 already separates those contracts. No PIN literal or cleanup example should be added merely to illustrate intent.
- **Callback examples are clean for this category:** lines 75–86 and 102–118 use existing `UserPresenceOperations.Ctap2.MakeCredential/GetAssertion` constants, retain a fallback branch, and format application intent next to the SDK operation. Exact constants are in `src/Core/src/Credentials/UserPresenceOperations.cs:28–40`. `OnUserPresenceRequestedAsync(UserPresenceContext, CancellationToken)` matches `IUserPresencePrompt.cs:46–48`; omitting a resolution override is legal because it has a default implementation at `63–66`. The callback returns promptly rather than waiting for touch.
- **Citations:** this local example and its explanatory paragraphs contain no normative WebAuthn/CTAP specification citation requiring version/section reconciliation. They demonstrate an SDK-local ambient display-text contract, not a WebAuthn transaction-confirmation extension.

## Companion fix: WA-SAMPLE-EXPAND-002 (coordinator assignment needed)

**Location:** `src/Core/src/Credentials/UserPresenceIntent.cs:20–31`, exact invocation **29**.

The XML `<code>` block repeats `await client.GetAssertionAsync(options, cancellationToken);`. It has the identical signature mismatch and requires the same named argument. This was discovered while verifying the SDK contract directly used by C's example, not by sweeping Core implementation documentation.

**Proposed one-line documentation-only correction:** `await client.GetAssertionAsync(options, cancellationToken: cancellationToken);`.

This file is **not** in packet C's listed external files. Add it explicitly to an editor's authorized ownership before editing, or record a precise follow-up; do not silently claim the category is fully closed while leaving the duplicate published XML example broken. No Core implementation change is implicated. The `requestOptions` naming clarification can be mirrored if applied to the usage guide.

## Example-bearing file dispositions

“Clean” below means clean for **external WebAuthn consumer snippet syntax and its immediate usage context**, not a whole-file clean bill of health.

| File and baseline lines | Disposition / evidence |
| --- | --- |
| `docs/usage/user-interaction.md:75–118` | Required fix at 98 (001). Two related callback/switch blocks clean as described above. Keep caller-owned prompt lifetime at 197–200. |
| `docs/migration/v1-to-v2.md:110–111,144,213–214,679–688` | Clean. New-side factory shape places required `WebAuthnClientOptions` after origin, and optional session options before cancellation. `RpIdValidator = applicationValidator` is explicitly an application-provided placeholder, not a permissive validator implementation. Removed shapes are marked historical/before. Output migration correctly uses property initializers and list count plus `SelectAsync`; it does not index an unchecked list. Current prose assigns RP syntax and binding policy to the application and preserves PIN prompt/attempt settings. No fenced WebAuthn ceremony recipe occurs in this file. |
| `docs/migration/v1-to-v2-map.yml:517–531,556–580,628–649` | Clean at the textual WebAuthn sketch level. Constructor at 522 matches `(ctap2Session, origin, options)`; factory sketch at 561 matches origin/client-options/session-options/cancellation order. `...`, bare parameter types, and member lists are clearly mapping notation, not compilable standalone C#. Required callback, optional PIN prompt, bounded SDK retry responsibility, and count-based selection migration are preserved. Line 567's removed suffix-checker narrative is historical and explicitly qualified as such by 580; do not rewrite it as a new consumer example. YAML parsing is not claimed. |
| `src/Core/src/Credentials/UserPresenceIntent.cs:26–31` | Companion outside packet list: required fix at 29 (002). Otherwise the scope example and caller/SDK distinction agree with the usage guide. |

No additional factual mismatch was found in the enumerated **consumer** examples. In particular, do not misclassify lower-level `session.MakeCredentialAsync` / `session.GetAssertionAsync` examples as WebAuthn-client overloads merely because surrounding prose mentions WebAuthn.

## Complete packet-C external inventory for this category

| External file | Category disposition |
| --- | --- |
| `2026-09-21-yubikit-async-boundaries-ISA.md` | Historical/specification/evidence artifact; screened blocks and WebAuthn references contain no consumer ceremony example. Its source paths/scan commands are not current consumer recipes. |
| `CLAUDE.md` | WebAuthn module/toolchain references only; no WebAuthn consumer code block. Root behavioral instructions read. |
| `docs/architecture/applet-public-api.md` | Facade-convention reference only; no WebAuthn consumer example. |
| `docs/architecture/physical-device-model.md` | WebAuthn transport table; code blocks use Core/Management/PIV, not WebAuthn. No consumer snippet to expand here. |
| `docs/architecture/raw-access-tiers.md` | Explicitly lower-level raw CTAPHID/Core examples; no WebAuthn-client consumer block. |
| `docs/architecture/sdk-architecture-diagrams.md` | WebAuthn dependency/transport diagram labels, no WebAuthn consumer example. CTAP2 sequence diagram is not a WebAuthn overload example. |
| `docs/architecture/webauthn-extension-processing.md` | Complete page read to classify its blocks. Lines 83–106 are an internal interface/type schematic, and inline ceremony-flow calls are implementation architecture, not consumer snippets. Existing broader C findings on this page are neither reopened nor declared clean by this review. |
| `docs/docs-inventory-report.md` | Explicit generated historical snapshot; no consumer example. Preserve historical paths. |
| `docs/NATIVE-AOT.md` | WebAuthn support-status mentions; no WebAuthn consumer code. Publish/property blocks are a separate category. |
| `docs/SDK-HOUSE-STYLE.md` | WebAuthn delegation/DTO guidance; no WebAuthn consumer example. |
| `docs/TESTING.md` | WebAuthn test-command/hardware coordination examples only; no consumer ceremony block. No commands were executed or CLI claims re-audited. |
| `docs/usage/user-interaction.md` | Example-bearing: required fix 001; related callback snippets reviewed clean. |
| `docs/v1-to-v2-comparison.md` | WebAuthn package/prompt prose, no consumer code block. |
| `docs/v2-highlights.md` | WebAuthn overview prose, no consumer code block. |
| `README.md` | WebAuthn package link only; its consumer code example is Management, not WebAuthn. |
| `src/Core/README.md` | WebAuthn link / shared touch-operation prose; code blocks are Core/Management/PIV/raw examples, no WebAuthn-client example. |
| `src/Ctap2/CLAUDE.md` | CTAP2 extension examples, including a section headed “Extensions (WebAuthn),” use `ExtensionBuilder` and `MakeCredentialOptions`, not WebAuthn client/request types. No high-level WebAuthn consumer example; unrelated CTAP2 examples not re-audited. |
| `src/Ctap2/examples/FidoTool/README.md` | CTAP2 tool commands plus test-versus-production client-data-hash warning; no WebAuthn client consumer snippet. |
| `src/Ctap2/README.md` | CTAP2 session calls have RP/client-data-hash/options shapes, not WebAuthn client calls. No high-level consumer snippet; unrelated CTAP2 recipes excluded. |
| `src/Ctap2/tests/CLAUDE.md` | CTAP2 session/test examples only, including its “Test WebAuthn Extensions” section; no high-level client snippet. |
| `src/Management/examples/ManagementTool/README.md` | WebAuthn occurs only in a FIDO HID interface description; no relevant example. |
| `src/Management/README.md` | WebAuthn transport comparison row only; no relevant example. |
| `src/PublicApi/CLAUDE.md` | Facade-convention prose only, no consumer example. Instructions read. |
| `src/Tests.Shared/CLAUDE.md` | WebAuthn appears in test-device safety guidance; no WebAuthn consumer example. Instructions consulted; unrelated harness examples excluded. |
| `TOOLCHAIN.md` | WebAuthn runner/filter examples only, not consumer code. No runner behavior re-audit. |
| `docs/migration/v1-to-v2.md` | Example-bearing inline migration API sketches reviewed clean above. |
| `docs/migration/v1-to-v2-map.yml` | Example-bearing textual constructor/factory/result sketches reviewed clean above. |
| `docs/migration/v1-to-v2-changelog.md` | Historical API-shape accounts, including 2026-10-31 current-boundary update and older removed contracts. No runnable WebAuthn consumer snippet; preserve chronology. |
| `docs/migration/v1-to-v2-gaps.md` | Explicit point-in-time comparison with later qualified updates; no WebAuthn consumer code block. No historical feature claims re-audited. |

## Evidence, verification limits, and editor handoff

- **Primary evidence for the defect:** baseline public method declaration, `src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs:47–50`. The source-level argument mismatch is certain without compiling the example; compilation was prohibited and was not attempted.
- **Language supporting reference:** Microsoft C# [Named and optional arguments](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/named-and-optional-arguments), consulted through Context7. Its example uses a named third argument to omit an optional second parameter. This supports C# binding syntax, not SDK behavior.
- **Result/scope supporting evidence:** baseline authentication response creation at `WebAuthnClient.Authentication.cs:104–116`; `MatchedCredential.SelectAsync` at `Client/Authentication/MatchedCredential.cs:88–90`; `UserPresenceIntent.BeginScope`/disposal at `src/Core/src/Credentials/UserPresenceIntent.cs:73–79,105–115`; prompt interface at `IUserPresencePrompt.cs:46–48,63–66`; touch-operation constants at `UserPresenceOperations.cs:28–40`.
- This review does not certify external specification citations in internal architecture diagrams or revalidate WA-C's unrelated prose findings. No normative citation is attached to the defective consumer block. No additional external-spec research was needed to establish this local API mismatch.
- YAML parse validity, compiled snippets, runtime callback timing, OS/device behavior, and live authentication were not verified. The historical YAML parse issue remains outside this category.
- WA-A/WA-B in-flight diffs were not inspected. Their implementations were not re-audited; baseline declarations and the narrow response/scope paths above were used solely as evidence for consumer documentation.
- **C editor:** add 001 to the accepted packet, retaining the current intent/prompt ownership qualifications. **Coordinator:** explicitly assign the Core XML companion 002 or preserve it as an open follow-up. **Fresh re-review:** check both named arguments in the final tree, confirm the request-options naming is not confused with session options, and retain all previously accepted C work. A final clean claim for this category must account for both locations.
