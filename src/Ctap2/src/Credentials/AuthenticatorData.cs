// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Yubico.YubiKit.Ctap2.Credentials;

/// <summary>
/// Represents the authenticator data structure as defined in WebAuthn/CTAP specifications.
/// </summary>
/// <remarks>
/// <para>
/// The authenticator data is a binary structure that contains:
/// - 32 bytes: RP ID hash (SHA-256)
/// - 1 byte: Flags
/// - 4 bytes: Signature counter (big-endian uint32)
/// - (optional) Attested credential data
/// - (optional) Extensions data (CBOR)
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#sctn-authenticator-data
/// for the base layout. BE/BS meanings are defined in WebAuthn Level 3, Recommendation 25 August
/// 2026, §6.1.3 (https://www.w3.org/TR/2026/REC-webauthn-3-20260825/#sctn-credential-backup).
/// </para>
/// <para>
/// Parse copies the full input, RP hash, attested fields and extension suffix into independent storage.
/// RawData preserves authenticator data, not a surrounding CTAP response. Extension/raw bytes can contain
/// secret-derived material; this type has no disposal/zeroing API. Do not log that material or assume
/// clearing the original input or disposing a session clears these copies.
/// </para>
/// <para>
/// Flags report received bits, not independently verified events. Parsing and RP-hash comparison do not
/// verify a signature, origin/challenge, account binding, requested policy or attestation trust.
/// </para>
/// </remarks>
public sealed class AuthenticatorData
{
    /// <summary>
    /// Minimum size of authenticator data (rpIdHash + flags + signCount).
    /// </summary>
    private const int MinimumLength = 37;

    /// <summary>
    /// Gets the SHA-256 hash of the Relying Party ID.
    /// </summary>
    public ReadOnlyMemory<byte> RpIdHash { get; }

    /// <summary>
    /// Gets the authenticator data flags.
    /// </summary>
    public AuthenticatorDataFlags Flags { get; }

    /// <summary>
    /// Gets the signature counter value.
    /// </summary>
    /// <remarks>
    /// The authenticator can use a per-credential or global counter, or report zero without counter
    /// support. The RP compares stored state and chooses a risk policy (WebAuthn L2 §6.1.1); this SDK
    /// enforces no monotonicity. A counter mismatch is not certain proof of cloning.
    /// </remarks>
    public uint SignCount { get; }

    /// <summary>
    /// Gets the attested credential data, if present (when AT flag is set).
    /// </summary>
    public AttestedCredentialData? AttestedCredentialData { get; }

    /// <summary>
    /// Gets the CBOR-encoded extensions data, if present (when ED flag is set).
    /// </summary>
    /// <remarks>Copies the complete remaining suffix without checking that it is one complete CBOR map.</remarks>
    public ReadOnlyMemory<byte>? Extensions { get; }

    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public ReadOnlyMemory<byte> RawData { get; }

    /// <summary>
    /// Gets whether the received UP bit reports user presence.
    /// </summary>
    public bool UserPresent => Flags.HasFlag(AuthenticatorDataFlags.UserPresent);

    /// <summary>
    /// Gets whether the received UV bit reports user verification.
    /// </summary>
    public bool UserVerified => Flags.HasFlag(AuthenticatorDataFlags.UserVerified);

    /// <summary>
    /// Gets whether the received BE bit reports that this credential is backup eligible.
    /// </summary>
    public bool BackupEligible => Flags.HasFlag(AuthenticatorDataFlags.BackupEligibility);

    /// <summary>
    /// Gets whether the received BS bit reports that this credential is currently backed up.
    /// </summary>
    public bool BackedUp => Flags.HasFlag(AuthenticatorDataFlags.BackupState);

    /// <summary>
    /// Gets a value indicating whether attested credential data is present.
    /// </summary>
    public bool HasAttestedCredentialData => Flags.HasFlag(AuthenticatorDataFlags.AttestedCredentialData);

    /// <summary>
    /// Gets a value indicating whether extension data is present.
    /// </summary>
    public bool HasExtensions => Flags.HasFlag(AuthenticatorDataFlags.ExtensionData);

    private AuthenticatorData(
        ReadOnlyMemory<byte> rawData,
        ReadOnlyMemory<byte> rpIdHash,
        AuthenticatorDataFlags flags,
        uint signCount,
        AttestedCredentialData? attestedCredentialData,
        ReadOnlyMemory<byte>? extensions)
    {
        RawData = rawData;
        RpIdHash = rpIdHash;
        Flags = flags;
        SignCount = signCount;
        AttestedCredentialData = attestedCredentialData;
        Extensions = extensions;
    }

    /// <summary>
    /// Parses authenticator data from raw bytes.
    /// </summary>
    /// <param name="data">The raw authenticator data bytes.</param>
    /// <returns>The parsed AuthenticatorData.</returns>
    /// <remarks>
    /// Checks the 37-byte prefix and parses attested data when AT is set. With ED set, the remaining
    /// bytes are copied without extension-map validation; with ED unset, trailing bytes are retained
    /// in RawData but otherwise ignored. It does not enforce all flag relationships or ceremony rules.
    /// </remarks>
    /// <exception cref="ArgumentException">The fixed prefix or declared attested fields are too short.</exception>
    /// <exception cref="System.Formats.Cbor.CborContentException">An extracted attested CBOR value cannot be read.</exception>
    public static AuthenticatorData Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinimumLength)
        {
            throw new ArgumentException(
                $"Authenticator data must be at least {MinimumLength} bytes, got {data.Length}.",
                nameof(data));
        }

        var rawData = data.ToArray();
        int offset = 0;

        // Parse RP ID hash (32 bytes)
        var rpIdHash = data.Slice(offset, 32).ToArray();
        offset += 32;

        // Parse flags (1 byte)
        var flags = (AuthenticatorDataFlags)data[offset];
        offset += 1;

        // Parse signature counter (4 bytes, big-endian)
        var signCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Parse attested credential data if present
        AttestedCredentialData? attestedCredentialData = null;
        if (flags.HasFlag(AuthenticatorDataFlags.AttestedCredentialData))
        {
            var remaining = data[offset..];
            attestedCredentialData = AttestedCredentialData.Parse(remaining, out var bytesRead);
            offset += bytesRead;
        }

        // Parse extensions if present
        ReadOnlyMemory<byte>? extensions = null;
        if (flags.HasFlag(AuthenticatorDataFlags.ExtensionData))
        {
            // Preserve the ED suffix; this extraction does not validate its CBOR map or full consumption.
            extensions = data[offset..].ToArray();
        }

        return new AuthenticatorData(
            rawData,
            rpIdHash,
            flags,
            signCount,
            attestedCredentialData,
            extensions);
    }

    /// <summary>
    /// Parses authenticator data from memory.
    /// </summary>
    /// <param name="data">The raw authenticator data.</param>
    /// <returns>The parsed AuthenticatorData.</returns>
    /// <remarks>Copies storage and applies the same parsing limits as the span overload.</remarks>
    public static AuthenticatorData Parse(ReadOnlyMemory<byte> data) => Parse(data.Span);

    /// <summary>
    /// Compares the received RP ID hash with SHA-256 of the UTF-8-encoded expected RP ID.
    /// </summary>
    /// <param name="rpId">The expected RP ID string.</param>
    /// <returns>True if the hash matches, false otherwise.</returns>
    /// <remarks>A match is only this hash comparison, not origin authorization or complete RP assertion verification.</remarks>
    public bool VerifyRpIdHash(string rpId)
    {
        ArgumentException.ThrowIfNullOrEmpty(rpId);

        Span<byte> expectedHash = stackalloc byte[32];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rpId), expectedHash);

        return CryptographicOperations.FixedTimeEquals(expectedHash, RpIdHash.Span);
    }
}

/// <summary>
/// Flags for authenticator data as defined in WebAuthn specification.
/// </summary>
[Flags]
public enum AuthenticatorDataFlags : byte
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// User present result (UP).
    /// </summary>
    UserPresent = 0x01,

    /// <summary>
    /// Reserved for future use (RFU1).
    /// </summary>
    RFU1 = 0x02,

    /// <summary>
    /// User verified result (UV).
    /// </summary>
    UserVerified = 0x04,

    /// <summary>
    /// Backup eligibility (BE).
    /// </summary>
    BackupEligibility = 0x08,

    /// <summary>
    /// Backup state (BS).
    /// </summary>
    BackupState = 0x10,

    /// <summary>
    /// Reserved for future use (RFU2).
    /// </summary>
    RFU2 = 0x20,

    /// <summary>
    /// Attested credential data included (AT).
    /// </summary>
    AttestedCredentialData = 0x40,

    /// <summary>
    /// Extension data included (ED).
    /// </summary>
    ExtensionData = 0x80
}