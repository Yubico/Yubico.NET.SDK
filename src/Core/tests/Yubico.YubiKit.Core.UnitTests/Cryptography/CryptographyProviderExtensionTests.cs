// Copyright (C) 2026 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

[Collection(CryptographyProvidersCollection.Name)]
public class CryptographyProviderExtensionTests
{
    [Fact]
    public void EcdhCreator_CustomPrimitive_IsInvoked()
    {
        Func<IEcdhPrimitives> original = CryptographyProviders.EcdhPrimitivesCreator;
        var primitive = new RecordingEcdhPrimitives();

        try
        {
            CryptographyProviders.EcdhPrimitivesCreator = () => primitive;
            using var hostKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var deviceKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            ECParameters deviceParameters = deviceKey.ExportParameters(false);
            var encodedPoint = new byte[65];
            encodedPoint[0] = 0x04;
            deviceParameters.Q.X.AsSpan().CopyTo(encodedPoint.AsSpan(1, 32));
            deviceParameters.Q.Y.AsSpan().CopyTo(encodedPoint.AsSpan(33, 32));
            using var encodedDeviceKey = new Tlv(0x5F49, encodedPoint);

            Span<byte> secret = Scp11X963Kdf.GetSharedSecret(
                hostKey,
                hostKey,
                deviceKey.PublicKey,
                encodedDeviceKey.AsMemory());

            Assert.Equal(2, primitive.ComputeSharedSecretCalls);
            CryptographicOperations.ZeroMemory(secret);
        }
        finally
        {
            CryptographyProviders.EcdhPrimitivesCreator = original;
        }
    }

    [Fact]
    public void GetSharedSecret_WhenProviderFails_DisposesEphemeralSdKeyOwner()
    {
        Func<IEcdhPrimitives> original = CryptographyProviders.EcdhPrimitivesCreator;
        ECDiffieHellman? ephemeralSdKeyOwner = null;

        try
        {
            CryptographyProviders.EcdhPrimitivesCreator = () => new ThrowingEcdhPrimitives();
            using var ephemeralOceKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var staticOceKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var staticSdKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellmanPublicKey staticSdPublicKey = staticSdKey.PublicKey;
            using var ephemeralSdKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var ephemeralSdPublicKey = new Tlv(0x5F49, ExportUncompressedPoint(ephemeralSdKey));

            Assert.Throws<CryptographicException>(() => Scp11X963Kdf.GetSharedSecret(
                ephemeralOceKey,
                staticOceKey,
                staticSdPublicKey,
                ephemeralSdPublicKey.AsMemory(),
                parameters => ephemeralSdKeyOwner = ECDiffieHellman.Create(parameters)));
        }
        finally
        {
            CryptographyProviders.EcdhPrimitivesCreator = original;
        }

        ECDiffieHellman observedOwner = Assert.IsAssignableFrom<ECDiffieHellman>(ephemeralSdKeyOwner);
        Assert.Throws<ObjectDisposedException>(() => observedOwner.ExportParameters(false));
    }

    [Fact]
    public void CmacCreator_CustomPrimitive_IsInvoked()
    {
        Func<CmacBlockCipherAlgorithm, ICmacPrimitives> original = CryptographyProviders.CmacPrimitivesCreator;
        var primitive = new RecordingCmacPrimitives();

        try
        {
            CryptographyProviders.CmacPrimitivesCreator = _ => primitive;
            Span<byte> output = stackalloc byte[16];
            StaticKeys.DeriveKey(new byte[16], 0x04, new byte[16], 128, output);

            Assert.True(primitive.InitializeCalled);
        }
        finally
        {
            CryptographyProviders.CmacPrimitivesCreator = original;
        }
    }

    [Fact]
    public void DefaultEcdhProvider_PassesP256KnownAnswerVector()
    {
        // D = 1 makes the shared secret trivially equal to the remote point (1 * Q = Q), while still
        // requiring the local key's own Q (the P-256 generator point G, since D=1) to correspond to D.
        byte[] generatorX = Convert.FromHexString(
            "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296");
        byte[] generatorY = Convert.FromHexString(
            "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");
        byte[] expectedSecret = Convert.FromHexString(
            "7CF27B188D034F7E8A52380304B51AC3C08969E277F21B35A60B48FC47669978");
        var localPrivateKey = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = generatorX, Y = generatorY },
            D = new byte[32]
        };
        localPrivateKey.D[^1] = 1;
        var remotePublicKey = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = expectedSecret,
                Y = Convert.FromHexString(
                    "07775510DB8ED040293D9AC69F7430DBBA7DADE63CE982299E04B79D227873D1")
            }
        };

        byte[] secret = EcdhPrimitives.Create().ComputeSharedSecret(localPrivateKey, remotePublicKey);

        Assert.Equal(expectedSecret, secret);
        CryptographicOperations.ZeroMemory(secret);
        CryptographicOperations.ZeroMemory(localPrivateKey.D);
    }

    [Fact]
    public void DefaultEcdhProvider_LocalAndRemoteAgreeOnSharedSecret()
    {
        // Cross-checks the primitive against real independently generated key pairs in both directions,
        // guarding against a local-key construction bug that happens to be masked by a trivial D=1 vector.
        using var aliceKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var bobKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        ECParameters aliceFull = aliceKey.ExportParameters(true);
        ECParameters bobFull = bobKey.ExportParameters(true);
        ECParameters alicePublic = aliceKey.ExportParameters(false);
        ECParameters bobPublic = bobKey.ExportParameters(false);

        try
        {
            byte[] fromAlice = EcdhPrimitives.Create().ComputeSharedSecret(aliceFull, bobPublic);
            byte[] fromBob = EcdhPrimitives.Create().ComputeSharedSecret(bobFull, alicePublic);
            using ECDiffieHellmanPublicKey bobDotNetPublicKey = bobKey.PublicKey;
            byte[] expected = aliceKey.DeriveRawSecretAgreement(bobDotNetPublicKey);

            Assert.Equal(expected, fromAlice);
            Assert.Equal(expected, fromBob);

            CryptographicOperations.ZeroMemory(fromAlice);
            CryptographicOperations.ZeroMemory(fromBob);
            CryptographicOperations.ZeroMemory(expected);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aliceFull.D!);
            CryptographicOperations.ZeroMemory(bobFull.D!);
        }
    }

    [Fact]
    public void DefaultCmacProvider_PassesRfc4493KnownAnswerVector()
    {
        byte[] key = Convert.FromHexString("2B7E151628AED2A6ABF7158809CF4F3C");
        byte[] expected = Convert.FromHexString("BB1D6929E95937287FA37D129B756746");
        Span<byte> mac = stackalloc byte[16];
        using ICmacPrimitives primitive = CmacPrimitives.Create(CmacBlockCipherAlgorithm.Aes128);

        primitive.CmacInit(key);
        primitive.CmacFinal(mac);

        Assert.True(mac.SequenceEqual(expected));
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(mac);
    }

    [Fact]
    public void DeriveSessionKeys_ReceiptMismatch_ZerosProviderSharedSecrets()
    {
        Func<IEcdhPrimitives> original = CryptographyProviders.EcdhPrimitivesCreator;
        var primitive = new RecordingEcdhPrimitives(0xA5);

        try
        {
            CryptographyProviders.EcdhPrimitivesCreator = () => primitive;
            using var hostKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var deviceKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            byte[] devicePoint = ExportUncompressedPoint(deviceKey);
            using var devicePointTlv = new Tlv(0x5F49, devicePoint);
            Memory<byte> hostAuthenticateData = TlvHelper.EncodeAndDisposeList(
                new Tlv(0xA6, TlvHelper.EncodeAndDisposeList(
                    new Tlv(0x95, [0x3C]),
                    new Tlv(0x80, [0x88]),
                    new Tlv(0x81, [0x10]))),
                new Tlv(0x5F49, ExportUncompressedPoint(hostKey)));

            Assert.Throws<BadResponseException>(() => Scp11X963Kdf.DeriveSessionKeys(
                hostKey,
                hostKey,
                hostAuthenticateData,
                deviceKey.PublicKey,
                devicePointTlv.AsMemory(),
                new byte[15]));

            Assert.Equal(2, primitive.SharedSecrets.Count);
            Assert.All(primitive.SharedSecrets, secret => Assert.All(secret, value => Assert.Equal(0, value)));
        }
        finally
        {
            CryptographyProviders.EcdhPrimitivesCreator = original;
        }
    }

    [Fact]
    public void CmacProviderFailure_DoesNotAdvanceScpMacChain()
    {
        Func<CmacBlockCipherAlgorithm, ICmacPrimitives> original = CryptographyProviders.CmacPrimitivesCreator;
        var primitive = new ThrowingCmacPrimitives();
        using var state = new ScpState(
            new SessionKeys(new byte[16], new byte[16], new byte[16]),
            new byte[16]);

        try
        {
            CryptographyProviders.CmacPrimitivesCreator = _ => primitive;

            Assert.Throws<NotSupportedException>(() => state.Mac([0x01, 0x02]));
            Assert.True(primitive.Disposed);
        }
        finally
        {
            CryptographyProviders.CmacPrimitivesCreator = original;
        }

        using var freshState = new ScpState(
            new SessionKeys(new byte[16], new byte[16], new byte[16]),
            new byte[16]);
        Assert.Equal(freshState.Mac([0x01, 0x02]), state.Mac([0x01, 0x02]));
    }

    [Fact]
    public void CmacProviderDisposeFailure_DoesNotAdvanceScpMacChain()
    {
        Func<CmacBlockCipherAlgorithm, ICmacPrimitives> original = CryptographyProviders.CmacPrimitivesCreator;
        using var state = new ScpState(
            new SessionKeys(new byte[16], new byte[16], new byte[16]),
            new byte[16]);

        try
        {
            CryptographyProviders.CmacPrimitivesCreator = _ => new ThrowingDisposeCmacPrimitives();

            Assert.Throws<NotSupportedException>(() => state.Mac([0x01, 0x02]));
        }
        finally
        {
            CryptographyProviders.CmacPrimitivesCreator = original;
        }

        using var freshState = new ScpState(
            new SessionKeys(new byte[16], new byte[16], new byte[16]),
            new byte[16]);
        Assert.Equal(freshState.Mac([0x01, 0x02]), state.Mac([0x01, 0x02]));
    }

    [Fact]
    public void StaticKeysDerive_WhenLaterProviderFails_ZerosAllWorkingBuffers()
    {
        Func<CmacBlockCipherAlgorithm, ICmacPrimitives> original = CryptographyProviders.CmacPrimitivesCreator;
        var finalCalls = 0;
        var senc = Enumerable.Repeat((byte)0xCC, 16).ToArray();
        var smac = Enumerable.Repeat((byte)0xCC, 16).ToArray();
        var srmac = Enumerable.Repeat((byte)0xCC, 16).ToArray();
        using var staticKeys = StaticKeys.GetDefaultKeys();

        try
        {
            CryptographyProviders.CmacPrimitivesCreator = _ => new FailingFinalCmacPrimitives(
                () => ++finalCalls);

            Assert.Throws<CryptographicException>(() =>
                staticKeys.Derive(new byte[16], senc, smac, srmac));
        }
        finally
        {
            CryptographyProviders.CmacPrimitivesCreator = original;
        }

        Assert.All(senc, value => Assert.Equal(0, value));
        Assert.All(smac, value => Assert.Equal(0, value));
        Assert.All(srmac, value => Assert.Equal(0, value));
    }

    [Fact]
    public void GenerateOceReceipt_WhenProviderDisposeFails_ZeroesAbandonedReceipt()
    {
        Func<CmacBlockCipherAlgorithm, ICmacPrimitives> original = CryptographyProviders.CmacPrimitivesCreator;
        byte[]? abandonedReceipt = null;

        try
        {
            CryptographyProviders.CmacPrimitivesCreator = _ => new ThrowingDisposeCmacPrimitives();

            Assert.Throws<CryptographicException>(() => Scp11X963Kdf.GenerateOceReceiptAesCmac(
                new byte[16],
                [0x01, 0x02],
                length => abandonedReceipt = Enumerable.Repeat((byte)0xCC, length).ToArray()));
        }
        finally
        {
            CryptographyProviders.CmacPrimitivesCreator = original;
        }

        byte[] observedReceipt = Assert.IsType<byte[]>(abandonedReceipt);
        Assert.All(observedReceipt, value => Assert.Equal(0, value));
    }

    private static byte[] ExportUncompressedPoint(ECDiffieHellman key)
    {
        ECParameters parameters = key.ExportParameters(false);
        var point = new byte[65];
        point[0] = 0x04;
        parameters.Q.X.AsSpan().CopyTo(point.AsSpan(1, 32));
        parameters.Q.Y.AsSpan().CopyTo(point.AsSpan(33, 32));
        return point;
    }

    private sealed class RecordingEcdhPrimitives(byte fill = 0) : IEcdhPrimitives
    {
        public int ComputeSharedSecretCalls { get; private set; }

        public List<byte[]> SharedSecrets { get; } = [];

        public ECParameters GenerateKeyPair(ECCurve curve) => default;

        public byte[] ComputeSharedSecret(ECParameters localPrivateKey, ECParameters remotePublicKey)
        {
            ComputeSharedSecretCalls++;
            var secret = new byte[32];
            secret.AsSpan().Fill(fill);
            SharedSecrets.Add(secret);
            return secret;
        }
    }

    private sealed class ThrowingEcdhPrimitives : IEcdhPrimitives
    {
        public ECParameters GenerateKeyPair(ECCurve curve) => throw new NotSupportedException();

        public byte[] ComputeSharedSecret(ECParameters localPrivateKey, ECParameters remotePublicKey) =>
            throw new CryptographicException("Injected ECDH provider failure.");
    }

    private sealed class RecordingCmacPrimitives : ICmacPrimitives
    {
        public bool InitializeCalled { get; private set; }

        public void CmacInit(ReadOnlySpan<byte> keyData) => InitializeCalled = true;

        public void CmacUpdate(ReadOnlySpan<byte> dataToMac) { }

        public void CmacFinal(Span<byte> macBuffer) { }

        public void Dispose() { }
    }

    private sealed class ThrowingCmacPrimitives : ICmacPrimitives
    {
        public bool Disposed { get; private set; }

        public void CmacInit(ReadOnlySpan<byte> keyData) { }

        public void CmacUpdate(ReadOnlySpan<byte> dataToMac) { }

        public void CmacFinal(Span<byte> macBuffer)
        {
            macBuffer.Fill(0xA5);
            throw new CryptographicException("Injected CMAC failure.");
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class ThrowingDisposeCmacPrimitives : ICmacPrimitives
    {
        public void CmacInit(ReadOnlySpan<byte> keyData) { }

        public void CmacUpdate(ReadOnlySpan<byte> dataToMac) { }

        public void CmacFinal(Span<byte> macBuffer) => macBuffer.Fill(0xA5);

        public void Dispose() => throw new CryptographicException("Injected CMAC disposal failure.");
    }

    private sealed class FailingFinalCmacPrimitives(Func<int> getFinalCall) : ICmacPrimitives
    {
        public void CmacInit(ReadOnlySpan<byte> keyData) { }

        public void CmacUpdate(ReadOnlySpan<byte> dataToMac) { }

        public void CmacFinal(Span<byte> macBuffer)
        {
            macBuffer.Fill(0xA5);
            if (getFinalCall() == 2)
            {
                throw new CryptographicException("Injected second derivation failure.");
            }
        }

        public void Dispose() { }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CryptographyProvidersCollection
{
    public const string Name = "Cryptography providers";
}