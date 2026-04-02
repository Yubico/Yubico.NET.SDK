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

using System.Buffers;
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Builder for constructing CTAP2 extension input maps.
/// </summary>
/// <remarks>
/// <para>
/// This builder creates the CBOR-encoded extensions map used in makeCredential
/// and getAssertion requests. Extensions are added using the fluent API and
/// encoded to CBOR when Build() is called.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var extensions = new ExtensionBuilder()
///     .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
///     .WithCredBlob(myBlobData)
///     .Build();
/// </code>
/// </para>
/// </remarks>
public sealed class ExtensionBuilder
{
    private CredProtectPolicy? _credProtect;
    private bool _credProtectEnforcePolicy;
    private ReadOnlyMemory<byte>? _credBlob;
    private LargeBlobInput? _largeBlob;
    private LargeBlobAssertionInput? _largeBlobAssertion;
    private HmacSecretInput? _hmacSecret;
    private bool _hmacSecretMc;
    private bool _minPinLength;
    private bool _prf;
    private PrfInput? _prfInput;
    
    /// <summary>
    /// Adds the credProtect extension with the specified policy.
    /// </summary>
    /// <param name="policy">The credential protection policy.</param>
    /// <param name="enforcePolicy">If true, credential creation fails if policy cannot be honored.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithCredProtect(
        CredProtectPolicy policy, 
        bool enforcePolicy = false)
    {
        _credProtect = policy;
        _credProtectEnforcePolicy = enforcePolicy;
        return this;
    }
    
    /// <summary>
    /// Adds the credBlob extension with data to store.
    /// </summary>
    /// <param name="blob">The blob data to store (max 32 bytes typically).</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithCredBlob(ReadOnlyMemory<byte> blob)
    {
        _credBlob = blob;
        return this;
    }
    
    /// <summary>
    /// Adds the largeBlob extension for makeCredential.
    /// </summary>
    /// <param name="support">The required support level.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithLargeBlob(LargeBlobSupport support = LargeBlobSupport.Preferred)
    {
        _largeBlob = new LargeBlobInput { Support = support };
        return this;
    }
    
    /// <summary>
    /// Adds the largeBlob extension for getAssertion with read.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithLargeBlobRead()
    {
        _largeBlobAssertion = new LargeBlobAssertionInput { Read = true };
        return this;
    }
    
    /// <summary>
    /// Adds the largeBlob extension for getAssertion with write.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithLargeBlobWrite(ReadOnlyMemory<byte> data)
    {
        _largeBlobAssertion = new LargeBlobAssertionInput { Write = data };
        return this;
    }
    
    /// <summary>
    /// Adds the hmac-secret extension for getAssertion.
    /// </summary>
    /// <param name="input">The hmac-secret input parameters.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecret(HmacSecretInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _hmacSecret = input;
        return this;
    }
    
    /// <summary>
    /// Adds the hmac-secret extension for getAssertion using protocol helpers.
    /// </summary>
    /// <param name="protocol">The PIN/UV auth protocol.</param>
    /// <param name="sharedSecret">The shared secret from key agreement.</param>
    /// <param name="keyAgreement">The platform's key agreement public key.</param>
    /// <param name="salt1">The first salt (32 bytes).</param>
    /// <param name="salt2">Optional second salt (32 bytes). Pass empty span for no second salt.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecret(
        IPinUvAuthProtocol protocol,
        ReadOnlySpan<byte> sharedSecret,
        IReadOnlyDictionary<int, object?> keyAgreement,
        ReadOnlySpan<byte> salt1,
        ReadOnlySpan<byte> salt2 = default)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(keyAgreement);
        
        if (salt1.Length != 32)
        {
            throw new ArgumentException("Salt1 must be exactly 32 bytes.", nameof(salt1));
        }
        
        if (!salt2.IsEmpty && salt2.Length != 32)
        {
            throw new ArgumentException("Salt2 must be exactly 32 bytes if provided.", nameof(salt2));
        }
        
        // Prepare salt data
        byte[] saltData;
        if (!salt2.IsEmpty)
        {
            saltData = new byte[64];
            salt1.CopyTo(saltData);
            salt2.CopyTo(saltData.AsSpan(32));
        }
        else
        {
            saltData = salt1.ToArray();
        }
        
        // Encrypt salts
        var saltEnc = protocol.Encrypt(sharedSecret, saltData);
        
        // Compute auth tag
        var saltAuth = protocol.Authenticate(sharedSecret, saltEnc);
        
        _hmacSecret = new HmacSecretInput
        {
            KeyAgreement = keyAgreement,
            SaltEnc = saltEnc,
            SaltAuth = saltAuth,
            PinUvAuthProtocol = protocol.Version
        };
        
        return this;
    }
    
    /// <summary>
    /// Adds the hmac-secret-mc extension for makeCredential.
    /// </summary>
    /// <remarks>
    /// This extension allows retrieving hmac-secret output during credential creation.
    /// Requires firmware 5.4+.
    /// </remarks>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithHmacSecretMakeCredential()
    {
        _hmacSecretMc = true;
        return this;
    }
    
    /// <summary>
    /// Adds the minPinLength extension.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithMinPinLength()
    {
        _minPinLength = true;
        return this;
    }
    
    /// <summary>
    /// Indicates PRF extension support request for makeCredential.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithPrf()
    {
        _prf = true;
        return this;
    }
    
    /// <summary>
    /// Adds PRF extension with evaluation inputs for getAssertion.
    /// </summary>
    /// <param name="input">The PRF input parameters.</param>
    /// <returns>This builder for chaining.</returns>
    public ExtensionBuilder WithPrf(PrfInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _prf = true;
        _prfInput = input;
        return this;
    }
    
    /// <summary>
    /// Builds the CBOR-encoded extensions map.
    /// </summary>
    /// <returns>The CBOR-encoded extensions map, or null if no extensions.</returns>
    public ReadOnlyMemory<byte>? Build()
    {
        if (!HasExtensions())
        {
            return null;
        }
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        Encode(writer);
        return writer.Encode();
    }
    
    /// <summary>
    /// Encodes the extensions map to a CBOR writer.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        
        var count = CountExtensions();
        writer.WriteStartMap(count);
        
        // Extensions must be sorted by key for canonical CBOR
        // Sort order: "credBlob" < "credProtect" < "hmac-secret" < "hmac-secret-mc" < "largeBlob" < "minPinLength" < "prf"
        
        if (_credBlob.HasValue)
        {
            writer.WriteTextString(ExtensionIdentifiers.CredBlob);
            writer.WriteByteString(_credBlob.Value.Span);
        }
        
        if (_credProtect.HasValue)
        {
            writer.WriteTextString(ExtensionIdentifiers.CredProtect);
            writer.WriteInt32((int)_credProtect.Value);
        }
        
        if (_hmacSecret != null)
        {
            writer.WriteTextString(ExtensionIdentifiers.HmacSecret);
            _hmacSecret.Encode(writer);
        }
        
        if (_hmacSecretMc)
        {
            writer.WriteTextString(ExtensionIdentifiers.HmacSecretMakeCredential);
            writer.WriteBoolean(true);
        }
        
        if (_largeBlob != null)
        {
            writer.WriteTextString(ExtensionIdentifiers.LargeBlob);
            _largeBlob.Encode(writer);
        }
        
        if (_largeBlobAssertion != null)
        {
            writer.WriteTextString(ExtensionIdentifiers.LargeBlob);
            _largeBlobAssertion.Encode(writer);
        }
        
        if (_minPinLength)
        {
            writer.WriteTextString(ExtensionIdentifiers.MinPinLength);
            writer.WriteBoolean(true);
        }
        
        if (_prf)
        {
            writer.WriteTextString(ExtensionIdentifiers.Prf);
            if (_prfInput != null)
            {
                EncodePrfInput(writer, _prfInput);
            }
            else
            {
                // For makeCredential, just indicate support request
                writer.WriteStartMap(0);
                writer.WriteEndMap();
            }
        }
        
        writer.WriteEndMap();
    }
    
    private static void EncodePrfInput(CborWriter writer, PrfInput input)
    {
        writer.WriteStartMap(1);
        writer.WriteTextString("eval");
        
        var evalCount = 1;
        if (input.Second.HasValue) evalCount++;
        
        writer.WriteStartMap(evalCount);
        
        if (input.First.HasValue)
        {
            writer.WriteTextString("first");
            writer.WriteByteString(input.First.Value.Span);
        }
        
        if (input.Second.HasValue)
        {
            writer.WriteTextString("second");
            writer.WriteByteString(input.Second.Value.Span);
        }
        
        writer.WriteEndMap();
        writer.WriteEndMap();
    }
    
    private bool HasExtensions()
    {
        return _credProtect.HasValue ||
               _credBlob.HasValue ||
               _largeBlob != null ||
               _largeBlobAssertion != null ||
               _hmacSecret != null ||
               _hmacSecretMc ||
               _minPinLength ||
               _prf;
    }
    
    private int CountExtensions()
    {
        var count = 0;
        if (_credProtect.HasValue) count++;
        if (_credBlob.HasValue) count++;
        if (_largeBlob != null || _largeBlobAssertion != null) count++;
        if (_hmacSecret != null) count++;
        if (_hmacSecretMc) count++;
        if (_minPinLength) count++;
        if (_prf) count++;
        return count;
    }
}
