using System.Security.Cryptography;

namespace Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests;

// Test-only, independent AES-block CMAC: RFC 4493 §§2.3–2.4 and all four §4 vectors.
// Vendor _derive and SymmetricAuth mappings are pinned under hardware-probe/sources/.
internal static class SymmetricOracle
{
    public static void SelfTest()
    {
        byte[] key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] message = Convert.FromHexString(
            "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e51" +
            "30c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710");
        int[] lengths = [0, 16, 40, 64];
        string[] vectors = ["bb1d6929e95937287fa37d129b756746", "070a16b46b4d4144f79bdd9dd04a287c",
            "dfa66747de9ae63030ca32611497c827", "51f0bebf7e3b9d92fc49741779363cfe"];
        Span<byte> actual = stackalloc byte[16];
        try
        {
            for (int i = 0; i < lengths.Length; i++)
            {
                byte[] expected = Convert.FromHexString(vectors[i]);
                try
                {
                    Cmac(key, message.AsSpan(0, lengths[i]), actual);
                    Assert.True(CryptographicOperations.FixedTimeEquals(expected, actual),
                        $"Independent CMAC failed RFC 4493 vector {i + 1}; stop before device binding.");
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(expected);
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actual);
            CryptographicOperations.ZeroMemory(message);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public static void Derive(ReadOnlySpan<byte> key, byte constant, ReadOnlySpan<byte> context, Span<byte> output)
    {
        Assert.Equal(16, key.Length);
        Assert.Equal(16, context.Length);
        Assert.True(output.Length is 8 or 16);
        Span<byte> input = stackalloc byte[32];
        Span<byte> mac = stackalloc byte[16];
        try
        {
            // Python _derive: zero[11] || type || 00 || L[2, big endian] || 01 || context.
            input.Clear();
            input[11] = constant;
            input[14] = checked((byte)(output.Length * 8));
            input[15] = 1;
            context.CopyTo(input[16..]);
            Cmac(key, input, mac);
            mac[..output.Length].CopyTo(output);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mac);
            CryptographicOperations.ZeroMemory(input);
        }
    }

    private static void Cmac(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, Span<byte> result)
    {
        using var aes = Aes.Create();
        aes.SetKey(key);
        Span<byte> zero = stackalloc byte[16];
        Span<byte> l = stackalloc byte[16];
        Span<byte> k1 = stackalloc byte[16];
        Span<byte> k2 = stackalloc byte[16];
        Span<byte> state = stackalloc byte[16];
        Span<byte> block = stackalloc byte[16];
        try
        {
            zero.Clear();
            state.Clear();
            Assert.Equal(16, aes.EncryptEcb(zero, l, PaddingMode.None));
            Double(l, k1);
            Double(k1, k2);
            int blocks = Math.Max(1, (message.Length + 15) / 16);
            for (int i = 0; i < blocks - 1; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    block[j] = (byte)(state[j] ^ message[i * 16 + j]);
                }
                Assert.Equal(16, aes.EncryptEcb(block, state, PaddingMode.None));
            }
            block.Clear();
            ReadOnlySpan<byte> last = message[((blocks - 1) * 16)..];
            last.CopyTo(block);
            bool complete = !message.IsEmpty && message.Length % 16 == 0;
            if (!complete)
            {
                block[last.Length] = 0x80;
            }
            ReadOnlySpan<byte> subkey = complete ? k1 : k2;
            for (int j = 0; j < 16; j++)
            {
                block[j] ^= (byte)(state[j] ^ subkey[j]);
            }
            Assert.Equal(16, aes.EncryptEcb(block, result, PaddingMode.None));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block);
            CryptographicOperations.ZeroMemory(state);
            CryptographicOperations.ZeroMemory(k2);
            CryptographicOperations.ZeroMemory(k1);
            CryptographicOperations.ZeroMemory(l);
            CryptographicOperations.ZeroMemory(zero);
        }
    }

    private static void Double(ReadOnlySpan<byte> input, Span<byte> output)
    {
        int carry = 0;
        for (int i = 15; i >= 0; i--)
        {
            output[i] = (byte)((input[i] << 1) | carry);
            carry = input[i] >> 7;
        }
        output[15] ^= (byte)(carry * 0x87);
    }
}

// Owned async fixture buffers: fixed laboratory inputs, never real HSM credentials.
internal sealed class SymmetricFixture : IDisposable
{
    public byte[] Management { get; } = new byte[16];
    public byte[] Kenc { get; } = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];
    public byte[] Kmac { get; } = [0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F];
    public byte[] Context { get; } = [0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38];
    public byte[] Password { get; } = [.. "ProbeStart1!"u8];
    public byte[] WrongPassword { get; } = [.. "ProbeWrong2!"u8];
    public byte[] NewPassword { get; } = [.. "ProbeNext03!"u8];
    public byte[] Senc { get; } = new byte[16];
    public byte[] Smac { get; } = new byte[16];
    public byte[] Srmac { get; } = new byte[16];
    public byte[] Card { get; } = new byte[8];

    public void InitializeOracle()
    {
        SymmetricOracle.Derive(Kenc, 0x04, Context, Senc);
        SymmetricOracle.Derive(Kmac, 0x06, Context, Smac);
        SymmetricOracle.Derive(Kmac, 0x07, Context, Srmac);
        SymmetricOracle.Derive(Smac, 0x00, Context, Card);
    }

    public void AssertKeys(Yubico.YubiKit.YubiHsm.SessionKeys keys)
    {
        Assert.Equal(16, keys.SEnc.Length);
        Assert.Equal(16, keys.SMac.Length);
        Assert.Equal(16, keys.SRmac.Length);
        Assert.True(CryptographicOperations.FixedTimeEquals(Senc, keys.SEnc), "S-ENC differs from independent oracle.");
        Assert.True(CryptographicOperations.FixedTimeEquals(Smac, keys.SMac), "S-MAC differs from independent oracle.");
        Assert.True(CryptographicOperations.FixedTimeEquals(Srmac, keys.SRmac), "S-RMAC differs from independent oracle.");
    }

    public void Dispose()
    {
        byte[][] owned = [Management, Kenc, Kmac, Context, Password, WrongPassword, NewPassword, Senc, Smac, Srmac, Card];
        foreach (byte[] bytes in owned)
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }
}
