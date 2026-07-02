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
using Xunit;
using Yubico.YubiKit.Fido2.Cose;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

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
        byte[] cbor = PreviewSignCbor.EncodeCoseSignArgs(args);

        Assert.Equal(0x21, cbor[^2]);
        Assert.Equal(0x40, cbor[^1]);
    }

    [Fact]
    public void ArkgP256SignArgs_AcceptsExactly64ByteContext()
    {
        var args = new ArkgP256SignArgs(new byte[81], new byte[64]);
        byte[] cbor = PreviewSignCbor.EncodeCoseSignArgs(args);

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
}