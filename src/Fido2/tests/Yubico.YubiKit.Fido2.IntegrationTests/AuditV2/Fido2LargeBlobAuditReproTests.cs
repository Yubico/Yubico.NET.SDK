using System.Formats.Cbor;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Buffers.Binary;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Fido2.LargeBlobs;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests.AuditV2;

public class Fido2LargeBlobAuditReproTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1621")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1621_SdkWriteIsReadablePerSpec(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            Skip.If(!info.Options.TryGetValue("largeBlobs", out bool supported) || !supported,
                "Device has no authenticatorLargeBlobs storage");
            using var clientPin = await FidoTestHelpers.SetOrVerifyPinAsync(session, FidoTestData.PinUtf8);
            try
            {
                byte[] challenge = FidoTestData.GenerateChallenge();
                byte[] makeToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                    FidoTestData.PinUtf8, PinUvAuthTokenPermissions.MakeCredential, FidoTestData.RpId);
                try
                {
                    var response = await session.MakeCredentialAsync(challenge, FidoTestData.CreateRelyingParty(),
                        FidoTestData.CreateUser(), FidoTestData.ES256Params,
                        new MakeCredentialOptions
                        {
                            ResidentKey = true,
                            PinUvAuthParam = FidoTestHelpers.ComputeMakeCredentialAuthParam(clientPin.Protocol, makeToken, challenge),
                            PinUvAuthProtocol = clientPin.Protocol.Version,
                            Extensions = new ExtensionBuilder().WithLargeBlobKey().Build()
                        });
                    Assert.NotNull(response.LargeBlobKey);
                    byte[] writeToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                        FidoTestData.PinUtf8, PinUvAuthTokenPermissions.LargeBlobWrite);
                    try
                    {
                        var storage = new LargeBlobStorage(session, clientPin.Protocol, writeToken,
                            (info.MaxMsgSize ?? 1024) - 64);
                        await storage.SetBlobAsync(response.LargeBlobKey.Value, "audit-interop"u8.ToArray());
                        // Read the raw serialized array, independently of SDK deserialization.
                        var request = new CborWriter();
                        request.WriteStartMap(2);
                        request.WriteInt32(1);
                        request.WriteInt32((info.MaxMsgSize ?? 1024) - 64);
                        request.WriteInt32(3);
                        request.WriteInt32(0);
                        request.WriteEndMap();
                        byte[] message = [0x0c, .. request.Encode()];
                        var rawResponse = await session.SendCborRequestAsync(message);
                        var reader = new CborReader(rawResponse);
                        Assert.Equal(1, reader.ReadStartMap());
                        Assert.Equal(1, reader.ReadInt32());
                        byte[] raw = reader.ReadByteString();
                        Assert.True(raw.Length < (info.MaxMsgSize ?? 1024) - 64,
                            "Test expects one fragment; use a fresh or otherwise small blob array");
                        Assert.Equal(SHA256.HashData(raw.AsSpan(0, raw.Length - 16)).AsSpan(0, 16).ToArray(), raw[^16..]);
                        var array = new CborReader(raw.AsMemory(0, raw.Length - 16), CborConformanceMode.Ctap2Canonical);
                        int count = array.ReadStartArray() ?? 0;
                        bool matched = false;
                        Span<byte> ad = stackalloc byte[12];
                        "blob"u8.CopyTo(ad);
                        for (int i = 0; i < count; i++)
                        {
                            Assert.Equal(3, array.ReadStartMap());
                            Assert.Equal(1, array.ReadInt32());
                            byte[] ct = array.ReadByteString();
                            Assert.Equal(2, array.ReadInt32());
                            byte[] nonce = array.ReadByteString();
                            Assert.Equal(3, array.ReadInt32());
                            int size = array.ReadInt32();
                            array.ReadEndMap();
                            BinaryPrimitives.WriteUInt64LittleEndian(ad[4..], (ulong)size);
                            byte[] compressed = new byte[ct.Length - 16];
                            try
                            {
                                using var aes = new AesGcm(response.LargeBlobKey.Value.Span, 16);
                                aes.Decrypt(nonce, ct.AsSpan(0, compressed.Length), ct.AsSpan(compressed.Length), compressed, ad);
                                using var inflate = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress);
                                using var output = new MemoryStream();
                                inflate.CopyTo(output);
                                matched = output.ToArray().AsSpan().SequenceEqual("audit-interop"u8);
                            }
                            catch (CryptographicException)
                            {
                                // Another credential's entry is expected not to decrypt with this key.
                            }
                        }
                        Assert.True(matched);
                    }
                    finally { CryptographicOperations.ZeroMemory(writeToken); }
                }
                finally { CryptographicOperations.ZeroMemory(makeToken); }
            }
            finally { await FidoTestHelpers.DeleteAllCredentialsForRpAsync(session, FidoTestData.RpId, FidoTestData.PinUtf8); }
        });

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1621")]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task YESDK1621_ReadsPythonFido2WrittenBlob(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            // Run C-interop.py first with a credential's largeBlobKey from a separate,
            // user-authorized getAssertion. Never print or persist that key in this test.
            string? keyHex = Environment.GetEnvironmentVariable("AUDIT_LARGE_BLOB_KEY_HEX");
            Skip.If(string.IsNullOrEmpty(keyHex), "Run C-interop.py and provide AUDIT_LARGE_BLOB_KEY_HEX");
            byte[] key = Convert.FromHexString(keyHex ?? "");
            try
            {
                var info = await session.GetInfoAsync();
                byte[]? blob = await new LargeBlobStorage(session, (info.MaxMsgSize ?? 1024) - 64)
                    .GetBlobAsync(key);
                Assert.Equal("python-fido2-audit-interop"u8.ToArray(), blob);
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        });
}
