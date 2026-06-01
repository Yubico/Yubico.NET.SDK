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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents a WebAuthn attestation statement.
    /// </summary>
    public abstract class AttestationStatement
    {
        private protected AttestationStatement(string format, ReadOnlyMemory<byte> encoded)
        {
            Format = format;
            Encoded = encoded;
        }

        /// <summary>
        /// The attestation statement format identifier.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// The raw CBOR encoding of the attestation statement map.
        /// </summary>
        public ReadOnlyMemory<byte> Encoded { get; }

        internal static AttestationStatement FromCbor(
            string format,
            ReadOnlyMemory<byte> encodedAttestationStatement)
        {
            try
            {
                return format switch
                {
                    AttestationFormats.Packed => PackedAttestationStatement.FromCbor(encodedAttestationStatement),
                    AttestationFormats.FidoU2f => FidoU2fAttestationStatement.FromCbor(encodedAttestationStatement),
                    AttestationFormats.Apple => AppleAttestationStatement.FromCbor(encodedAttestationStatement),
                    AttestationFormats.None => NoneAttestationStatement.FromCbor(encodedAttestationStatement),
                    _ => new UnknownAttestationStatement(format, encodedAttestationStatement)
                };
            }
            catch (Exception exception) when (
                exception is CborContentException ||
                exception is Ctap2DataException ||
                exception is InvalidCastException ||
                exception is InvalidOperationException ||
                exception is KeyNotFoundException ||
                exception is FormatException ||
                exception is CryptographicException)
            {
                return new UnknownAttestationStatement(format, encodedAttestationStatement);
            }
        }

        private protected static IReadOnlyList<X509Certificate2>? ReadCertificates(
            CborMap<string> statementMap)
        {
            if (!statementMap.Contains("x5c"))
            {
                return null;
            }

            var certList = statementMap.ReadArray<byte[]>("x5c");
            var certificates = new List<X509Certificate2>(certList.Count);

            for (int index = 0; index < certList.Count; index++)
            {
                certificates.Add(new X509Certificate2(certList[index]));
            }

            return certificates;
        }

        private protected static bool ContainsOnlyKnownKeys(
            CborMap<string> statementMap,
            params string[] knownKeys)
        {
            foreach (string key in statementMap.Keys)
            {
                bool known = false;
                for (int index = 0; index < knownKeys.Length; index++)
                {
                    if (string.Equals(key, knownKeys[index], StringComparison.Ordinal))
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// A packed attestation statement.
    /// </summary>
    public sealed class PackedAttestationStatement : AttestationStatement
    {
        private const string AlgorithmKey = "alg";
        private const string SignatureKey = "sig";
        private const string CertificatesKey = "x5c";
        private const string EcdaaKeyIdKey = "ecdaaKeyId";

        private PackedAttestationStatement(
            ReadOnlyMemory<byte> encoded,
            CoseAlgorithmIdentifier algorithm,
            ReadOnlyMemory<byte> signature,
            IReadOnlyList<X509Certificate2>? certificates,
            ReadOnlyMemory<byte> ecdaaKeyId)
            : base(AttestationFormats.Packed, encoded)
        {
            Algorithm = algorithm;
            Signature = signature;
            Certificates = certificates;
            EcdaaKeyId = ecdaaKeyId;
        }

        /// <summary>
        /// The algorithm used to create the attestation signature.
        /// </summary>
        public CoseAlgorithmIdentifier Algorithm { get; }

        /// <summary>
        /// The attestation signature bytes from the <c>sig</c> field.
        /// </summary>
        public ReadOnlyMemory<byte> Signature { get; }

        /// <summary>
        /// The certificates from the attestation statement's <c>x5c</c> field.
        /// </summary>
        public IReadOnlyList<X509Certificate2>? Certificates { get; }

        /// <summary>
        /// The ECDAA key identifier from the attestation statement's
        /// <c>ecdaaKeyId</c> field, or empty when absent.
        /// </summary>
        public ReadOnlyMemory<byte> EcdaaKeyId { get; }

        internal static PackedAttestationStatement FromCbor(ReadOnlyMemory<byte> encoded)
        {
            var statementMap = new CborMap<string>(encoded);
            if (!statementMap.Contains(AlgorithmKey) ||
                !statementMap.Contains(SignatureKey) ||
                !ContainsOnlyKnownKeys(statementMap, AlgorithmKey, SignatureKey, CertificatesKey, EcdaaKeyIdKey))
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
            }

            var algorithm = (CoseAlgorithmIdentifier)statementMap.ReadInt32(AlgorithmKey);
            ReadOnlyMemory<byte> signature = statementMap.ReadByteString(SignatureKey);
            IReadOnlyList<X509Certificate2>? certificates = ReadCertificates(statementMap);
            ReadOnlyMemory<byte> ecdaaKeyId = statementMap.Contains(EcdaaKeyIdKey)
                ? statementMap.ReadByteString(EcdaaKeyIdKey)
                : ReadOnlyMemory<byte>.Empty;

            return new PackedAttestationStatement(
                encoded,
                algorithm,
                signature,
                certificates,
                ecdaaKeyId);
        }
    }

    /// <summary>
    /// A FIDO U2F attestation statement.
    /// </summary>
    public sealed class FidoU2fAttestationStatement : AttestationStatement
    {
        private const string SignatureKey = "sig";
        private const string CertificatesKey = "x5c";

        private FidoU2fAttestationStatement(
            ReadOnlyMemory<byte> encoded,
            ReadOnlyMemory<byte> signature,
            IReadOnlyList<X509Certificate2> certificates)
            : base(AttestationFormats.FidoU2f, encoded)
        {
            Signature = signature;
            Certificates = certificates;
        }

        /// <summary>
        /// The attestation signature bytes from the <c>sig</c> field.
        /// </summary>
        public ReadOnlyMemory<byte> Signature { get; }

        /// <summary>
        /// The certificates from the attestation statement's <c>x5c</c> field.
        /// </summary>
        public IReadOnlyList<X509Certificate2> Certificates { get; }

        internal static FidoU2fAttestationStatement FromCbor(ReadOnlyMemory<byte> encoded)
        {
            var statementMap = new CborMap<string>(encoded);
            if (!statementMap.Contains(SignatureKey) ||
                !statementMap.Contains(CertificatesKey) ||
                !ContainsOnlyKnownKeys(statementMap, SignatureKey, CertificatesKey))
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
            }

            return new FidoU2fAttestationStatement(
                encoded,
                statementMap.ReadByteString(SignatureKey),
                ReadCertificates(statementMap) ?? throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info));
        }
    }

    /// <summary>
    /// An Apple anonymous attestation statement.
    /// </summary>
    public sealed class AppleAttestationStatement : AttestationStatement
    {
        private const string CertificatesKey = "x5c";

        private AppleAttestationStatement(
            ReadOnlyMemory<byte> encoded,
            IReadOnlyList<X509Certificate2> certificates)
            : base(AttestationFormats.Apple, encoded)
        {
            Certificates = certificates;
        }

        /// <summary>
        /// The certificates from the attestation statement's <c>x5c</c> field.
        /// </summary>
        public IReadOnlyList<X509Certificate2> Certificates { get; }

        internal static AppleAttestationStatement FromCbor(ReadOnlyMemory<byte> encoded)
        {
            var statementMap = new CborMap<string>(encoded);
            if (!statementMap.Contains(CertificatesKey) ||
                !ContainsOnlyKnownKeys(statementMap, CertificatesKey))
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
            }

            return new AppleAttestationStatement(
                encoded,
                ReadCertificates(statementMap) ?? throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info));
        }
    }

    /// <summary>
    /// A none attestation statement.
    /// </summary>
    public sealed class NoneAttestationStatement : AttestationStatement
    {
        private NoneAttestationStatement(ReadOnlyMemory<byte> encoded)
            : base(AttestationFormats.None, encoded) { }

        internal static NoneAttestationStatement FromCbor(ReadOnlyMemory<byte> encoded)
        {
            var statementMap = new CborMap<string>(encoded);
            if (statementMap.Count != 0)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
            }

            return new NoneAttestationStatement(encoded);
        }
    }

    /// <summary>
    /// An attestation statement whose format or contents are not parsed by this SDK.
    /// </summary>
    public sealed class UnknownAttestationStatement : AttestationStatement
    {
        internal UnknownAttestationStatement(string format, ReadOnlyMemory<byte> encoded)
            : base(format, encoded) { }
    }
}
