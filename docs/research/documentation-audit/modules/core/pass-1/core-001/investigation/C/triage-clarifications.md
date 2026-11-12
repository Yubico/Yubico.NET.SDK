# Packet C triage clarifications

This addendum clarifies the completed records checkpointed at `bb94`. It preserves
their historical wording, hashes, candidate IDs and dispositions. Packet C remains
complete for static investigation: **104 primary files and 176 shared IDs**. The
earlier 174 figure was prose orientation; all 176 actual assigned IDs were read and
dispositioned. Central prose reconciliation adds no units or new coverage claim here.

## Exact supersession map

| Concern | Earlier wording to interpret historically | Authoritative final observation |
| --- | --- | --- |
| `CORE-C-C007` | `findings.json` → candidate `CORE-C-C007` → `confidence`: “exact BER clauses still require original-source review” | The same candidate's `final_observation` ([line 282](findings.json#L282)); `final-review.json` → `candidate_addenda["CORE-C-C007"]` ([line 10](final-review.json#L10)) |
| `CORE-C-C011` | The broad supplied-Q warning in `ECPrivateKey.cs:196–211`, including the implication that supplying Q necessarily takes the same macOS derivation route | Candidate `CORE-C-C011` → `final_observation` ([line 510](findings.json#L510)); `final-review.json` → `candidate_addenda["CORE-C-C011"]` ([line 14](final-review.json#L14)) |
| `CORE-C-C013` | `findings.json` → candidate `CORE-C-C013` → `confidence`: “AsnWriter/provider internals need exact revision follow-up” | The same candidate's `final_observation` ([line 629](findings.json#L629)); `final-review.json` → `candidate_addenda["CORE-C-C013"]` ([line 16](final-review.json#L16)) |

These are scoped supersessions. C007's completed investigator source reading does
not replace independent review. C011's other managed-copy concerns remain intact.
For C013, the AsnWriter follow-up is complete; actual provider qualification remains
separate. The original candidate confidence fields stay unchanged for provenance.

## C007 — original BER passages were inspected

The final conclusion uses **ITU-T X.690 (02/2021), ISO/IEC 8825-1:2021**:
§8.1.2.2/§8.1.2.4 specify low-tag and continuation-octet forms, and
§8.1.3.4–§8.1.3.6 specify short/long/indefinite length forms. The long-form rule
forbids FF; Note 2 permits a sender to use more length octets than the minimum.
This supports the recorded exact overflow/truncation fixes and accepted redundant
long-form lengths. Single-item remainder admission, bounded tag handling and negative
`BerLength` input remain separate implementation/policy concerns.

Evidence anchors are the [final register's X.690 row](sources/final-source-register.md#L18),
the retained `sources/x690-202102.txt:492–597` reading derivative, and original PDF
identity in `sources/ber-source-attempts.json`. The implementation observations concern
`src/Core/src/Utilities/Tlv.cs:178–259` and `BerLength.cs:17–55`. Independent review
must recheck the original clauses and affected units. No parser experiment or SDK test
was executed, and the general BER rules do not make this bounded helper a general codec.

## C011 — narrow the supplied-Q warning to the checked route

The pinned **.NET runtime v10.0.0** source routes absent Q through
`ExtractPublicKeyFromPrivateKey`, which replaces the ECParameters value with decoded
parameters containing a new managed D array. Supplying Q skips that extraction in the
checked source. `ImportKey` returns its CryptoPool staging in `finally`, and signing
uses the imported SecKey. The broad supplied-Q derivation warning therefore exceeds
the inspected evidence. This narrows the documentation claim without promising that
all runtime/native private material is erased.

The [final register's macOS row](sources/final-source-register.md#L48) points to retained
`sources/dotnet-v10.0.0-EccSecurityTransforms.cs:184–232,242–269`,
`dotnet-v10.0.0-EccSecurityTransforms.macOS.cs:52–79`, and
`dotnet-v10.0.0-ECDsaSecurityTransforms.cs:74–102`. Their identities are in
`sources/provider-source-manifest.json`. Independent review must check those routes
against the SDK call sites. Installed-runtime revision, SecKey/native-memory behavior
and actual provider cleanup remain unqualified; no runtime or native execution occurred.

## C013 — pinned AsnWriter source settles the internal-copy question

The retained **.NET runtime v10.0.0 AsnWriter** source shows that `Encode()` returns an
independent copy while leaving writer storage intact; `Reset()` clears the written
internal prefix. Production private encoders omit Reset. Existing observer tests cover
explicit INTEGER/encoded temporary arrays, rather than the writer's private storage.
This is the final source-backed cleanup conclusion, with no failure injection or fix.

Evidence anchors are the [final register's AsnWriter row](sources/final-source-register.md#L47),
`sources/dotnet-v10.0.0-AsnWriter.cs:79–99,185–209`, its identity in
`sources/completion-fetch-manifest.json`, and
`src/Core/src/Cryptography/AsnPrivateKeyEncoder.cs:68–284`. Independent review must
recheck the pinned implementation, encoder scopes and observer limits. Actual runtime/
provider erasure and exceptional-path behavior remain separate qualification work.

This clarification adds no accepted finding, product change, new research or execution
result. Fresh editing and independent review remain later gates; existing reports,
candidate/unit ledgers and source manifests retain their original bytes.
