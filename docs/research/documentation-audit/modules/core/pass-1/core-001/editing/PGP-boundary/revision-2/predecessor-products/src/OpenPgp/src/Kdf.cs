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
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Hash algorithms supported by the OpenPGP KDF.
/// </summary>
public enum KdfHashAlgorithm : byte
{
    Sha256 = 0x08,
    Sha512 = 0x0A,
}

/// <summary>
///     Base class for OpenPGP Key Derivation Function (KDF) configuration.
///     Dispatches parsing to <see cref="KdfNone" /> or <see cref="KdfIterSaltedS2k" />.
/// </summary>
/// <remarks>
///     Standalone instances are caller-owned and disposable. A session retains the same object supplied
///     to SetKdfAsync or returned by GetKdfAsync and disposes its current cache during teardown. Keep that
///     object undisposed while the session uses it; retain outer cleanup scopes for replaced/reset objects.
///     KdfIterSaltedS2k disposal clears its backing arrays but does not prevent subsequent Process calls.
///     Disposing an aliased object early can therefore change derivation rather than produce a disposed-object error.
/// </remarks>
public abstract class Kdf : IDisposable
{
    /// <summary>
    ///     The KDF algorithm identifier.
    /// </summary>
    public abstract int Algorithm { get; }

    /// <summary>
    ///     Derives PIN bytes from the given UTF-8 PIN bytes for the specified PIN type.
    /// </summary>
    /// <param name="pw">The PIN type (User, Reset, or Admin).</param>
    /// <param name="pin">
    ///     The borrowed UTF-8 encoded PIN. The caller owns the buffer and must clear it after use.
    /// </param>
    /// <returns>
    ///     The derived PIN bytes. The caller owns the returned array and must zero it with
    ///     <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory(Span{byte})" />.
    /// </returns>
    public abstract byte[] Process(Pw pw, ReadOnlySpan<byte> pin);

    /// <summary>
    ///     Serializes the KDF configuration to its wire format (concatenated TLVs).
    /// </summary>
    /// <returns>Caller-owned bytes, potentially containing initial PIN hashes; clear after transmission/use.</returns>
    public abstract byte[] ToBytes();

    /// <inheritdoc />
    public virtual void Dispose() { }

    /// <summary>
    ///     Parses a KDF configuration from the encoded TLV data (DO 0xF9).
    /// </summary>
    /// <remarks>
    ///     Algorithm 3 requires the modeled S2K fields; absent/other identifiers currently produce KdfNone.
    ///     Malformed TLV or missing required S2K fields can fail parsing. Retain raw F9 data when inspecting
    ///     identifiers beyond the model. The returned object is owned by the caller unless retained by a session.
    ///     Parsing copies TLV fields and clones salts/initial hashes into the result. Result disposal clears
    ///     its backing arrays, not the caller's encoding or the intermediate dictionary field copies, which
    ///     currently have no explicit clearing scope.
    /// </remarks>
    public static Kdf Parse(ReadOnlySpan<byte> encoded)
    {
        var data = TlvHelper.DecodeDictionary(encoded);

        if (data.TryGetValue(0x81, out var algorithmData) && algorithmData.Length > 0)
        {
            var algorithm = algorithmData.Span[0];
            if (algorithm == KdfIterSaltedS2k.KdfAlgorithm)
            {
                return KdfIterSaltedS2k.ParseData(data);
            }
        }

        return new KdfNone();
    }
}

/// <summary>
///     No KDF applied — PINs are sent as raw UTF-8 bytes.
/// </summary>
public sealed class KdfNone : Kdf
{
    /// <inheritdoc />
    public override int Algorithm => 0;

    /// <inheritdoc />
    public override byte[] Process(Pw pw, ReadOnlySpan<byte> pin) =>
        pin.ToArray();

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        using var tlv = new Tlv(0x81, [(byte)Algorithm]);
        return tlv.AsSpan().ToArray();
    }
}

/// <summary>
///     Iterated and Salted String-to-Key derivation (RFC 9580 §3.7.1.3), as the OpenPGP card's KDF-DO
///     applies it to PINs.
/// </summary>
/// <remarks>
///     <para>
///         <c>IterationCount</c> is the total number of bytes fed to one hash computation, not a number
///         of rounds; the card stores it as an unsigned 32-bit value. The salt followed by the PIN is
///         repeated until that many bytes have been hashed, then the hash is finalised once. When the
///         count is smaller than salt plus PIN, one full copy is hashed anyway, as required by
///         RFC 9580 §3.7.1.3. That citation defines the derivation primitive; Card specification 3.4.1
///         §4.3.2 defines F9 fields and assigns credential/configuration alignment to terminal software.
///     </para>
///     <para>
///         The iterated-salted S2K calculation is:
///         <code>
///         data = salt + pin_bytes
///         total = max(iteration_count, len(data))
///         (data_count, trailing) = divmod(total, len(data))
///         feed data data_count times into hash
///         feed data[:trailing] into hash
///         return hash.finalize()
///         </code>
///     </para>
/// </remarks>
public sealed class KdfIterSaltedS2k : Kdf
{
    internal const int KdfAlgorithm = 3;

    // Backing fields — KdfIterSaltedS2k always owns these arrays.
    // Init setters clone input so this instance can clear its own storage on disposal.
    private byte[] _saltUser = [];
    private byte[]? _saltReset;
    private byte[]? _saltAdmin;
    private byte[]? _initialHashUser;
    private byte[]? _initialHashAdmin;

    /// <inheritdoc />
    public override int Algorithm => KdfAlgorithm;

    /// <summary>
    ///     The hash algorithm used for key derivation.
    /// </summary>
    public KdfHashAlgorithm HashAlgorithm { get; init; }

    /// <summary>
    ///     The expanded input-byte count, 0 to <see cref="uint.MaxValue" />; derivation hashes at least
    ///     one full salt-plus-PIN copy. F9 encodes this as four bytes, unlike RFC packet S2K's coded octet.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value does not fit the card's unsigned 32-bit field.</exception>
    public long IterationCount
    {
        get;
        init => field = value is >= 0 and <= uint.MaxValue
            ? value
            : throw new ArgumentOutOfRangeException(nameof(IterationCount), value, "The KDF iteration count is an unsigned 32-bit value.");
    }

    /// <summary>
    ///     8-byte salt for the User PIN.
    ///     <para>The setter copies the provided memory — this instance owns the backing array.</para>
    /// </summary>
    public ReadOnlyMemory<byte> SaltUser
    {
        get => _saltUser;
        init => _saltUser = value.ToArray();
    }

    /// <summary>
    ///     8-byte salt for the Reset Code. Null if not configured.
    ///     <para>The setter copies the provided memory — this instance owns the backing array.</para>
    /// </summary>
    public ReadOnlyMemory<byte>? SaltReset
    {
        // Explicit null guard: byte[]? null → implicit ReadOnlyMemory<byte> conversion
        // yields Empty (non-null Nullable), breaking ?? fallback in GetSalt.
        get => _saltReset is { } s ? (ReadOnlyMemory<byte>?)s : null;
        init => _saltReset = value?.ToArray();
    }

    /// <summary>
    ///     8-byte salt for the Admin PIN. Null if not configured.
    ///     <para>The setter copies the provided memory — this instance owns the backing array.</para>
    /// </summary>
    public ReadOnlyMemory<byte>? SaltAdmin
    {
        get => _saltAdmin is { } s ? (ReadOnlyMemory<byte>?)s : null;
        init => _saltAdmin = value?.ToArray();
    }

    /// <summary>
    ///     Initial User PIN hash used for provisioning. Null if absent.
    ///     <para>The setter copies the provided memory — this instance owns the backing array.</para>
    /// </summary>
    public ReadOnlyMemory<byte>? InitialHashUser
    {
        get => _initialHashUser is { } s ? (ReadOnlyMemory<byte>?)s : null;
        init => _initialHashUser = value?.ToArray();
    }

    /// <summary>
    ///     Initial Admin PIN hash used for provisioning. Null if absent.
    ///     <para>The setter copies the provided memory — this instance owns the backing array.</para>
    /// </summary>
    public ReadOnlyMemory<byte>? InitialHashAdmin
    {
        get => _initialHashAdmin is { } s ? (ReadOnlyMemory<byte>?)s : null;
        init => _initialHashAdmin = value?.ToArray();
    }

    /// <summary>
    ///     Gets the salt for the specified PIN type. Falls back to <see cref="SaltUser" />
    ///     if the specific salt is not available.
    /// </summary>
    public ReadOnlyMemory<byte> GetSalt(Pw pw) =>
        pw switch
        {
            Pw.User => SaltUser,
            Pw.Reset => SaltReset ?? SaltUser,
            Pw.Admin => SaltAdmin ?? SaltUser,
            _ => SaltUser,
        };

    /// <inheritdoc />
    public override byte[] Process(Pw pw, ReadOnlySpan<byte> pin)
    {
        var salt = GetSalt(pw);

        // data = salt + pin
        var data = new byte[salt.Length + pin.Length];
        salt.Span.CopyTo(data);
        pin.CopyTo(data.AsSpan(salt.Length));

        try
        {
            return DoProcess(HashAlgorithm, IterationCount, data);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <summary>
    ///     Performs the iterated-salted-S2K hash computation.
    /// </summary>
    internal static byte[] DoProcess(KdfHashAlgorithm hashAlgorithm, long iterationCount, ReadOnlySpan<byte> data)
    {
        // RFC 9580 §3.7.1.3: never less than one full salt + PIN.
        var (dataCount, trailing) = Math.DivRem(Math.Max(iterationCount, data.Length), data.Length);

        using var incrementalHash = hashAlgorithm switch
        {
            KdfHashAlgorithm.Sha256 => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
            KdfHashAlgorithm.Sha512 => IncrementalHash.CreateHash(HashAlgorithmName.SHA512),
            _ => throw new ArgumentOutOfRangeException(nameof(hashAlgorithm)),
        };

        for (long i = 0; i < dataCount; i++)
        {
            incrementalHash.AppendData(data);
        }

        if (trailing > 0)
        {
            incrementalHash.AppendData(data[..(int)trailing]);
        }

        return incrementalHash.GetHashAndReset();
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var tlvs = new List<Tlv>();
        try
        {
            tlvs.Add(new Tlv(0x81, [(byte)Algorithm]));
            tlvs.Add(new Tlv(0x82, [(byte)HashAlgorithm]));

            Span<byte> iterBytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(iterBytes, (uint)IterationCount);
            tlvs.Add(new Tlv(0x83, iterBytes));

            tlvs.Add(new Tlv(0x84, SaltUser.Span));

            if (SaltReset is { } saltReset)
            {
                tlvs.Add(new Tlv(0x85, saltReset.Span));
            }

            if (SaltAdmin is { } saltAdmin)
            {
                tlvs.Add(new Tlv(0x86, saltAdmin.Span));
            }

            if (InitialHashUser is { } hashUser)
            {
                tlvs.Add(new Tlv(0x87, hashUser.Span));
            }

            if (InitialHashAdmin is { } hashAdmin)
            {
                tlvs.Add(new Tlv(0x88, hashAdmin.Span));
            }

            // Individual TLVs are disposed below. EncodeList's complete encoding is copied again here;
            // that intermediate currently has no clearing scope, independently of the caller-owned result.
            return TlvHelper.EncodeList([.. tlvs]).ToArray();
        }
        finally
        {
            foreach (var tlv in tlvs)
            {
                tlv.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        // Directly zero the owned backing arrays — no MemoryMarshal needed.
        CryptographicOperations.ZeroMemory(_saltUser);
        if (_saltReset is not null) CryptographicOperations.ZeroMemory(_saltReset);
        if (_saltAdmin is not null) CryptographicOperations.ZeroMemory(_saltAdmin);
        if (_initialHashUser is not null) CryptographicOperations.ZeroMemory(_initialHashUser);
        if (_initialHashAdmin is not null) CryptographicOperations.ZeroMemory(_initialHashAdmin);
        GC.SuppressFinalize(this);
        base.Dispose();
    }

    internal static KdfIterSaltedS2k ParseData(IDictionary<int, ReadOnlyMemory<byte>> data) =>
        new()
        {
            HashAlgorithm = (KdfHashAlgorithm)data[0x82].Span[0],
            IterationCount = BinaryPrimitives.ReadUInt32BigEndian(data[0x83].Span),
            SaltUser = data[0x84],                                                // init setter clones
            SaltReset = data.TryGetValue(0x85, out var sr) ? sr : null,           // init setter clones
            SaltAdmin = data.TryGetValue(0x86, out var sa) ? sa : null,           // init setter clones
            InitialHashUser = data.TryGetValue(0x87, out var hu) ? hu : null,     // init setter clones
            InitialHashAdmin = data.TryGetValue(0x88, out var ha) ? ha : null,    // init setter clones
        };
}
