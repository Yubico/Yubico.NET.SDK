# YubiKit documentation voice and editorial reference

## Status

The user approved the two passages below during planning. They establish the desired voice and were applied in the OATH-001 documentation edit batch. Source line numbers may change. The examples are style references, not exemptions from future factual verification.

## Voice

Write as an experienced engineer explaining the SDK to another capable engineer who is unfamiliar with this codebase. Be direct, precise, and approachable. Include the context needed to act correctly.

**Direct, calm, and explicit. Use natural sentences, preserve important distinctions, and spend words where they save the reader effort.**

## Editorial rules

- Lead with behavior or the reader's task. Remove introductory throat-clearing.
- Name the actor: caller, session, SDK, device, or operating system.
- Use exact terminology consistently. Explain unfamiliar domain terms where needed.
- Keep conditions near the claims they qualify. Firmware, transport, ownership, lifetime, and cancellation qualifications are part of the meaning.
- Use paragraphs for explanations, numbered steps for procedures, and tables for genuine comparisons.
- Prefer concrete statements to vague assurances and unsupported absolutes.
- Retain useful rationale and examples. Remove repetition, obvious code narration, and obsolete development history.
- Optimize for understanding, not minimum word count. Expand compressed text when it makes responsibilities or behavior clearer.
- Keep useful local context even when a canonical explanation exists elsewhere. Do not make API documentation depend on excessive link-following.
- Use present-tense v2 behavior in active reference material. Put useful transition history in migration guidance.
- Do not introduce warnings, headings, lists, or introductory/concluding summaries merely to make a small passage look structured.
- Never remove a technical qualification just to improve flow. Return disputed meaning to investigation.

## Adapt to the document's purpose

| Surface | Editorial objective |
| --- | --- |
| API/XML reference | State contracts precisely; retain conditions, ownership, exceptions, and useful cross-references |
| How-to guide | Address the reader directly; explain prerequisites and the path to completing a task |
| Tutorial | Introduce concepts in the order needed for a guided learning experience |
| Architecture explanation | Explain rationale, constraints, and trade-offs |
| Internal comment | Explain local intent or a non-obvious constraint, without restating the code |
| Historical record | Preserve historical meaning and clearly identify status; avoid implying current authority |

## Approved example 1: API reference

Source: `src/Oath/src/IOathSession.cs`, `IsLocked` documentation, lines 35–46 when curated.

### Original

> Gets whether the **current session** is locked and must call `ValidateAsync` before protected operations can be performed.
>
> This reflects only the current session's unlock state, not whether the device has a password configured at all: it starts `true` when the device has a password configured, and becomes `false` once `ValidateAsync` succeeds — even though the device remains password-protected for the next session. To check whether the device has a password configured, independent of this session's unlock state, use `IsPasswordProtected` instead.

### Approved proposed wording

> Gets whether the current session must be unlocked before performing protected operations.
>
> When a password is configured, the session starts locked. Call `ValidateAsync` to unlock it. Successful validation sets this property to `false` without removing the password.
>
> To check whether a password is configured, use `IsPasswordProtected`.

### Editorial lesson

State the contract first, then explain the state transition in the order the caller encounters it. Preserve the difference between unlocking a session and removing password protection. Shortening to “Gets the session lock state” would lose useful guidance.

Planning evidence: session initialization sets `IsLocked` from the returned challenge; successful validation sets it to `false` without clearing `IsPasswordProtected`. Recheck current code before applying the wording.

## Approved example 2: usage guide

Source: `src/Oath/README.md`, password-protection introduction, line 88 when curated.

### Original

> `DeriveKey` runs PBKDF2 over the device salt, so it needs a live session. Both buffers are yours to zero.

### Approved proposed wording

> Use `DeriveKey` on an open OATH session to derive an access key from a UTF-8-encoded password. The method uses the device’s salt.
>
> You own both the password buffer and the returned key. Clear each with `CryptographicOperations.ZeroMemory` when it is no longer needed, including when an operation fails. The example below uses a `finally` block to clear both buffers.

### Editorial lesson

Start with the task, name both buffers, and make cleanup actionable. The longer wording requires less inference. The exact derivation algorithm belongs in the API reference when it does not help the immediate task.

Planning evidence: `OathSession.DeriveKey` uses the session salt and rejects disposed sessions; the README example clears both arrays in `finally`. Recheck current code and the neighboring example before applying the wording.

## External references

Use these as supporting guidance, not sources of SDK behavior:

- Primary prose reference: [Google developer documentation style highlights](https://developers.google.com/style/highlights).
- Supporting clarity guidance: [Microsoft: simple and human](https://learn.microsoft.com/en-us/style-guide/brand-voice-above-all-simple-human).
- Document-purpose guidance: [Diátaxis](https://diataxis.fr/).

The approved local examples settle routine voice questions. External guides do not justify repeated preference-driven rewrites or an unsolicited repository-wide reorganization.

## Editorial acceptance check

Can the intended reader find the main point, understand the necessary conditions, identify their responsibilities, and act correctly without reconstructing the implementation? Is each remaining explanation useful in this location? Has any shortening made the text cryptic or inaccurate?
