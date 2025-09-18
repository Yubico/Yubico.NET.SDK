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
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2;

/// <summary>
///     A FIDO2 <c>UserEntity</c>, consisting of ID, display name, and name. This
///     is used when the FIDO2 standard specifies a
///     <c>PublicKeyCredentialUserEntity</c>.
/// </summary>
/// <remarks>
///     A relying party (RP) will specify the user ID (which might or might not
///     be human-readable), which can be an account number. Either the platform
///     or the RP can specify a display name, the name of the account holder, and
///     a name, which is an account name (different accounts might have the same
///     display name). The display name and name are human-readable and can be
///     displayed to the user.
///     <para>
///         This class holds the RP ID, display name, and name, and can encode and
///         decode them as part of CBOR structures.
///     </para>
///     <para>
///         The FIDO2 standard specifies that when communicating with the
///         authenticator, the ID is not a required element, although it will likely
///         lead to interoperability issues if no value is given. This class will
///         require an ID.
///     </para>
///     <para>
///         The W3C standard declares the display name and name required elements,
///         but the FIDO2 standard declares them optional. Because the FIDO2 standard
///         specifically prescribes authenticator functionality, this class will
///         allow null display name and name.
///     </para>
/// </remarks>
public class UserEntity : ICborEncode
{
    private const string TagId = "id";
    private const string TagDisplayName = "displayName";
    private const string TagName = "name";

    /// <summary>
    ///     Constructs a new instance of <see cref="UserEntity" />.
    /// </summary>
    /// <param name="id">
    ///     The user's account ID. This constructor will copy a reference to the
    ///     input <c>id</c>.
    /// </param>
    public UserEntity(ReadOnlyMemory<byte> id)
    {
        Id = id;
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="UserEntity" /> from the
    ///     <c>encodedUserEntity</c>.
    /// </summary>
    /// <remarks>
    ///     This constructor expects the encoding to follow this CBOR template.
    ///     <code>
    ///    map {
    ///      "id"          --byte string--
    ///      "name"        --text string-- (optional)
    ///      "displayName" --text string-- (optional)
    ///    }
    /// </code>
    /// </remarks>
    /// <param name="encodedUserEntity">
    ///     The CBOR encoding of the user information.
    /// </param>
    /// <param name="bytesRead">
    ///     The constructor will return the number of bytes read.
    /// </param>
    /// <exception cref="Ctap2DataException">
    ///     The <c>encodedUserEntity</c> is not a correct encoding.
    /// </exception>
    public UserEntity(ReadOnlyMemory<byte> encodedUserEntity, out int bytesRead)
    {
        var cborMap = new CborMap<string>(encodedUserEntity);
        Id = cborMap.ReadByteString(TagId);
        Name = (string?)cborMap.ReadOptional<string>(TagName);
        DisplayName = (string?)cborMap.ReadOptional<string>(TagDisplayName);
        bytesRead = cborMap.BytesRead;
    }

    /// <summary>
    ///     The <c>id</c> component of the <c>UserEntity</c>.
    /// </summary>
    public ReadOnlyMemory<byte> Id { get; set; }

    /// <summary>
    ///     The <c>name</c> component of the <c>UserEntity</c>.
    /// </summary>
    /// <remarks>
    ///     The standard specifies that this element of a user entity is
    ///     optional. However, YubiKeys prior to version 5.3.0 require a
    ///     <c>Name</c> in order to make a credential.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    ///     The <c>displayName</c> component of the <c>UserEntity</c>.
    /// </summary>
    public string? DisplayName { get; set; }

    #region ICborEncode Members

    /// <inheritdoc />
    public byte[] CborEncode()
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, true);
        cbor.WriteStartMap(null);

        cbor.WriteTextString(TagId);
        cbor.WriteByteString(Id.Span);
        if (DisplayName is not null)
        {
            cbor.WriteTextString(TagDisplayName);
            cbor.WriteTextString(DisplayName);
        }

        if (Name is not null)
        {
            cbor.WriteTextString(TagName);
            cbor.WriteTextString(Name);
        }

        cbor.WriteEndMap();
        return cbor.Encode();
    }

    #endregion
}
