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

using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Extensions;

namespace Yubico.YubiKit.Ctap2.UnitTests.Extensions;

[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public class ArkgP256SignArgsTests
{
    [Fact]
    public void ArkgP256SignArgs_AlgorithmIsMinus65539()
    {
        var args = new ArkgP256SignArgs(
            new byte[81],
            ReadOnlyMemory<byte>.Empty);

        Assert.Equal(-65539, args.Algorithm);
        Assert.Equal(CoseAlgorithm.ArkgP256.Value, args.Algorithm);
        Assert.Equal(CoseAlgorithm.Esp256SplitArkgPlaceholder.Value, args.Algorithm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(80)]
    [InlineData(82)]
    [InlineData(160)]
    public void ArkgP256SignArgs_RejectsWrongKeyHandleLength(int len)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new ArkgP256SignArgs(new byte[len], ReadOnlyMemory<byte>.Empty));
        Assert.Contains("81 bytes", ex.Message);
        Assert.Equal("keyHandle", ex.ParamName);
    }

    [Theory]
    [InlineData(65)]
    [InlineData(128)]
    public void ArkgP256SignArgs_RejectsContextOver64Bytes(int len)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new ArkgP256SignArgs(new byte[81], new byte[len]));
        Assert.Contains("64 bytes", ex.Message);
        Assert.Equal("context", ex.ParamName);
    }

    [Fact]
    public void ArkgP256SignArgs_AcceptsEmptyContext()
    {
        var args = new ArkgP256SignArgs(new byte[81], ReadOnlyMemory<byte>.Empty);
        byte[] cbor = args.ToAdditionalArgs();

        Assert.Equal(0x21, cbor[^2]);
        Assert.Equal(0x40, cbor[^1]);
    }

    [Fact]
    public void ArkgP256SignArgs_AcceptsExactly64ByteContext()
    {
        var args = new ArkgP256SignArgs(new byte[81], new byte[64]);
        byte[] cbor = args.ToAdditionalArgs();

        var reader = new CborReader(cbor, CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        _ = reader.ReadByteString();
        reader.ReadInt32();
        byte[] ctxOut = reader.ReadByteString();
        Assert.Equal(64, ctxOut.Length);
    }

    [Theory]
    [InlineData("", "40")] // Empty byte string.
    [InlineData("ARKG-P256.test vectors", "56")] // 22-byte string.
    public void ToAdditionalArgs_EncodesExactSigningAlgorithmAndArguments(string label, string contextHeader)
    {
        // Synthetic wire fixture: 16-byte tag and 65-byte uncompressed point, not a valid key.
        var keyHandle = new byte[81];
        keyHandle.AsSpan(0, 16).Fill(0x33);
        keyHandle[16] = 0x04;
        keyHandle.AsSpan(17).Fill(0x44);
        byte[] context = System.Text.Encoding.ASCII.GetBytes(label);
        var args = CoseSignArgs.ArkgP256(keyHandle, context);

        // {3: -65539, -1: bstr(81), -2: bstr(ctx)} in canonical order.
        byte[] expected = [.. Convert.FromHexString("A3033A00010002205851"), .. keyHandle,
            0x21, .. Convert.FromHexString(contextHeader), .. context];

        Assert.Equal(expected, args.ToAdditionalArgs());
    }
}