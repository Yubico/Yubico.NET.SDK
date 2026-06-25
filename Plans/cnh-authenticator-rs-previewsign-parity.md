# cnh-authenticator-rs-extension previewSign Parity Report

**Date:** 2026-04-23
**Investigated:** cnh-authenticator-rs-extension @ commit `c83cbce` (2026-04-09), local path `/Users/Dennis.Dyall/Code/y/cnh-authenticator-rs-extension`
**Crate:** `sign-extension-host` v0.1.0 ("Native messaging host for previewSign FIDO bridge"), edition 2021; vendored library `authenticator` under `native/deps/`
**Verdict:** **HARDWARE-PROVEN** — Registration **and** Authentication; signature is returned and printed by an interactive hardware test (`hid-test` binary)

## Findings

**Code paths:** Full implementation for both registration and authentication, with a working hardware harness that exercises the round-trip and surfaces the signature.

- **Registration (`MakeCredential` with `generateKey`):** `native/deps/authenticator/src/ctap2/commands/make_credentials.rs:66` — parses the `previewSign.generateKey` input, returns the unsigned attestation object in CBOR response key 6.

- **Authentication (`GetAssertion` with `signByCredential`):** Two layers:
  - High-level parsing: `native/crates/host/src/webauthn.rs:420-457` — parses the `signByCredential` map, validates `keyHandle` / `tbs` / `additionalArgs`.
  - Low-level wire encoding (the upstream reference): `native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323`. Verbatim:
    ```rust
    if let Some(ref sign) = self.sign {
        // Build CBOR map with integer keys for each credential's signing inputs.
        // Format: "previewSign" => {2: kh, 6: tbs, 7: additional_args}
        // When there's one credential, it's a single map.
        // For multiple, it should select based on allow_list (done in statemachine).
        // For now, serialize the first entry.
        use std::collections::BTreeMap;
        let mut sign_map = BTreeMap::new();
        if let Some(first) = sign.sign_by_credential.first() {
            log::debug!(
                "previewSign GetAssertion: kh={} bytes, tbs={} bytes, args={:?} bytes",
                first.key_handle.len(),
                first.tbs.len(),
                first.additional_args.as_ref().map(|a| a.len()),
            );
            sign_map.insert(
                serde_cbor::Value::Integer(2),
                serde_cbor::Value::Bytes(first.key_handle.clone()),
            );
            sign_map.insert(
                serde_cbor::Value::Integer(6),
                serde_cbor::Value::Bytes(first.tbs.clone()),
            );
            if let Some(ref args) = first.additional_args {
                sign_map.insert(
                    serde_cbor::Value::Integer(7),
                    serde_cbor::Value::Bytes(args.clone()),
                );
            }
        } else {
            log::warn!("previewSign GetAssertion: sign_by_credential is EMPTY");
        }
        map.serialize_entry("previewSign", &serde_cbor::Value::Map(sign_map))?;
    }
    ```

**Wire-format contract (canonical, from the encoder above):**

| CBOR key | Type | Meaning |
|---|---|---|
| `2` (integer) | `bytes` | `key_handle` |
| `6` (integer) | `bytes` | `tbs` (to-be-signed; in `hid-test` this is `Sha256(raw_tbs)`, 32 bytes) |
| `7` (integer, optional) | `bytes` | `additional_args` — for ARKG, the CBOR `COSE_Sign_Args` map `{3: alg, -1: arkg_kh, -2: ctx}` |

The map is wrapped under the string key `"previewSign"` inside the GetAssertion extensions map. `BTreeMap` ordering means keys serialize in ascending integer order: 2, 6, 7. The `serde_cbor` `Value::Bytes` produces standard CBOR major-type 2 byte strings.

**Hardware tests:** ✅ **HARDWARE-PROVEN** — the `hid-test` binary calls a real YubiKey, derives an ARKG key, signs `"Hello, previewSign v4!"`, and prints the resulting signature.

- Request build: `native/crates/hid-test/src/main.rs:257-294` — constructs `signByCredential` with `key_handle = gk.key_handle`, `tbs = Sha256(b"Hello, previewSign v4!").to_vec()`, `additional_args = encode_arkg_sign_args(COSE_ALG_ESP256_ARKG, &derived.key_handle, arkg_ctx)`.
- Touch prompt: `:330-331` — `>>> Touch your YubiKey again <<<`.
- Signature receive + print: `:366-379`. Verbatim:
  ```rust
  match result_rx2.recv() {
      Ok(Ok(sign_result)) => {
          println!("\n--- GetAssertion Result ---");
          println!("  Signature:     {} bytes", sign_result.assertion.signature.len());
          println!("  Signature hex: {}", hex(&sign_result.assertion.signature));

          if let Some(ref sign_out) = sign_result.extensions.sign {
              if let Some(ref sig) = sign_out.signature {
                  println!("  previewSign signature: {} ({} bytes)", hex(sig), sig.len());
              } else {
                  println!("  previewSign: no signature in output");
              }
          } else {
              println!("  No sign extension outputs");
          }
      }
      ...
  }
  ```

**Cross-platform Python harness:** `scripts/test_previewsign.py:131-138` exercises the same registration + authentication flow against `webauthn.dll` on Windows and HID transport elsewhere. Secondary reference, not required for the C# port.

**Constraints / what is NOT proven by Rust:**
- `hid-test` exercises `signByCredential` with **exactly one entry** (single-credential auth). Multi-credential probe-selection per CTAP §10.2.1 step 7 is **not** demonstrated. The encoder comment at `get_assertion.rs:294` admits: *"For multiple, it should select based on allow_list (done in statemachine). For now, serialize the first entry."* — implying the multi-credential path is unimplemented even in Rust's high-level driver.
- ARKG-specific: `additional_args` carries an ARKG `COSE_Sign_Args` payload. Use cases without ARKG would omit key 7. Both shapes are valid per the encoder.

## Citations

- `native/deps/authenticator/src/ctap2/commands/get_assertion.rs:290-323` — wire-format ground truth (integer keys 2/6/7, bytes values, BTreeMap ordering)
- `native/crates/hid-test/src/main.rs:257-294` — `signByCredential` request build (single entry, SHA-256 of TBS, ARKG `additional_args`)
- `native/crates/hid-test/src/main.rs:330-331` — user-presence prompt
- `native/crates/hid-test/src/main.rs:366-379` — signature received + printed
- `native/crates/host/src/webauthn.rs:420-457` — high-level GetAssertion previewSign parsing
- `native/deps/authenticator/src/ctap2/commands/make_credentials.rs:66` — registration parsing
- `scripts/test_previewsign.py:131-138` — Python cross-platform harness (secondary)
- Crate metadata: `Cargo.toml` for `sign-extension-host` v0.1.0, last commit `c83cbce` 2026-04-09 13:47:07 +0200

## Recommendation for Phase 9.2

**Adopts path 2A.** The Rust encoder is the upstream reference required by the principle "only ship what an upstream reference has proven works on hardware." Port the integer-keyed CBOR map structure and the SHA-256 TBS preprocessing into `PreviewSignAdapter.BuildAuthenticationCbor`.

**Note on the C# bug:** The diagnostic at `PreviewSignTests.cs:101-107` reports that C# already uses keys 2/6/7. The persisting `Invalid length (0x03)` therefore likely lives in **byte-string length headers**, **outer wrapping**, or **omission of `additional_args` for ARKG-signed inputs** — not in the key choice. Engineer must do a byte-by-byte diff against `serde_cbor`'s output for an identical input.

**Not adopted by this report:**
- Multi-credential probe (CTAP §10.2.1 step 7) — unproven by Rust as well; defers to Phase 10.
