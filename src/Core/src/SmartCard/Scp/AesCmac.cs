using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

internal sealed class AesCmac : IDisposable
{
    private const int BlockSize = 16;
    private const int Aes128KeyLength = 16;
    private const byte PaddingByte = 0x80;
    private const byte ReductionPolynomial = 0x87;

    private readonly Aes _aes;
    private readonly byte[] _buffer = new byte[BlockSize];
    private readonly byte[] _cbcState = new byte[BlockSize];
    private readonly byte[] _keyBuffer = GC.AllocateUninitializedArray<byte>(Aes128KeyLength, true);
    private readonly byte[] _subkey1 = GC.AllocateUninitializedArray<byte>(BlockSize, true);
    private readonly byte[] _subkey2 = GC.AllocateUninitializedArray<byte>(BlockSize, true);
    private int _bufferOffset;
    private bool _disposed;

    public AesCmac(ReadOnlySpan<byte> key)
    {
        if (key.Length != Aes128KeyLength)
            throw new ArgumentException($"Key must be {Aes128KeyLength} bytes", nameof(key));

        try
        {
            key.CopyTo(_keyBuffer);
            _aes = Aes.Create();
            _aes.Key = _keyBuffer;
            GenerateSubkeys();
        }
        catch
        {
            _aes?.Dispose();
            CryptographicOperations.ZeroMemory(_keyBuffer);
            throw;
        }
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (_disposed) return;

        _aes.Dispose();
        CryptographicOperations.ZeroMemory(_keyBuffer);
        CryptographicOperations.ZeroMemory(_subkey1);
        CryptographicOperations.ZeroMemory(_subkey2);
        CryptographicOperations.ZeroMemory(_buffer);
        CryptographicOperations.ZeroMemory(_cbcState);
        _disposed = true;
    }

    #endregion

    public void AppendData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var offset = 0;

        if (_bufferOffset > 0 && data.Length > 0)
        {
            var toCopy = Math.Min(BlockSize - _bufferOffset, data.Length);
            data[..toCopy].CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += toCopy;
            offset = toCopy;

            if (_bufferOffset == BlockSize && offset < data.Length)
            {
                ProcessBlock(_buffer);
                _bufferOffset = 0;
            }
        }

        while (offset + BlockSize < data.Length)
        {
            ProcessBlock(data.Slice(offset, BlockSize));
            offset += BlockSize;
        }

        var remaining = data.Length - offset;
        if (remaining > 0)
        {
            data.Slice(offset, remaining).CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += remaining;
        }
    }

    public byte[] GetHashAndReset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            Span<byte> lastBlock = stackalloc byte[BlockSize];

            if (_bufferOffset == BlockSize)
            {
                XorBlocks(_buffer, _subkey1, _cbcState, lastBlock);
            }
            else
            {
                _buffer.AsSpan(0, _bufferOffset).CopyTo(lastBlock);
                lastBlock[_bufferOffset] = PaddingByte;
                XorBlocks(lastBlock, _subkey2, _cbcState, lastBlock);
            }

            var result = new byte[BlockSize];
            var bytesWritten = _aes.EncryptEcb(lastBlock, result, PaddingMode.None);
            if (bytesWritten != BlockSize)
                throw new CryptographicException($"Final encryption failed: {bytesWritten} bytes");

            _bufferOffset = 0;
            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(_buffer);
            CryptographicOperations.ZeroMemory(_cbcState);
        }
    }

    public bool VerifyAndReset(ReadOnlySpan<byte> expectedMac) =>
        CryptographicOperations.FixedTimeEquals(GetHashAndReset(), expectedMac);

    private void GenerateSubkeys()
    {
        Span<byte> zero = stackalloc byte[BlockSize];
        Span<byte> l = stackalloc byte[BlockSize];

        try
        {
            _aes.EncryptEcb(zero, l, PaddingMode.None);
            LeftShiftOneAnd(l, _subkey1);
            LeftShiftOneAnd(_subkey1, _subkey2);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(l);
        }
    }

    private static void LeftShiftOneAnd(ReadOnlySpan<byte> input, Span<byte> output)
    {
        byte overflow = 0;
        for (var i = BlockSize - 1; i >= 0; i--)
        {
            output[i] = (byte)((input[i] << 1) | overflow);
            overflow = (byte)((input[i] >> 7) & 1);
        }

        if (overflow != 0)
            output[BlockSize - 1] ^= ReductionPolynomial;
    }

    private void ProcessBlock(ReadOnlySpan<byte> block)
    {
        Span<byte> temp = stackalloc byte[BlockSize];
        XorBlocks(block, _cbcState, temp);

        var bytesWritten = _aes.EncryptEcb(temp, _cbcState, PaddingMode.None);
        if (bytesWritten != BlockSize)
            throw new CryptographicException($"Block encryption failed: {bytesWritten} bytes");
    }

    // XOR helpers
    private static void XorBlocks(
        ReadOnlySpan<byte> a,
        ReadOnlySpan<byte> b,
        Span<byte> output)
    {
        for (var i = 0; i < BlockSize; i++)
            output[i] = (byte)(a[i] ^ b[i]);
    }

    private static void XorBlocks(
        ReadOnlySpan<byte> a,
        ReadOnlySpan<byte> b,
        ReadOnlySpan<byte> c,
        Span<byte> output)
    {
        for (var i = 0; i < BlockSize; i++)
            output[i] = (byte)(a[i] ^ b[i] ^ c[i]);
    }
#if DEBUG
    // Exposed for unit testing only
    internal ReadOnlySpan<byte> DebugGetSubkey1() => _subkey1;
    internal ReadOnlySpan<byte> DebugGetSubkey2() => _subkey2;
#endif
}