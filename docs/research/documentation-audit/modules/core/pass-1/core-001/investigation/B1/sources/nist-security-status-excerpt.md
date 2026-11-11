# Original NIST passage retrieved through Exa

Retrieved November 11, 2026 as the single focused follow-up after direct PDF HTTP 403. Source URL: https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73pt2-5.pdf. Title: *Interfaces for Personal Identity Verification: Part 2 – PIV Card Application Card Command Interface*, July 2024 final, §2.4.2. This is retained extraction text, rather than authenticated PDF bytes.

> A successful execution of an authentication protocol SHALL set the security status indicator associated with the credential used in the protocol to TRUE. An aborted or failed execution of an authentication protocol SHALL set the security status indicator associated with the credential used in the protocol to FALSE.

> A security status indicator SHALL be said to be a global security status indicator if it is not changed when the currently selected application changes from one application to another. In essence, when changing from one application to another, the global security status indicators SHALL remain unchanged.

> A security status indicator is said to be an application security status indicator if it is set to FALSE when the currently selected application changes from one application to another. Every security status indicator is either a global security status indicator or an application security status indicator. The security status indicators associated with the PIV Card Application PIN, the PIN Unblocking Key (PUK), OCC, pairing code, and the PIV Card Application Administration Key are application security status indicators for the PIV Card Application, whereas the security status indicator associated with the Global PIN is a global security status indicator.

The original publication record returned by the same search identifies July 15, 2024 final publication and supersession of SP 800-73-4. The excerpt supports the application/global-status distinction. Cross-process handle behavior and YubiKey-specific policy require separate platform/vendor evidence.
