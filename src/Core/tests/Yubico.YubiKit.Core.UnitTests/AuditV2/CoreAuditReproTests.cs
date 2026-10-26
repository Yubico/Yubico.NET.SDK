// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.AuditV2;

public class CoreAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void YESDK1635_RsaPkcs8RejectsModulusUnrelatedToPrivatePrimes()
    {
        using var a = RSA.Create(2048);
        using var b = RSA.Create(2048);
        RSAParameters privateA = a.ExportParameters(true);
        RSAParameters publicB = b.ExportParameters(false);
        byte[]? pkcs8 = null;
        try
        {
            var inner = new AsnWriter(AsnEncodingRules.DER);
            inner.PushSequence();
            inner.WriteInteger(0);
            inner.WriteInteger(new BigInteger(publicB.Modulus, isUnsigned: true, isBigEndian: true));
            foreach (byte[] component in new[] { privateA.Exponent!, privateA.D!, privateA.P!,
                         privateA.Q!, privateA.DP!, privateA.DQ!, privateA.InverseQ! })
            {
                inner.WriteInteger(new BigInteger(component, isUnsigned: true, isBigEndian: true));
            }

            inner.PopSequence();
            var outer = new AsnWriter(AsnEncodingRules.DER);
            outer.PushSequence();
            outer.WriteInteger(0);
            outer.PushSequence();
            outer.WriteObjectIdentifier("1.2.840.113549.1.1.1");
            outer.WriteNull();
            outer.PopSequence();
            byte[] innerEncoding = inner.Encode();
            try
            {
                outer.WriteOctetString(innerEncoding);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(innerEncoding);
            }

            outer.PopSequence();
            pkcs8 = outer.Encode();
            Assert.Throws<CryptographicException>(() =>
            {
                using var key = RSAPrivateKey.CreateFromPkcs8(pkcs8);
            });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pkcs8);
            CryptographicOperations.ZeroMemory(privateA.D);
            CryptographicOperations.ZeroMemory(privateA.P);
            CryptographicOperations.ZeroMemory(privateA.Q);
            CryptographicOperations.ZeroMemory(privateA.DP);
            CryptographicOperations.ZeroMemory(privateA.DQ);
            CryptographicOperations.ZeroMemory(privateA.InverseQ);
        }
    }

    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void YESDK1635_EcPkcs8RejectsPublicPointFromDifferentPrivateScalar()
    {
        using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters privateA = a.ExportParameters(true);
        ECParameters publicB = b.ExportParameters(false);
        byte[] pointB = [0x04, .. publicB.Q.X!, .. publicB.Q.Y!];
        try
        {
            var inner = new AsnWriter(AsnEncodingRules.DER);
            inner.PushSequence();
            inner.WriteInteger(1);
            inner.WriteOctetString(privateA.D);
            var publicTag = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true);
            inner.PushSequence(publicTag);
            inner.WriteBitString(pointB);
            inner.PopSequence(publicTag);
            inner.PopSequence();
            var outer = new AsnWriter(AsnEncodingRules.DER);
            outer.PushSequence();
            outer.WriteInteger(0);
            outer.PushSequence();
            outer.WriteObjectIdentifier("1.2.840.10045.2.1");
            outer.WriteObjectIdentifier("1.2.840.10045.3.1.7");
            outer.PopSequence();
            outer.WriteOctetString(inner.Encode());
            outer.PopSequence();
            byte[] pkcs8 = outer.Encode();
            try
            {
                Assert.Throws<CryptographicException>(() =>
                {
                    using var key = ECPrivateKey.CreateFromPkcs8(pkcs8);
                });
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs8);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateA.D);
        }
    }
}
