# Wave2 documentation-impact re-review

Reviewer: fresh Sol6.1 `ses_e5eafbacbffe0WyCMY38PG6uW9`, read-only against the combined staged/unstaged parent tree. Approved voice from `style.md`; documentation-first scope and OATH behavior exception explicit.

## Coverage

- Core Base32 public XML, README and contributor guidance.
- OATH credential/request XML, session XML, README, contributor/test guidance, changed CLI help/reset messages.
- OATH-only exceptions in Tests.Shared README/CLAUDE.
- New OATH migration sections and updated exception map entry, neighboring password-retry recipe, and focused searches for invalidated current active/migration claims.
- Source dependencies: combined decoder/identity/numeric validation, Code/time flow, terminalreset/disposal ownership, retry exception propagation and relevant tests.
- Historical snapshots and other application claims were not rewritten. The earlier suggestion about gaps.md:300 concernedYubiHSMAuth, notOATH.

## Required corrections and closure

1. Password-retry migration recipe still mapped failures only toOathException. Corrected to distinguish device-reported rejection and device-HMAC BadResponseException, includingWrongPasswordqualification.
2. AuthenticateAndRetry genericXML retainedoldpassword-failuredescription. CorrectedOathExceptionprovenance andaddedBadResponseExceptionentry; nongenericoverloadcrossreferencesit.
3. Activev1comparison stillsaidBase32wasnotpublic. Correctedtoidentifythepublicdecode-onlyexception andlinkitsownershipguidance, preservingthebroaderremovedcodec distinction.

Re-review: **PASS**, allthreeclosed; nonewfactualinaccuraciesfoundinneighboringguidance. No runtimechangeswere madeforthese prosecorrections.

## Checks and limits

- Combinedfullbuild/unit/docs/architecture/complexitygate:3,998unitexecutions,0failures,5existingskips,0warnings; OATH436.
- AfterXML/prosecorrections:fullbuild+docsQApassed0warnings.
- Newmigrationanchorswerecheckedagainstactualheadings. NewerPCSCREADMElinkpreserved.
- Existingdeviceevidenceisexplicitlybounded:125/5.8.0.alpha.2,19live+13offlinecases,7skips; SCPinitialization6A88beforeRESET doesnotverifyliveSCPreset. Finalcombinedhardwaregateispendingseparately.
