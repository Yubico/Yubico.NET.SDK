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
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

public class CredBlobAssertionOutputTests
{
    [Fact]
    public void CredBlobAssertionOutput_DecodesCorrectly()
    {
        var blob = new byte[] { 10, 20, 30 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(blob);
        var encoded = writer.Encode();

        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var output = CredBlobAssertionOutput.Decode(reader);

        Assert.Equal(blob, output.Blob.ToArray());
    }
}