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

using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using CommunityToolkit.Diagnostics;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2;

/// <summary>
/// This abstract class represents a FIDO2 operation that can be performed by
/// the authenticator. It provides common functionality for managing
/// parameters associated with the operation,
/// such as <c>MakeCredential</c> and <c>GetAssertionParameters</c>.
/// <seealso cref="MakeCredentialParameters"/>
/// <seealso cref="GetAssertionParameters"/>
/// </summary>
public abstract class AuthenticatorOperationParameters<TOperationParameters> : ICborEncode 
    where TOperationParameters : AuthenticatorOperationParameters<TOperationParameters>
{
    private readonly Dictionary<string, byte[]> _extensions = [];
    private readonly Dictionary<string, bool> _options = [];
    
    /// <summary>
    /// The list of extensions. This is an optional parameter, so it can be
    /// null.
    /// </summary>
    /// <remarks>
    /// To add an entry to the list, call <see cref="AddExtension{T}"/>.
    /// <para>
    /// For each value, the standard (or the vendor in the case of
    /// vendor-defined extensions) will define the structure of the value.
    /// From that structure the value can be encoded following CBOR rules.
    /// The result of the encoding the value is what is stored in this
    /// dictionary.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, byte[]> Extensions => _extensions;

    /// <summary>
    /// The list of authenticator options. Each standard-defined option is a
    /// key/value pair, where the key is a string and the value is a boolean.
    /// </summary>
    /// <remarks>
    /// To add options, call <see cref="AddOption"/>.
    /// </remarks>
    public IReadOnlyDictionary<string, bool> Options => _options;

    /// <summary>
    /// Add an entry to the extensions list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each extension is a key/value pair. For each extension the key is a
    /// string (such as "credProtect" or "hmac-secret"). However, each value
    /// is different.
    /// </para>
    /// </remarks>
    /// <param name="extensionKey">
    /// The key of key/value to add.
    /// </param>
    /// <param name="value">
    /// The CBOR-encoded value of key/value to add.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <c>extensionKey</c> or <c>encodedValue</c> arg is null.
    /// </exception>
    public void AddExtension<TValue>(string extensionKey, TValue value)
    {
        Guard.IsNotNullOrWhiteSpace(extensionKey);
        Guard.IsNotNull(value);

        _extensions[extensionKey] = value switch
        {
            byte[] byteArray => byteArray, // Assume already encoded
            ReadOnlyMemory<byte> romValue => romValue.ToArray(), // Assume already encoded
            Memory<byte> memValue => memValue.ToArray(), // Assume already encoded
            bool boolValue => boolValue ? [CborHelpers.True] : [CborHelpers.False],
            int intValue => EncodeValue(cbor => cbor.WriteInt32(intValue)),
            byte byteValue => EncodeValue(cbor => cbor.WriteInt32(byteValue)),
            string stringValue => EncodeValue(cbor => cbor.WriteTextString(stringValue)),
            ICborEncode cborEncode => cborEncode.CborEncode(),
            _ => throw new ArgumentException(ExceptionMessages.Ctap2CborUnexpectedValue, nameof(value))
        };

        return;

        static byte[] EncodeValue(Action<CborWriter> writeAction)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            writeAction(cbor);
            return cbor.Encode();
        }
    }
    
    /// <summary>
    /// Add an entry to the list of options.
    /// </summary>
    /// <remarks>
    /// If the <c>Options</c> list already contains an entry with the given
    /// <c>optionKey</c>, this method will replace it.
    /// <para>
    /// Note that the standard specifies valid option keys. Currently they
    /// are "rk", "up", and "uv". This method will accept any key given and
    /// pass it to the YubiKey. If an invalid key is used, the YubiKey will
    /// return an error.
    /// </para>
    /// </remarks>
    /// <param name="optionKey">
    /// The option to add. This is the key of the option key/value pair.
    /// </param>
    /// <param name="optionValue">
    /// The value this option will possess.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <c>optionKey</c> arg is null.
    /// </exception>
    public void AddOption(string optionKey, bool optionValue) => _options[optionKey] = optionValue;

    /// <inheritdoc/>
    public abstract byte[] CborEncode();
}
