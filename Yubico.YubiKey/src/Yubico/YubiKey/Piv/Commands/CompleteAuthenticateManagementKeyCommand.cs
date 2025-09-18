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
using System.Globalization;
using System.Security.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     Complete the process to authenticate the PIV management key.
/// </summary>
/// <remarks>
///     In the PIV standard, there is a command called GENERAL AUTHENTICATE.
///     Although it is one command, it can do four things: authenticate a
///     management key (challenge-response), sign arbitrary data, RSA decryption,
///     and EC Diffie-Hellman. The SDK breaks these four operations into separate
///     classes. This class is how you complete the process of performing
///     "GENERAL AUTHENTICATE: management key".
///     <para>
///         The partner Response class is <see cref="CompleteAuthenticateManagementKeyResponse" />.
///     </para>
///     <para>
///         See the comments for the class
///         <see cref="InitializeAuthenticateManagementKeyCommand" />, there is a
///         lengthy discussion of the process of authenticating the management key,
///         including descriptions of the challenges and responses.
///     </para>
///     <para>
///         When you pass a management key to this class (the management key to
///         authenticate), the class will copy it, use it immediately, and overwrite
///         the local buffer. The class will not keep a reference to your key data.
///         Because of this, you can overwrite the management key data immediately
///         upon return from the constructor if you want. See the User's Manual
///         <xref href="UsersManualSensitive"> entry on sensitive data</xref>
///         for more information on this topic.
///     </para>
///     <para>
///         This class will need a random number generator and either a triple-DES or
///         AES object. It will get them from the
///         <see cref="Yubico.YubiKey.Cryptography.CryptographyProviders" />
///         class. That class will build default implementations. It is possible to
///         change that class to build alternate versions. See the user's manual
///         entry on <xref href="UsersManualAlternateCrypto"> alternate crypto </xref>
///         for information on how to do so.
///     </para>
/// </remarks>
public sealed class CompleteAuthenticateManagementKeyCommand
    : IYubiKeyCommand<CompleteAuthenticateManagementKeyResponse>
{
    private const byte AuthMgmtKeyInstruction = 0x87;
    private const byte AuthMgmtKeyParameter2 = 0x9B;

    private const int Aes128KeyLength = 16;
    private const int Aes192KeyLength = 24;
    private const int Aes256KeyLength = 32;

    private const int CommandTag = 0x7C;
    private const int ClientResponseTagSingle = 0x82;
    private const int ClientResponseTag = 0x80;
    private const int YubiKeyChallengeTag = 0x81;
    private const int EmptyTag = 0x82;
    private const int BlockCount = 4;
    private const int ClientResponseOffset = 0;
    private const int YubiKeyChallengeOffset = 1;
    private const int ExpectedResponseOffset = 2;
    private const int ClientChallengeOffset = 3;
    private readonly int _blockSize;

    private readonly byte[] _buffer;
    private readonly Memory<byte> _dataMemory;
    private readonly ReadOnlyMemory<byte> _expectedResponse;

    private readonly bool _isMutual;

    // The default constructor explicitly defined. We don't want it to be
    // used.
    // Note that there is no object-initializer constructor. All the
    // constructor inputs have no default or are secret byte arrays.
    private CompleteAuthenticateManagementKeyCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Build a new instance of the
    ///     <c>CompleteAuthenticateManagementKeyCommand</c> class for the
    ///     algorithm specified in <c>initializeAuthenticationResponse</c>.
    /// </summary>
    /// <remarks>
    ///     The input Response Object is the successful Response from step 1. The
    ///     response has information on whether the process was initiated for
    ///     single or mutual authentication, along with the management key's
    ///     algorithm. The object created using this constructor will therefore
    ///     be able to perform the appropriate operations and build the
    ///     appropriate APDU based on how the process was initiated.
    ///     <para>
    ///         This class will use the random number generator and Triple-DES or AES
    ///         classes from <see cref="CryptographyProviders" />. If you want this
    ///         class to use classes other than the defaults, change them. See also
    ///         the user's manual entry on
    ///         <xref href="UsersManualAlternateCrypto"> alternate crypto </xref> for
    ///         information on how to do so.
    ///     </para>
    /// </remarks>
    /// <param name="initializeAuthenticationResponse">
    ///     The Response Object from Step 1.
    /// </param>
    /// <param name="managementKey">
    ///     The bytes of the management key.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The <c>initializeAuthenticationResponse</c> argument is null
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The <c>initializeAuthenticationResponse</c> argument does not
    ///     represent a complete response.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     The <c>managementKey</c> argument is not a valid key, or the
    ///     <c>algorithm</c> is not valid or does not match the data.
    /// </exception>
    /// <exception cref="CryptographicException">
    ///     The Triple-DES or AES operation failed.
    /// </exception>
    public CompleteAuthenticateManagementKeyCommand(
        InitializeAuthenticateManagementKeyResponse initializeAuthenticationResponse,
        ReadOnlySpan<byte> managementKey)
    {
        if (initializeAuthenticationResponse is null)
        {
            throw new ArgumentNullException(nameof(initializeAuthenticationResponse));
        }

        if (initializeAuthenticationResponse.Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidApduResponseData));
        }

        Algorithm = initializeAuthenticationResponse.Algorithm;

        (bool isMutual, var clientAuthenticationChallenge) = initializeAuthenticationResponse.GetData();
        _isMutual = isMutual;

        // With single auth, encrypt the challenge. Mutual decrypts.
        // Note that the constructors for the ISym objects take in an arg
        // "isEncrypting". If true, encrypt. We want to decrypt for mutual
        // auth, so when _isMutual is true, we want to pass false to the ISym
        // constructor. And vice versa.

        // JUSTIFICATION (disable 618): We are using the
        // *ForManagementKey classes in the way they were intended.
        #pragma warning disable 618
        using ISymmetricForManagementKey symObject = Algorithm switch
        {
            PivAlgorithm.TripleDes => new TripleDesForManagementKey(managementKey, !_isMutual),
            PivAlgorithm.Aes128 => new AesForManagementKey(managementKey, Aes128KeyLength, !_isMutual),
            PivAlgorithm.Aes192 => new AesForManagementKey(managementKey, Aes192KeyLength, !_isMutual),
            PivAlgorithm.Aes256 => new AesForManagementKey(managementKey, Aes256KeyLength, !_isMutual),
            _ => throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidAlgorithm))
        };
        #pragma warning restore 618

        _blockSize = symObject.BlockSize;
        _buffer = new byte[BlockCount * symObject.BlockSize];
        _dataMemory = new Memory<byte>(_buffer);

        int copyCount = clientAuthenticationChallenge.Length >= _blockSize
            ? _blockSize
            : clientAuthenticationChallenge.Length;

        clientAuthenticationChallenge.CopyTo(_dataMemory.Slice(ClientChallengeOffset * _blockSize, copyCount));

        int bytesWritten = 0;
        int expectedWritten = _blockSize;

        if (_isMutual)
        {
            // For mutual auth, we will decrypt the witness
            using var randomObject = CryptographyProviders.RngCreator();
            randomObject.GetBytes(_buffer, ExpectedResponseOffset * _blockSize, _blockSize);

            // The app will send the YubiKey a challenge in the clear. The
            // YubiKey will encrypt it. So we want to verify that what the
            // YubiKey returns is the encrypted challenge.
            // Instead of creating a new encryption object, just use decrypt.
            // Generate random data and call it the encrypted challenge, it is
            // the expected response. We know that encrypting the challenge
            // will produce the response, so decrypting the response will
            // produce the challenge.
            // Decrypt the YubiKey Authentication Expected Response
            // to get YubiKey Authentication Challenge.
            // The ISym API needs the key data in a byte array.

            bytesWritten += symObject.TransformBlock(
                _buffer,
                ExpectedResponseOffset * _blockSize,
                _blockSize,
                _buffer,
                YubiKeyChallengeOffset * _blockSize);

            expectedWritten += _blockSize;

            _expectedResponse = new ReadOnlyMemory<byte>(_buffer, ExpectedResponseOffset * _blockSize, _blockSize);
        }
        else
        {
            _expectedResponse = ReadOnlyMemory<byte>.Empty;
        }

        // (Mutual auth) Decrypt Client Authentication Challenge to generate
        // Client Authentication Response.
        // - or -
        // (Single auth) Encrypt Client Authentication Witness to generate
        // Client Authentication Response.
        bytesWritten += symObject.TransformBlock(
            _dataMemory.Slice(ClientChallengeOffset * _blockSize, _blockSize).ToArray(),
            0,
            _blockSize,
            _buffer,
            ClientResponseOffset * _blockSize);

        if (bytesWritten != expectedWritten)
        {
            throw new CryptographicException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.TripleDesFailed));
        }
    }

    /// <summary>
    ///     Which algorithm is the management key.
    /// </summary>
    public PivAlgorithm Algorithm { get; }

    #region IYubiKeyCommand<CompleteAuthenticateManagementKeyResponse> Members

    /// <summary>
    ///     Gets the YubiKeyApplication to which this command belongs. For this
    ///     command it's PIV.
    /// </summary>
    /// <value>
    ///     YubiKeyApplication.Piv
    /// </value>
    public YubiKeyApplication Application => YubiKeyApplication.Piv;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu()
    {
        var tlvWriter = new TlvWriter();
        using (tlvWriter.WriteNestedTlv(CommandTag))
        {
            if (_isMutual)
            {
                tlvWriter.WriteValue(
                    ClientResponseTag, _dataMemory.Slice(ClientResponseOffset * _blockSize, _blockSize).Span);

                tlvWriter.WriteValue(
                    YubiKeyChallengeTag, _dataMemory.Slice(YubiKeyChallengeOffset * _blockSize, _blockSize).Span);

                tlvWriter.WriteValue(EmptyTag, ReadOnlySpan<byte>.Empty);
            }
            else
            {
                tlvWriter.WriteValue(
                    ClientResponseTagSingle, _dataMemory.Slice(ClientResponseOffset * _blockSize, _blockSize).Span);
            }
        }

        byte[] encoding = tlvWriter.Encode();

        return new CommandApdu
        {
            Ins = AuthMgmtKeyInstruction,
            P1 = (byte)Algorithm,
            P2 = AuthMgmtKeyParameter2,
            Data = encoding
        };
    }

    /// <inheritdoc />
    public CompleteAuthenticateManagementKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
        new(responseApdu, _expectedResponse);

    #endregion
}
