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
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class demonstrates how to perform some certificate operations,
    // using the .NET Base Class Library.
    // This sample demonstrates operations that are not part of PIV or the SDK,
    // although it uses the SDK.
    // It is only presented as a convenience to Yubico's customers.
    public static class SampleCertificateOperations
    {
        // Build a cert request and store it in the slotContents.
        public static void GetCertRequest(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            X500DistinguishedName distinguishedName,
            SamplePivSlotContents slotContents)
        {
            if (slotContents is null)
            {
                throw new ArgumentNullException(nameof(slotContents));
            }

            // Build the AsymmetricAlgorithm object from the public key.
            using var dotNetPubKey = KeyConverter.GetDotNetFromPivPublicKey(slotContents.PublicKey);

            // Build a cert request object.
            // This sample code uses SHA-256 for all algorithms except ECC P-384.
            // With RSA it always uses PSS.
            slotContents.CertRequest = slotContents.Algorithm switch
            {
                PivAlgorithm.EccP256 => new CertificateRequest(
                    distinguishedName,
                    (ECDsa)dotNetPubKey,
                    HashAlgorithmName.SHA256),
                PivAlgorithm.EccP384 => new CertificateRequest(
                    distinguishedName,
                    (ECDsa)dotNetPubKey,
                    HashAlgorithmName.SHA384),
                _ => new CertificateRequest(
                    distinguishedName,
                    (RSA)dotNetPubKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss),
            };

            // Set more info in the cert request if you want.

            // Now sign. In order to sign the cert request, there either needs to
            // be a private key in the AsymmetricAlgorithm object, or we need to
            // use an X509SignatureGenerator.
            // With a YubiKey, we can't get the private key out, so we'll have to
            // use the SignatureGenerator.
            // A sample YubiKeySignatureGenerator is available to demonstrate how
            // that works.
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                var signer = new YubiKeySignatureGenerator(pivSession, slotContents.SlotNumber, slotContents.PublicKey);
                byte[] requestDer = slotContents.CertRequest.CreateSigningRequest(signer);

                slotContents.SetCertRequestDer(requestDer);
            }
        }

        // This sample builds a self-signed cert using the public key partner to
        // the private key in the given slot.
        // This method will build a cert with the name "Fake Root". There is also
        // a sample that can build a CA cert and a leaf cert.
        public static void GetSelfSignedCert(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SamplePivSlotContents slotContents)
        {
            var nameBuilder = new X500NameBuilder();
            nameBuilder.AddNameElement(X500NameElement.Country, "US");
            nameBuilder.AddNameElement(X500NameElement.State, "CA");
            nameBuilder.AddNameElement(X500NameElement.Locality, "Palo Alto");
            nameBuilder.AddNameElement(X500NameElement.Organization, "Fake");
            nameBuilder.AddNameElement(X500NameElement.CommonName, "Fake Root");
            var sampleRootName = nameBuilder.GetDistinguishedName();

            GetCertRequest(yubiKey, KeyCollectorDelegate, sampleRootName, slotContents);

            // Add the BasicConstraints and KeyUsage extensions.
            var basicConstraints = new X509BasicConstraintsExtension(true, true, 2, true);
            var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true);
            slotContents.CertRequest.CertificateExtensions.Add(basicConstraints);
            slotContents.CertRequest.CertificateExtensions.Add(keyUsage);

            var notBefore = DateTimeOffset.Now;
            var notAfter = notBefore.AddDays(3650);
            byte[] serialNumber = new byte[] { 0x01 };

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                var signer = new YubiKeySignatureGenerator(pivSession, slotContents.SlotNumber, slotContents.PublicKey);
                var selfSignedCert = slotContents.CertRequest.Create(
                    sampleRootName,
                    signer,
                    notBefore,
                    notAfter,
                    serialNumber);

                // Store this certificate in the slot next to the key.
                pivSession.ImportCertificate(slotContents.SlotNumber, selfSignedCert);
            }
        }

        // This sample builds a cert request, then builds a cert from that
        // request using the signer key and cert.
        // The method will verify that the signer slot contains a cert. If it is
        // a root cert (a self-signed cert), it will build a request for a CA
        // cert (Name = Fake CA). It will also verify that it contains the
        // BasicConstraints extension (path len >= 2) and the KeyUsage extension
        // (KeyCertSign).
        // If the signer slot contains a CA cert (not self signed,
        // BasicConstraints >= 1), this method will build a leaf cert (Name =
        // Fake Leaf) with no BasicConstraints (the default is not a CA) and a
        // KeyUsage extension of DigitalSignature.
        public static bool GetSignedCert(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            SamplePivSlotContents requestorSlotContents,
            SamplePivSlotContents signerSlotContents)
        {
            X509Certificate2 signerCert;
            if (requestorSlotContents is null)
            {
                throw new ArgumentNullException(nameof(requestorSlotContents));
            }
            if (signerSlotContents is null)
            {
                throw new ArgumentNullException(nameof(signerSlotContents));
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                // Get the cert from the signer slot. Make sure the
                // BasicConstraints and KeyUsage extensions are valid.
                signerCert = pivSession.GetCertificate(signerSlotContents.SlotNumber);
            }

            var nameBuilder = new X500NameBuilder();
            nameBuilder.AddNameElement(X500NameElement.Country, "US");
            nameBuilder.AddNameElement(X500NameElement.State, "CA");
            nameBuilder.AddNameElement(X500NameElement.Locality, "Palo Alto");
            nameBuilder.AddNameElement(X500NameElement.Organization, "Fake");

            // Is the issuer cert a self-signed cert? If so, the cert we're now
            // creating will be a CA cert and we'll need to make sure the
            // pathLen is at least 2. If not, the pathLen only needs to be 1 and
            // we'll be building a leaf cert.
            int pathLength = 1;
            bool isCa = false;
            if (signerCert.SubjectName.RawData.SequenceEqual(signerCert.IssuerName.RawData))
            {
                pathLength = 2;
                nameBuilder.AddNameElement(X500NameElement.CommonName, "Fake CA");
                isCa = true;
            }
            else
            {
                nameBuilder.AddNameElement(X500NameElement.CommonName, "Fake Leaf");
            }

            // We can use this signerCert only if it contains BasicConstraints
            // and KeyUsage, and their values are acceptable.
            int index = 0;
            int count = 2;
            while (index < signerCert.Extensions.Count && count < 2)
            {
                if (signerCert.Extensions[index] is X509BasicConstraintsExtension basicConstraints)
                {
                    if (!basicConstraints.CertificateAuthority || basicConstraints.PathLengthConstraint < pathLength)
                    {
                        return false;
                    }
                    count++;
                }
                else if (signerCert.Extensions[index] is X509KeyUsageExtension keyUsage)
                {
                    if (keyUsage.KeyUsages != X509KeyUsageFlags.KeyCertSign)
                    {
                        return false;
                    }
                    count++;
                }
                index++;
            }

            if (count < 2)
            {
                return false;
            }

            // Get a signed cert request.
            var sampleCertName = nameBuilder.GetDistinguishedName();
            GetCertRequest(yubiKey, KeyCollectorDelegate, sampleCertName, requestorSlotContents);

            // In the real world, the cert request would be sent as a PEM
            // construction or something similar, not an object.
            // In addition, in order to build a cert using the BCL means starting
            // with a cert request object. But there's a twist. The cert request
            // object that is used to build the cert itself must be set with the
            // digest algorithm used by the CA.
            // For example, suppose the requestor builds a cert request for an
            // ECC P256 key. They sign it using SHA-256 and the ECC P256 private
            // key partner to the public key inside the request. They send the
            // DER encoding or PEM of the request to the CA.
            // Next, the CA will build a cert request object using the name and
            // public key from the provided signed request. However, suppose the
            // CA is going to sign the cert using SHA-384 and an ECC P384 key.
            // In this case, the CA must build this new cert request object
            // specifying SHA-384, even though the request itself was signed
            // using SHA-256.
            // Side note: It is actually possible to build a cert from a cert
            // request object and the issuer's cert (no need to set the request
            // object with the digest algorithm). However, that requires loading
            // the private key into the issuer's cert object. For this sample,
            // the private key is on the YubiKey. The BCL has the
            // SignatureGenerator class to allow using alternate private keys,
            // but that is not available in the certificate class. Hence, we
            // cannot use the BCL API that builds a cert from an issuer cert plus
            // cert request object.
            // What this all means is that this sample code will get the cert
            // request from the request object as the PEM encoding and verify it
            // (as a CA would). The code will now get the DER encoding, and build
            // a new request object, using the appropriate digest algorithm based
            // on the CA's key, and finally build a cert.
            char[] requestPem = requestorSlotContents.GetCertRequestPem();
            if (!IsValidCertRequestSignature(requestPem))
            {
                return false;
            }

            // This sample uses SHA-256 for all cases except when the key is ECC
            // P384. One way to know if a public key is P384 is to look at the
            // encoded key value. It will be
            //   04 <x-coordinate> <y-coordinate>
            // where each coordinate is exactly 48 bytes (384 bits) long.
            var signerHash = HashAlgorithmName.SHA256;
            if (string.Equals(signerCert.PublicKey.Oid.FriendlyName, "ECC", StringComparison.Ordinal)
                && signerCert.PublicKey.EncodedKeyValue.RawData.Length == 97)
            {
                signerHash = HashAlgorithmName.SHA384;
            }

            byte[] requestDer = requestorSlotContents.GetCertRequestDer();
            var certRequest = BuildCertRequestFromDer(requestDer, signerHash);

            if (isCa)
            {
                // Add the BasicConstraints and KeyUsage extensions.
                var basicConstraints = new X509BasicConstraintsExtension(true, true, 1, true);
                var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true);
                certRequest.CertificateExtensions.Add(basicConstraints);
                certRequest.CertificateExtensions.Add(keyUsage);
            }

            var notBefore = DateTimeOffset.Now;
            var notAfter = new DateTimeOffset(signerCert.NotAfter);
            byte[] serialNumber = new byte[] { 0x02 };

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                var signer = new YubiKeySignatureGenerator(
                    pivSession,
                    signerSlotContents.SlotNumber,
                    signerSlotContents.PublicKey);

                var newCert = certRequest.Create(
                    signerCert.SubjectName,
                    signer,
                    notBefore,
                    notAfter,
                    serialNumber);

                // Store this certificate in the slot next to the key.
                pivSession.ImportCertificate(requestorSlotContents.SlotNumber, newCert);
            }

            return true;
        }

        // It would make a lot of sense if the BCL had a way to build a cert
        // request object from DER (or PEM) and then we could get each
        // individual component out. Unfortunately, for some reason, the BCL
        // does not have such a method or constructor. So to build a request
        // object, we'll need to decode the request ourselves. It would be
        // great if the BCL had some ASN.1 code publicly available, but it
        // does not, so we'll have to use the SDK's TLV code.
        // In the real world, we would want to have some code that handles
        // many possibilities of data inside a request (e.g. which extensions
        // did the requestor include?). Unfortunately, that would be a lot of
        // code (i.e. because the BCL does not make it easy, we would have to
        // write so much more code to parse), so this sample will simply
        // extract the name and public key.
        // Another bit of information to extract from the cert would be the RSA
        // padding scheme, if the signature algorithm is RSA. However, to avoid
        // writing a lot more code, this sample always uses PSS.
        public static CertificateRequest BuildCertRequestFromDer(byte[] requestDer, HashAlgorithmName signerHash)
        {
            using var requestPublicKey = GetComponentsFromCertRequestDer(
                requestDer,
                out byte[] _,
                out var _,
                out var requestName,
                out byte[] _);

            if (requestPublicKey is ECDsa ecDsa)
            {
                return new CertificateRequest(requestName, ecDsa, signerHash);
            }

            return new CertificateRequest(requestName, (RSA)requestPublicKey, signerHash, RSASignaturePadding.Pss);
        }

        // Verify that the signature on the cert request is valid.
        // This will extract the public key from the cert request, and perform a
        // signature verification on the cert request.
        // This does not verify anything else about the cert request, only the
        // signature.
        // If the signature verifies, this method returns true, otherwise it
        // returns false.
        // If the input certRequestPem is not the PEM of a cert request, this
        // method throws an exception.
        public static bool IsValidCertRequestSignature(char[] certRequestPem)
        {
            byte[] request = PemOperations.GetEncodingFromPem(certRequestPem, "CERTIFICATE REQUEST");

            using var pubKey = GetComponentsFromCertRequestDer(
                request,
                out byte[] toBeSigned,
                out var sigAlgId,
                out var _,
                out byte[] signature);

            if (string.Equals(pubKey.SignatureAlgorithm, "RSA", StringComparison.Ordinal))
            {
                return ((RSA)pubKey).VerifyData(
                    toBeSigned,
                    signature,
                    sigAlgId.HashAlgorithm,
                    sigAlgId.Padding);
            }

            // It's not RSA, so we'll use ECC.
            // The YubiKey returns the signature in a format that virtually all
            // standards specify. However, that is not the format the C#
            // verification method needs.
            var algorithm = pubKey.KeySize switch
            {
                256 => PivAlgorithm.EccP256,
                384 => PivAlgorithm.EccP384,
                _ => PivAlgorithm.None,
            };

            byte[] nonStandardSignature = DsaSignatureConverter.GetNonStandardDsaFromStandard(signature, algorithm);

            return ((ECDsa)pubKey).VerifyData(
                toBeSigned,
                new ReadOnlySpan<byte>(nonStandardSignature),
                sigAlgId.HashAlgorithm);
        }

        // The arguments toBeVerified and verifier are slot numbers. Get the
        // certificates from these slots to perform the operation.
        // Get the cert from the slot toBeVerified, and verify that the signature
        // on that cert is valid.
        // The public key to use to verify the signature is from the cert in the
        // slot verifier.
        // This will extract the public key from the verifier cert, and perform a
        // signature verification on the toBeVerified cert.
        // This does not verify anything else about the cert, only the signature.
        // If the signature verifies, this method returns true, otherwise it
        // returns false.
        // If there is no cert in one or both of the slots, this method throws an
        // exception.
        public static bool IsValidCertificateSignature(
            IYubiKeyDevice yubiKey,
            Func<KeyEntryData, bool> KeyCollectorDelegate,
            byte toBeVerified,
            byte verifier)
        {
            X509Certificate2 certToVerify;
            X509Certificate2 issuerCert;

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = KeyCollectorDelegate;

                certToVerify = pivSession.GetCertificate(toBeVerified);
                issuerCert = pivSession.GetCertificate(verifier);
            }

            // Get the DER encoding of the cert to verify.
            byte[] certDer = certToVerify.RawData;

            // Now get the individual components of the cert.
            //   SEQ {
            //     ToBeSigned,
            //     signing algID,
            //     signature (BIT STRING)
            var tlvReader = new TlvReader(certDer);
            var seqReader = tlvReader.ReadNestedTlv(0x30);
            var toBeSigned = seqReader.ReadEncoded(0x30);
            var algId = seqReader.ReadEncoded(0x30);
            var signature = seqReader.ReadValue(0x03);

            var sigAlgId = new SignatureAlgIdConverter(algId.ToArray());

            // Get the public key of the verifying cert. We need it as an
            // AsymmetricAlgorithm object.
            using var pubKey = GetPublicKeyFromCertificate(issuerCert);

            // The signature is a BIT FIELD so the first octet is the unused
            // bits. That's why in the following we use a Slice of the signature
            // buffer.
            if (string.Equals(pubKey.SignatureAlgorithm, "RSA", StringComparison.Ordinal))
            {
                return ((RSA)pubKey).VerifyData(
                    toBeSigned.Span,
                    signature.Span[1..],
                    sigAlgId.HashAlgorithm,
                    sigAlgId.Padding);
            }

            // It's not RSA, so we'll use ECC.
            // The YubiKey returns the signature in a format that virtually all
            // standards specify. However, that is not the format the C#
            // verification method needs.
            var algorithm = pubKey.KeySize switch
            {
                256 => PivAlgorithm.EccP256,
                384 => PivAlgorithm.EccP384,
                _ => PivAlgorithm.None,
            };

            // The signature is a BIT FIELD so the first octet is the unused
            // bits. That's why in the following we use a Slice of the signature
            // buffer.
            byte[] nonStandardSignature = DsaSignatureConverter.GetNonStandardDsaFromStandard(
                signature[1..].ToArray(),
                algorithm);

            return ((ECDsa)pubKey).VerifyData(
                toBeSigned.Span,
                new ReadOnlySpan<byte>(nonStandardSignature),
                sigAlgId.HashAlgorithm);
        }

        // Get the public key out of a certificate. This method will return the
        // public key as an instance of AsymmetricAlgorithm.
        // The Certificate object will return the public key, but only as an
        // instance of PublicKey. If the algorithm is RSA, then it is possible to
        // get the AsymmetricAlgorithm version of the public key out of the
        // PublicKey object. But if the algorithm is ECC, it is not possible.
        // Hence, this method is provided. No matter the algorithm, it will
        // return a new instance of AsymmetricAlgorithm containing the public
        // key. Because it is a new instance, it would be a good idea to use the
        // using key word on the return object:
        //   using AsymmetricAlgorithm pubKey =
        //     SampleCertificateOperations.GetPublicKeyFromCertificate(cert);
        public static AsymmetricAlgorithm GetPublicKeyFromCertificate(X509Certificate2 certificate)
        {
            if (certificate is null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (string.Equals(certificate.PublicKey.Oid.FriendlyName, "ECC", StringComparison.Ordinal))
            {
                var pivPub = new PivEccPublicKey(certificate.PublicKey.EncodedKeyValue.RawData);
                return KeyConverter.GetDotNetFromPivPublicKey(pivPub);
            }

            var returnValue = RSA.Create();
            returnValue.ImportSubjectPublicKeyInfo(certificate.PublicKey.GetRSAPublicKey().ExportSubjectPublicKeyInfo(), out int _);

            return returnValue;
        }

        // Parse the DER encoding of the cert request, extracting the public key
        // (returned as an AsymmetricAlgorithm), the ToBeSigned data, the
        // signature algID (returned as a SignatureAlgIdConverter), the subject
        // name (the name the requestor is specifying to be the subject name of
        // the requested cert), and the signature.
        // The key is returned as the return value so the caller can use the
        // using key word easily.
        public static AsymmetricAlgorithm GetComponentsFromCertRequestDer(
            byte[] requestDer,
            out byte[] toBeSigned,
            out SignatureAlgIdConverter sigAlgId,
            out X500DistinguishedName requestName,
            out byte[] signature)
        {
            // Read the cert Request:
            //   SEQ {
            //     ToBeSigned,
            //     signing algID,
            //     signature (BIT STRING)
            var tlvReader = new TlvReader(requestDer);
            var seqReader = tlvReader.ReadNestedTlv(0x30);
            var toBeSignedMemory = seqReader.ReadEncoded(0x30);
            var algId = seqReader.ReadEncoded(0x30);
            var signatureMemory = seqReader.ReadValue(0x03);

            toBeSigned = toBeSignedMemory.ToArray();
            sigAlgId = new SignatureAlgIdConverter(algId.ToArray());

            // The signature is a BIT FIELD so the first octet is the unused
            // bits. That's why in the following we use a Slice of the signature
            // buffer.
            signature = signatureMemory.Span[1..].ToArray();

            // Extract the name and public key from the ToBeSigned
            //   SEQ {
            //     version INT,
            //     Name,
            //     SubjectPublicKeyInfo
            //      --ignore the rest-- }
            tlvReader = new TlvReader(toBeSigned);
            seqReader = tlvReader.ReadNestedTlv(0x30);
            _ = seqReader.ReadValue(0x02);
            var subjectName = seqReader.ReadEncoded(0x30);
            var subjectPublicKeyInfo = seqReader.ReadEncoded(0x30);

            // Build an X500DistinguishedName from the encoded name.
            requestName = new X500DistinguishedName(subjectName.ToArray());

            // Build a key from the SubjectPublicKeyInfo
            char[] pubKeyPem = PemOperations.BuildPem("PUBLIC KEY", subjectPublicKeyInfo.ToArray());
            return KeyConverter.GetDotNetFromPem(pubKeyPem, false);
        }
    }
}
