// Copyright 2021 Yubico AB
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
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Converters;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for generating and
    // importing keys into the slots. It also contains code for importing
    // certificates associated with the private keys.
    public sealed partial class PivSession : IDisposable
    {
        private const int PivCertInfoTag = 0x71;
        private const int PivLrcTag = 0xFE;
        
        /// <summary>
        /// Indicates the certificate is not compressed
        /// </summary>
        private const byte UncompressedCert = 0;
        
        /// <summary>
        /// Indicates the certificate is compressed
        /// </summary>
        private const byte CompressedCert = 1;
        
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey, PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
        public PivPublicKey GenerateKeyPair(
            byte slotNumber,
            PivAlgorithm algorithm,
            PivPinPolicy pinPolicy = PivPinPolicy.Default,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
        {
            YubiKey.ThrowIfUnsupportedAlgorithm(algorithm);

            RefreshManagementKeyAuthentication();

            var command = new GenerateKeyPairCommand(slotNumber, algorithm, pinPolicy, touchPolicy);
            var response = Connection.SendCommand(command);

            return response.GetData();
        }
        
        /// <summary>
        /// Generate a new key pair in the given slot.
        /// </summary>
        /// <remarks>
        /// When you generate a key pair, you specify which slot will hold this
        /// new key. If there is a key in that slot already, this method will
        /// replace it. That old key will be gone and there will be nothing you
        /// can do to recover it. Hence, use this method with caution.
        /// <para>
        /// You also have the opportunity to specify the PIN and touch policies
        /// of the private key generated. These policies describe what will be
        /// required when using the key. For example, if the PIN policy is
        /// <c>Always</c>, then every time the key is used (to sign, decrypt, or
        /// perform key agreement), it will be necessary to verify the PIV PIN.
        /// With the touch policy, for instance, setting it to <c>Always</c> will
        /// require touch every time the key is used. This method has the
        /// policies as optional arguments. If you do not specify these
        /// arguments, the key pair will be generated with the policies set to
        /// <c>Default</c>. Currently for all YubiKeys, the default PIN
        /// policy is <c>Once</c>, and the default touch policy is <c>Never</c>.
        /// </para>
        /// <para>
        /// This method will return the public key partner to the private key
        /// generated in the slot. For YubiKeys before version 5.3, it is the
        /// only time you will have the opportunity to obtain the public key, so
        /// make sure your application manages it right from the start. Beginning
        /// with version 5.3, it is possible to get a public key out of a slot at
        /// any time.
        /// </para>
        /// <para>
        /// Note that while this method will return the public key, you will
        /// still need to obtain a certificate for the private key outside of
        /// this SDK. Once you have the certificate, you can load it into the
        /// YubiKey using the <see cref="ImportCertificate"/> method.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated during this session. If it has not been authenticated,
        /// this method will call <see cref="AuthenticateManagementKey"/>. That
        /// is, your application does not need to authenticate the management key
        /// separately (i.e., call <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c>), this method will determine if the
        /// management key has been authenticated or not, and if not, it will
        /// make the call to perform mutual authentication.
        /// </para>
        /// <para>
        /// The authentication method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, it
        /// will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>AuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication method noting
        /// the cancellation. In that case, it will throw an exception. If you
        /// want the authentication to return <c>false</c> on user cancellation,
        /// you must call <see cref="TryAuthenticateManagementKey(bool)"/> directly
        /// before calling this method.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot into which the key pair will be generated.
        /// </param>
        /// <param name="keyType">
        /// The type of the key to generate.
        /// </param>
        /// <param name="pinPolicy">
        /// The PIN policy the key will have. If no argument is given, the policy
        /// will be <c>Default</c>.
        /// </param>
        /// <param name="touchPolicy">
        /// The touch policy the key will have. If no argument is given, the policy
        /// will be <c>Default</c>.
        /// </param>
        /// <returns>
        /// The public key partner to the private key generated on the YubiKey.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The slot or algorithm specified is not valid for generating a key
        /// pair.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the specified <see cref="PivAlgorithm"/> is not supported by the provided <see cref="IYubiKeyDevice"/>.
        /// </exception>
        public IPublicKey GenerateKeyPair(
            byte slotNumber,
            KeyType keyType,
            PivPinPolicy pinPolicy = PivPinPolicy.Default,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
        {
            Logger.LogInformation("Generating key pair.");
            
            YubiKey.ThrowIfUnsupportedAlgorithm(keyType.GetPivAlgorithm());

            RefreshManagementKeyAuthentication();

            var command = new GenerateKeyPairCommand(slotNumber, keyType, pinPolicy, touchPolicy);
            var response = Connection.SendCommand(command);
            
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException("Error generating key pair: " + response);
            }

            return PivKeyDecoder.CreatePublicKey(response.Data, keyType);
        }

        /// <summary>
        /// Import a private key into the given slot.
        /// </summary>
        /// <remarks>
        /// When you import a key, you specify which slot will hold this key. If
        /// there is a key in that slot already, this method will replace it.
        /// That old key will be gone and there will be nothing you can do to
        /// recover it. Hence, use this method with caution.
        /// <para>
        /// This method will not return to you the public key partner to the
        /// private key imported into the slot. For YubiKeys before version 5.3,
        /// you will not have the opportunity to obtain the public key, so make
        /// sure your application manages it right from the start. Beginning with
        /// version 5.3, it is possible to get a public key out of a slot at any
        /// time.
        /// </para>
        /// <para>
        /// You also have the opportunity to specify the PIN and touch policies
        /// of the private key generated. These policies describe what will be
        /// required when using the key. For example, if the PIN policy is
        /// <c>Always</c>, then every time the key is used (to sign, decrypt, or
        /// perform key agreement), it will be necessary to verify the PIV PIN.
        /// With the touch policy, for instance, setting it to <c>Always</c> will
        /// require touch every time the key is used. This method has the
        /// policies as optional arguments. If you do not specify these
        /// arguments, the key pair will be generated with the policies set to
        /// <c>Default</c>. Currently for all YubiKeys, the default PIN
        /// policy is <c>Once</c>, and the default touch policy is <c>Never</c>.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated during this session. If it has not been authenticated,
        /// this method will call <see cref="AuthenticateManagementKey"/>. That
        /// is, your application does not need to authenticate the management key
        /// separately (i.e., call <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c>), this method will determine if the
        /// management key has been authenticated or not, and if not, it will
        /// make the call to perform mutual authentication.
        /// </para>
        /// <para>
        /// The authentication method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, it
        /// will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>AuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication method noting
        /// the cancellation. In that case, it will throw an exception. If you
        /// want the authentication to return <c>false</c> on user cancellation,
        /// you must call <see cref="TryAuthenticateManagementKey(bool)"/> directly
        /// before calling this method.
        /// </para>
        /// </remarks>
        /// <param name="privateKey">
        /// The private key to import into the YubiKey.
        /// </param>
        /// <param name="slotNumber">
        /// The slot into which the key will be imported.
        /// </param>
        /// <param name="pinPolicy">
        /// The PIN policy the key will have. If no argument is given, the policy
        /// will be <c>Default</c>.
        /// </param>
        /// <param name="touchPolicy">
        /// The touch policy the key will have. If no argument is given, the policy
        /// will be <c>Default</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>privateKey</c> argument is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for generating a key pair, or the
        /// <c>privateKey</c> object is empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the specified <see cref="PivAlgorithm"/> is not supported by the provided <see cref="IYubiKeyDevice"/>.
        /// </exception>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey, PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
        public void ImportPrivateKey(
            byte slotNumber,
            PivPrivateKey privateKey,
            PivPinPolicy pinPolicy = PivPinPolicy.Default,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
        {
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            YubiKey.ThrowIfUnsupportedAlgorithm(privateKey.Algorithm);

            if (ManagementKeyAuthenticated == false)
            {
                AuthenticateManagementKey();
            }

            var command = new ImportAsymmetricKeyCommand(privateKey, slotNumber, pinPolicy, touchPolicy);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }
        
        /// <summary>
        /// Import a private key into the given slot.
        /// </summary>
        /// <remarks>
        /// When you import a key, you specify which slot will hold this key. If
        /// there is a key in that slot already, this method will replace it.
        /// That old key will be gone and there will be nothing you can do to
        /// recover it. Hence, use this method with caution.
        /// <para>
        /// This method will not return to you the public key partner to the
        /// private key imported into the slot. For YubiKeys before version 5.3,
        /// you will not have the opportunity to obtain the public key, so make
        /// sure your application manages it right from the start. Beginning with
        /// version 5.3, it is possible to get a public key out of a slot at any
        /// time.
        /// </para>
        /// <para>
        /// You also have the opportunity to specify the PIN and touch policies
        /// of the private key generated. These policies describe what will be
        /// required when using the key. For example, if the PIN policy is
        /// <c>Always</c>, then every time the key is used (to sign, decrypt, or
        /// perform key agreement), it will be necessary to verify the PIV PIN.
        /// With the touch policy, for instance, setting it to <c>Always</c> will
        /// require touch every time the key is used. This method has the
        /// policies as optional arguments. If you do not specify these
        /// arguments, the key pair will be generated with the policies set to
        /// <c>Default</c>. Currently for all YubiKeys, the default PIN
        /// policy is <c>Once</c>, and the default touch policy is <c>Never</c>.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated during this session. If it has not been authenticated,
        /// this method will call <see cref="AuthenticateManagementKey"/>. That
        /// is, your application does not need to authenticate the management key
        /// separately (i.e., call <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c>), this method will determine if the
        /// management key has been authenticated or not, and if not, it will
        /// make the call to perform mutual authentication.
        /// </para>
        /// <para>
        /// The authentication method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, it
        /// will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>AuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication method noting
        /// the cancellation. In that case, it will throw an exception. If you
        /// want the authentication to return <c>false</c> on user cancellation,
        /// you must call <see cref="TryAuthenticateManagementKey(bool)"/> directly
        /// before calling this method.
        /// </para>
        /// </remarks>
        /// <param name="privateKey">
        /// The private key parameters and values to import into the YubiKey.
        /// </param>
        /// <param name="slotNumber">
        /// The slot into which the key will be imported.
        /// </param>
        /// <param name="pinPolicy">
        /// The PIN policy the key will have. If no argument is given, the policy
        /// will be <c>Default</c>.
        /// </param>
        /// <param name="touchPolicy">
        /// The touch policy the key will have. If no argument is given, the policy
        /// will be <c>Default</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>privateKey</c> argument is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for generating a key pair, or the
        /// <c>privateKey</c> object is empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the specified <see cref="PivAlgorithm"/> is not supported by the provided <see cref="IYubiKeyDevice"/>.
        /// </exception>
        public void ImportPrivateKey(
            byte slotNumber,
            IPrivateKey privateKey,
            PivPinPolicy pinPolicy = PivPinPolicy.Default,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
        {
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            YubiKey.ThrowIfUnsupportedAlgorithm(privateKey.KeyType.GetPivAlgorithm());

            RefreshManagementKeyAuthentication();

            using var pivEncodedKeyHandle = new ZeroingMemoryHandle(privateKey.EncodeAsPiv());
            var command = new ImportAsymmetricKeyCommand(
                pivEncodedKeyHandle.Data, 
                privateKey.KeyType,
                slotNumber, 
                pinPolicy, 
                touchPolicy);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <summary>
        /// Import a certificate into the given slot.
        /// </summary>
        /// <remarks>
        /// When you import a certificate, you specify which slot will hold this
        /// cert. If there is a cert in that slot already, this method will
        /// replace it.
        /// <para>
        /// The PIV standard specifies that the maximum length of a cert is 1,856
        /// bytes. The YubiKey allows for certs up to 3,052 bytes. However, if
        /// you want your application to be PIV-compliant, then use certs no
        /// longer than 1,856 bytes.
        /// </para>
        /// <para>
        /// This method will not verify that the cert matches the private key in
        /// the slot. It will simply store the cert given in the slot specified.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated during this session. If it has not been authenticated,
        /// this method will call <see cref="AuthenticateManagementKey"/>. That
        /// is, your application does not need to authenticate the management key
        /// separately (i.e., call <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c>), this method will determine if the
        /// management key has been authenticated or not, and if not, it will
        /// make the call to perform mutual authentication.
        /// </para>
        /// <para>
        /// The authentication method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, it
        /// will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>AuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication method noting
        /// the cancellation. In that case, it will throw an exception. If you
        /// want the authentication to return <c>false</c> on user cancellation,
        /// you must call <see cref="TryAuthenticateManagementKey(bool)"/> directly
        /// before calling this method.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot into which the key will be imported.
        /// </param>
        /// <param name="certificate">
        /// The certificate to import into the YubiKey.
        /// </param>
        /// <param name="compress">
        /// If true the certificate will be compressed before being stored on the YubiKey.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>certificate</c> argument is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for importing a certificate.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void ImportCertificate(byte slotNumber, X509Certificate2 certificate, bool compress = false)
        {
            if (certificate is null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            RefreshManagementKeyAuthentication();

            byte certInfo = UncompressedCert; 
            byte[] certDer = certificate.GetRawCertData();
            if (compress)
            {
                certInfo = CompressedCert;
                try
                {
                    certDer = Compress(certDer);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.FailedCompressingCertificate));
                }
            }

            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(PivEncodingTag))
            {
                tlvWriter.WriteValue(PivCertTag, certDer);
                tlvWriter.WriteByte(PivCertInfoTag, certInfo);
                tlvWriter.WriteValue(PivLrcTag, null);
            }

            byte[] encodedCert = tlvWriter.Encode();

            var dataTag = GetCertDataTagFromSlotNumber(slotNumber);
            var command = new PutDataCommand((int)dataTag, encodedCert);
            var response = Connection.SendCommand(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <summary>
        /// Get the certificate in the given slot.
        /// </summary>
        /// <remarks>
        /// After obtaining a cert, you can install it into the slot containing
        /// the associated private key (<see cref="ImportCertificate"/>). Later
        /// on, when you need the cert (e.g. to send it along with a signed
        /// message), call this method to retrieve it.
        /// <para>
        /// It is not necessary to authenticate the management key nor verify the
        /// PIN in order to obtain a certificate.
        /// </para>
        /// <para>
        /// If the <c>slotNumber</c> given is for a slot that does not hold
        /// asymmetric keys, or if there is no cert in the slot, this method will
        /// throw an exception.
        /// </para>
        /// <para>
        /// If you want to get the attestation cert, do not use this method, call
        /// <see cref="GetAttestationCertificate"/>. If you call this method with
        /// slot number <c>0xF9</c> (<c>PivSlot.Attestation</c>) it will throw an
        /// exception.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot containing the requested cert.
        /// </param>
        /// <returns>
        /// The cert residing in the slot specified.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for getting a certificate.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The slot did not contain a cert, or the YubiKey had some other error,
        /// such as unreliable connection.
        /// </exception>
        public X509Certificate2 GetCertificate(byte slotNumber)
        {
            var dataTag = GetCertDataTagFromSlotNumber(slotNumber);

            var command = new GetDataCommand((int)dataTag);
            var response = Connection.SendCommand(command);
            var encodedCertData = response.GetData();

            var responseData = TlvObjects.UnpackValue(PivEncodingTag, encodedCertData.Span);
            var certData = TlvObjects.DecodeDictionary(responseData.Span);

            bool hasCertInfo = certData.TryGetValue(PivCertInfoTag, out var certInfo);
            if (!certData.TryGetValue(PivCertTag, out var certBytes))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.FailedParsingCertificate));
            }

            byte[] certBytesCopy = certBytes.ToArray();
            bool isCompressed = hasCertInfo && certInfo.Length > 0 && certInfo.Span[0] == CompressedCert;
            if (!isCompressed)
            {
                return new X509Certificate2(certBytesCopy);
            }

            try
            {
                return new X509Certificate2(Decompress(certBytesCopy));
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.FailedDecompressingCertificate));
            }
        }

        // There is a DataTag to use when calling PUT DATA. To put a cert onto
        // the YubiKey, use PUT DATA. Each slot has its own DataTag to use. This
        // will map the slot number to the appropriate DataTag.
        private static PivDataTag GetCertDataTagFromSlotNumber(byte slotNumber)
        {
            if (slotNumber >= PivSlot.Retired1 && slotNumber <= PivSlot.Retired20)
            {
                return PivDataTag.Retired1 + (slotNumber - PivSlot.Retired1);
            }

            return slotNumber switch
            {
                PivSlot.Authentication => PivDataTag.Authentication,
                PivSlot.Signing => PivDataTag.Signature,
                PivSlot.KeyManagement => PivDataTag.KeyManagement,
                PivSlot.CardAuthentication => PivDataTag.CardAuthentication,
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidSlot,
                        slotNumber)),
            };
        }

        private void RefreshManagementKeyAuthentication()
        {
            if (ManagementKeyAuthenticated)
            {
                return;
            }

            AuthenticateManagementKey();
        }

        static private byte[] Compress(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using (var compressor = new GZipStream(compressedStream, CompressionLevel.Optimal))
            {
                compressor.Write(data, 0, data.Length);
            }

            return compressedStream.ToArray();
        }

        static private byte[] Decompress(byte[] data)
        {
            using var dataStream = new MemoryStream(data);
            using var decompressor = new GZipStream(dataStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            decompressor.CopyTo(decompressedStream);
            
            return decompressedStream.ToArray();
        }
    }
}
