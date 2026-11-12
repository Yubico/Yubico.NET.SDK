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

using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography
{
    /// <summary>
    /// Represents the parameters for an Elliptic Curve (EC) private key.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the parameters specific to EC private keys
    /// and provides factory methods for creating instances from EC parameters
    /// or DER-encoded data.
    /// </remarks>
    public class ECPrivateKey : PrivateKey // TODO wrap an ECDH
    {
        /// <summary>
        /// Gets the Elliptic Curve parameters associated with this instance.
        /// </summary>
        /// <value>
        /// An <see cref="ECParameters"/> structure containing the curve parameters, key, and other
        /// cryptographic elements needed for EC operations. The array fields are owned by this
        /// object, must not be modified or cleared by the caller, and are cleared when the object is
        /// disposed.
        /// </value>
        public ECParameters Parameters { get; }

        /// <summary>
        /// Gets the metadata associated with this EC private key.
        /// </summary>
        /// <value>
        /// A <see cref="KeyInfo"/> object that describes the key's representation.
        /// </value>
        public override KeyInfo KeyInfo { get; }

        /// <inheritdoc />
        public override KeyType KeyType => KeyInfo.KeyType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ECPrivateKey"/> class.
        /// It is a wrapper for the <see cref="ECParameters"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create an instance from a <see cref="ECParameters"/> object. It will deep copy
        /// the parameters from the ECParameters object.
        /// </remarks>
        /// <param name="parameters">The EC parameters.</param>
        /// <param name="parametersCopied">An optional internal observation hook.</param>
        /// <exception cref="ArgumentException">Thrown when parameters do not contain D value.</exception>
        /// <exception cref="CryptographicException">
        ///     Thrown when <c>Q</c> is present and is not <c>d·G</c> on the curve.
        /// </exception>
        private ECPrivateKey(
            ECParameters parameters,
            Action<ECParameters>? parametersCopied = null)
        {
            if (parameters.D is null)
            {
                throw new ArgumentException("Parameters must contain private key data (D value)", nameof(parameters));
            }

            var curveInfo = CryptographicDefinitions.Curves.GetByOid(parameters.Curve.Oid.Value ?? string.Empty);
            KeyInfo = CryptographicDefinitions.Keys.GetNamedEcByCurve(curveInfo);
            AsnUtilities.ValidateEcPrivateScalarArgument(parameters.D,
                curveInfo.NamedCurveOid ?? throw new NotSupportedException("An ordinary EC key requires a named-curve OID."), nameof(parameters));
            PrivateKeyConsistency.ThrowIfPublicPointDoesNotMatch(parameters);
            Parameters = parameters.DeepCopy();
            try
            {
                parametersCopied?.Invoke(Parameters);
            }
            catch
            {
                CryptographicOperations.ZeroMemory(Parameters.D);
                throw;
            }
        }

        /// <inheritdoc/>
        public override byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();
            return AsnPrivateKeyEncoder.EncodeToPkcs8(Parameters);
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            CryptographicOperations.ZeroMemory(Parameters.Q.Y);
            CryptographicOperations.ZeroMemory(Parameters.Q.X);
            CryptographicOperations.ZeroMemory(Parameters.D);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ECPrivateKey"/> from a DER-encoded private key.
        /// </summary>
        /// <param name="encodedKey">
        /// The borrowed DER-encoded private key. This method copies the decoded key material and
        /// does not modify or clear the input.
        /// </param>
        /// <returns>
        /// A new disposable key that owns and clears its copied private-key material.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// Thrown if the private key is invalid.
        /// </exception>
        /// <exception cref="System.Formats.Asn1.AsnContentException">The ASN.1 structure or algorithm parameters are malformed.</exception>
        /// <remarks>
        /// Accepts the typed decoder's attribute-free, version-0 PKCS#8 subset. AlgorithmIdentifier
        /// consumption is checked separately from outer and nested trailing-data admission. Scalar-width
        /// checking does not prove range; consistency checking runs when both public coordinates are present.
        /// </remarks>
        public static ECPrivateKey CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
            => CreateFromPkcs8(encodedKey, parametersDecoded: null);

        // Test-only observers may retain aliases to inspect clearing after the owner exits; never mutate live arrays.
        internal static ECPrivateKey CreateFromPkcs8(
            ReadOnlyMemory<byte> encodedKey,
            Action<ECParameters>? parametersDecoded)
        {
            var parameters = AsnPrivateKeyDecoder.CreateECParameters(encodedKey);
            try
            {
                parametersDecoded?.Invoke(parameters);
                return CreateFromParameters(parameters);
            }
            finally
            {
                // On success the constructor copied D; on failure this factory is unwinding.
                // Either way, the decoder's temporary D is factory-owned. CreateFromParameters
                // is deliberately different because its input remains caller-owned.
                CryptographicOperations.ZeroMemory(parameters.D);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="ECPrivateKey"/> from the given <paramref name="parameters"/>.
        /// </summary>
        /// <param name="parameters">
        /// The borrowed parameters to copy. The caller retains ownership of every input array and
        /// remains responsible for clearing sensitive arrays.
        /// </param>
        /// <returns>A new disposable key that owns and clears its copied private-key material.</returns>
        /// <exception cref="CryptographicException">
        ///     <paramref name="parameters"/> carries a <c>Q</c> that is not <c>d·G</c> on the curve.
        /// </exception>
        public static ECPrivateKey CreateFromParameters(ECParameters parameters) => new(parameters);

        // Test-only observers may retain aliases to inspect clearing after the owner exits; never mutate live arrays.
        internal static ECPrivateKey CreateFromParameters(
            ECParameters parameters,
            Action<ECParameters>? parametersCopied) =>
            new(parameters, parametersCopied);

        /// <summary>
        /// Creates an instance of <see cref="ECPrivateKey"/> from an <see cref="ECDiffieHellman"/> instance.
        /// </summary>
        /// <param name="ecdh">
        /// The borrowed <see cref="ECDiffieHellman"/> instance containing the private key. This
        /// method does not dispose it.
        /// </param>
        /// <returns>A new disposable key that owns and clears its copied private-key material.</returns>
        /// <remarks>
        /// This method exports the private key parameters from the <see cref="ECDiffieHellman"/> instance
        /// and creates a new <see cref="ECPrivateKey"/> wrapper. This is useful when working with ephemeral
        /// keys generated via <see cref="ECDiffieHellman.Create(ECCurve)"/> for protocols like SCP11.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ecdh"/> is null.</exception>
        public static ECPrivateKey CreateFromEcdh(ECDiffieHellman ecdh)
            => CreateFromEcdh(ecdh, parametersExported: null);

        // Test-only observers may retain aliases to inspect clearing after the owner exits; never mutate live arrays.
        internal static ECPrivateKey CreateFromEcdh(
            ECDiffieHellman ecdh,
            Action<ECParameters>? parametersExported)
        {
            ArgumentNullException.ThrowIfNull(ecdh);
            var parameters = ecdh.ExportParameters(includePrivateParameters: true);
            try
            {
                parametersExported?.Invoke(parameters);
                return CreateFromParameters(parameters);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(parameters.D);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="ECPrivateKey"/> from the given
        /// <paramref name="privateValue"/> and <paramref name="keyType"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <paramref name="privateValue"/> is taken as the raw private key data (scalar value). The
        /// public point is derived from it by the .NET runtime, and the copies this SDK makes on the way
        /// are cleared.
        /// </para>
        /// <para>
        /// The inspected .NET v10.0.0 macOS D-only import route derives the public point and creates a
        /// runtime-owned managed scalar copy whose cleanup is not exposed to this SDK. Supplying both
        /// public coordinates through <see cref="CreateFromParameters(ECParameters)"/> skips that extraction
        /// route in the inspected revision. Provider-owned copies and native erasure still require separate
        /// qualification for the runtime actually used; clearing SDK arrays alone establishes neither.
        /// </para>
        /// </remarks>
        /// <param name="privateValue">
        /// The borrowed raw private value to copy. The caller retains ownership and remains
        /// responsible for clearing the input.
        /// </param>
        /// <param name="keyType">The type of key this is.</param>
        /// <returns>A new disposable key that owns and clears its copied private-key material.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the key type is not a valid EC key.
        /// </exception>
        public static ECPrivateKey CreateFromValue(
            ReadOnlyMemory<byte> privateValue,
            KeyType keyType)
            => CreateFromValue(privateValue, keyType, parametersCreated: null);

        // Test-only observers may retain aliases to inspect clearing after the owner exits; never mutate live arrays.
        internal static ECPrivateKey CreateFromValue(
            ReadOnlyMemory<byte> privateValue,
            KeyType keyType,
            Action<ECParameters>? parametersCreated)
        {
            var keyInfo = keyType.GetKeyInfo();
            if (!keyInfo.IsShortWeierstrass || keyInfo.Asn1?.NamedCurveOid is not { } curveOid)
            {
                throw new ArgumentException("An ordinary EC key requires a modeled short-Weierstrass curve.", nameof(keyType));
            }

            AsnUtilities.ValidateEcPrivateScalarArgument(privateValue.Span, curveOid, nameof(privateValue));
            var curve = ECCurve.CreateFromValue(curveOid);
            var parameters = new ECParameters
            {
                Curve = curve,
                D = privateValue.ToArray(),
            };

            try
            {
                parametersCreated?.Invoke(parameters);
                using var ecdsa = ECDsa.Create(parameters);
                var exportedParameters = ecdsa.ExportParameters(includePrivateParameters: true);
                try
                {
                    parametersCreated?.Invoke(exportedParameters);
                    return CreateFromParameters(exportedParameters);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(exportedParameters.D);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(parameters.D);
            }
        }

        /// <summary>
        /// Converts this EC private key to an <see cref="ECDiffieHellman"/> instance for use in key agreement operations.
        /// </summary>
        /// <returns>An <see cref="ECDiffieHellman"/> instance initialized with this key's parameters.</returns>
        /// <remarks>
        /// This method creates a new <see cref="ECDiffieHellman"/> instance from the stored EC parameters.
        /// The returned instance can be used for ECDH key agreement operations, such as those used in
        /// SCP11 (Secure Channel Protocol 11).
        /// <para>
        /// The caller is responsible for disposing the returned <see cref="ECDiffieHellman"/> instance
        /// to ensure proper cleanup of cryptographic resources.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public ECDiffieHellman ToECDiffieHellman()
        {
            ThrowIfDisposed();
            return ECDiffieHellman.Create(Parameters);
        }
    }
}