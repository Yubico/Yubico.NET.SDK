// Copyright Yubico AB
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

using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Formats.Cbor;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;

namespace Yubico.YubiKit.Ctap2.UnitTests.Extensions;

[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
[Collection(nameof(Ctap2BackendLifecycleTests))] // Serializes temporary process-wide logging changes.
public class PreviewSignGeneratedKeyTests
{
    [Fact]
    public void TryFromMakeCredentialResponse_MalformedOutputWarnsWithoutAllowingLoggerFailureToEscape()
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = new ThrowingWarningLogger();
        factory.CreateLogger(typeof(PreviewSignGeneratedKey).FullName ?? throw new InvalidOperationException()).Returns(logger);
        using var logging = YubiKitLogging.UseTemporary(factory);
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(1);
        writer.WriteTextString("none");
        writer.WriteInt32(2);
        writer.WriteByteString([.. new byte[32], 0xC5, .. new byte[4 + 16], 0, 1, 0x2A, 0xA0,
            .. Convert.FromHexString("A16B707265766965775369676EA10328")]);
        writer.WriteInt32(3);
        writer.WriteEncodedValue([0xA0]);
        writer.WriteEndMap();

        var response = MakeCredentialResponse.Decode(writer.Encode());

        Assert.False(PreviewSignGeneratedKey.TryFromMakeCredentialResponse(response, out var key));
        Assert.Null(key);
        Assert.Equal(1, logger.Calls);
    }

    private sealed class ThrowingWarningLogger : ILogger
    {
        public int Calls { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Calls++;
            throw new InvalidOperationException("Test warning provider failed.");
        }
    }
}