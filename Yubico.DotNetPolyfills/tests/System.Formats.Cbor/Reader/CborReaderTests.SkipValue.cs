// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Source: https://github.com/dotnet/runtime/tree/v5.0.0-preview.7.20364.11/src/libraries/System.Formats.Cbor/tests

#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System;

namespace System.Formats.Cbor.UnitTests
{
    public partial class CborReaderTests
    {
        [Theory] // External CBOR library test
        [MemberData(nameof(SkipTestInputs))]
        public static void SkipValue_RootValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [MemberData(nameof(SkipTestInputs))]
        public static void SkipValue_NestedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"8301{hexEncoding}03".HexToByteArray();
            var reader = new CborReader(encoding);

            _ = reader.ReadStartArray();
            _ = reader.ReadInt64();
            reader.SkipValue();
            _ = reader.ReadInt64();
            reader.ReadEndArray();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [MemberData(nameof(SkipTestInputs))]
        public static void SkipValue_TaggedValue_HappyPath(string hexEncoding)
        {
            byte[] encoding = $"c2{hexEncoding}".HexToByteArray();
            var reader = new CborReader(encoding);

            _ = reader.ReadTag();
            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact] // External CBOR library test
        public static void SkipValue_NotAtValue_ShouldThrowInvalidOperationException()
        {
            byte[] encoding = "80".HexToByteArray();
            var reader = new CborReader(encoding);

            _ = reader.ReadStartArray();

            int bytesRemaining = reader.BytesRemaining;
            _ = Assert.Throws<InvalidOperationException>(() => reader.SkipValue());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory] // External CBOR library test
        [MemberData(nameof(SkipTestInvalidCborInputs))]
        public static void SkipValue_InvalidFormat_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            _ = Assert.Throws<CborContentException>(() => reader.SkipValue());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory] // External CBOR library test
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void SkipValue_InvalidUtf8_LaxConformance_ShouldSucceed(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, CborConformanceMode.Lax);

            reader.SkipValue();

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [InlineData(CborConformanceMode.Lax)]
        public static void SkipValue_ValidationEnabled_InvalidUtf8_LaxConformance_ShouldSucceed(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void SkipValue_ValidationEnabled_InvalidUtf8_StrictConformance_ShouldThrowCborContentException(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);

            CborContentException exn = Assert.Throws<CborContentException>(() => reader.SkipValue());
            Assert.NotNull(exn.InnerException);
            _ = Assert.IsType<DecoderFallbackException>(exn.InnerException);

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory] // External CBOR library test
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void SkipValue_ValidationDisabled_NonConformingValues_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);

            reader.SkipValue(disableConformanceModeChecks: true);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [MemberData(nameof(NonConformingSkipValueEncodings))]
        public static void SkipValue_ValidationEnabled_NonConformingValues_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);

            _ = Assert.Throws<CborContentException>(() => reader.SkipValue());
        }

        public static IEnumerable<object[]> NonConformingSkipValueEncodings =>
            new (CborConformanceMode Mode, string Encoding)[]
            {
                (CborConformanceMode.Ctap2Canonical, "1801"), // non-canonical integer representation
                (CborConformanceMode.Canonical, "5fff"), // indefinite-length byte string
                (CborConformanceMode.Canonical, "7fff"), // indefinite-length text string
                (CborConformanceMode.Canonical, "9fff"), // indefinite-length array
                (CborConformanceMode.Canonical, "bfff"), // indefinite-length map
                (CborConformanceMode.Strict, "a201020103"), // duplicate keys in map
                (CborConformanceMode.Canonical, "a201020103"), // duplicate keys in map
                (CborConformanceMode.Ctap2Canonical, "a202020101"), // unsorted keys in map
                (CborConformanceMode.Ctap2Canonical, "c001"), // tagged value
                (CborConformanceMode.Strict, "f81f"), // non-canonical simple value
            }.Select(l => new object[] { l.Mode, l.Encoding });

        [Fact] // External CBOR library test
        public static void SkipValue_SkippedValueFollowedByNonConformingValue_ShouldThrowCborContentException()
        {
            byte[] encoding = "827fff7fff".HexToByteArray();
            var reader = new CborReader(encoding, CborConformanceMode.Ctap2Canonical);

            _ = reader.ReadStartArray();
            reader.SkipValue(disableConformanceModeChecks: true);
            _ = Assert.Throws<CborContentException>(() => reader.ReadTextString());
        }

        [Fact] // External CBOR library test
        public static void SkipValue_NestedCborContentException_ShouldPreserveOriginalReaderState()
        {
            string hexEncoding = "820181bf01ff"; // [1, [ {_ 1 : <missing value> } ]]
            var reader = new CborReader(hexEncoding.HexToByteArray());

            _ = reader.ReadStartArray();
            _ = reader.ReadInt64();

            // capture current state
            int bytesRemaining = reader.BytesRemaining;

            // make failing call
            _ = Assert.Throws<CborContentException>(() => reader.SkipValue());

            // ensure reader state has reverted to original
            Assert.Equal(reader.BytesRemaining, bytesRemaining);

            // ensure we can read every value up to the format error
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
            _ = reader.ReadStartArray();
            Assert.Equal(CborReaderState.StartMap, reader.PeekState());
            _ = reader.ReadStartMap();
            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            _ = reader.ReadUInt64();
            _ = Assert.Throws<CborContentException>(() => reader.PeekState());
        }

        [Theory] // External CBOR library test
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SkipToParent_SimpleArray_HappyPath(int skipOffset)
        {
            byte[] encoding = "83010203".HexToByteArray(); // [1, 2, 3]
            var reader = new CborReader(encoding);

            _ = reader.ReadStartArray();
            for (int i = 0; i < skipOffset; i++)
            {
                _ = reader.ReadInt32();
            }

            reader.SkipToParent();

            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SkipToParent_NestedArray_HappyPath(int skipOffset)
        {
            byte[] encoding = "8283010203a0".HexToByteArray(); // [[1, 2, 3], { }]
            var reader = new CborReader(encoding);

            _ = reader.ReadStartArray();
            _ = reader.ReadStartArray();
            for (int i = 0; i < skipOffset; i++)
            {
                _ = reader.ReadInt32();
            }

            reader.SkipToParent();
            Assert.Equal(CborReaderState.StartMap, reader.PeekState());
        }

        [Theory] // External CBOR library test
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SkipToParent_NestedKey_HappyPath(int skipOffset)
        {
            byte[] encoding = "a17f616161626163ff80".HexToByteArray(); // { (_ "a", "b", "c") : [] }
            var reader = new CborReader(encoding);

            _ = reader.ReadStartMap();
            reader.ReadStartIndefiniteLengthTextString();
            for (int i = 0; i < skipOffset; i++)
            {
                _ = reader.ReadTextString();
            }

            reader.SkipToParent();
            Assert.Equal(CborReaderState.StartArray, reader.PeekState());
        }

        [Fact] // External CBOR library test
        public static void SkipToParent_RootContext_ShouldThrowInvalidOperationException()
        {
            byte[] encoding = "01".HexToByteArray();
            var reader = new CborReader(encoding);

            _ = Assert.Throws<InvalidOperationException>(() => reader.SkipToParent());
            _ = reader.ReadInt32();
            _ = Assert.Throws<InvalidOperationException>(() => reader.SkipToParent());
        }

        [Theory] // External CBOR library test
        [InlineData(50_000)]
        public static void SkipValue_ExtremelyNestedValues_ShouldNotStackOverflow(int depth)
        {
            // Construct a valid CBOR encoding with extreme nesting:
            // defines a tower of `depth` nested singleton arrays,
            // with the innermost array containing zero.
            byte[] encoding = new byte[depth + 1];
            encoding.AsSpan(0, depth).Fill(0x81); // array of length 1
            encoding[depth] = 0;

            var reader = new CborReader(encoding);

            reader.SkipValue();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        public static IEnumerable<object[]> SkipTestInputs => SampleCborValues.Select(x => new [] { x });
        public static IEnumerable<object[]> SkipTestInvalidCborInputs => InvalidCborValues.Select(x => new[] { x });
    }
}
