// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public sealed class ApplicationIdsTests
{
    public static TheoryData<string> IdentifierNames =>
    [
        nameof(ApplicationIds.Management),
        nameof(ApplicationIds.Otp),
        nameof(ApplicationIds.FidoU2f),
        nameof(ApplicationIds.Fido2),
        nameof(ApplicationIds.Oath),
        nameof(ApplicationIds.OpenPgp),
        nameof(ApplicationIds.Piv),
        nameof(ApplicationIds.YubiHsmAuth),
        nameof(ApplicationIds.SecurityDomain)
    ];

    [Fact]
    public void ApplicationIds_IsStatic()
    {
        Assert.True(typeof(ApplicationIds).IsAbstract);
        Assert.True(typeof(ApplicationIds).IsSealed);
    }

    [Theory]
    [MemberData(nameof(IdentifierNames))]
    public void Identifier_IsReadOnlyMemory(string identifierName)
    {
        PropertyInfo property = GetIdentifierProperty(identifierName);

        Assert.Equal(typeof(ReadOnlyMemory<byte>), property.PropertyType);
        Assert.False(Read(property).IsEmpty);
    }

    /// <summary>
    ///     A caller that recovers the array behind the returned memory and writes through it must not be
    ///     able to affect what any later caller reads.
    /// </summary>
    /// <remarks>
    ///     <see cref="ReadOnlyMemory{T}" /> is not by itself a barrier: <see cref="MemoryMarshal.TryGetArray{T}" />
    ///     hands back the underlying array, and writes through it are visible to everyone. If the identifiers
    ///     were exposed over one shared array, a single such write would corrupt applet selection for every
    ///     session in the process. Each read therefore returns its own copy, and this test asserts that by
    ///     performing exactly the write that would otherwise escape.
    /// </remarks>
    [Theory]
    [MemberData(nameof(IdentifierNames))]
    public void Identifier_WritingThroughRecoveredArray_DoesNotAffectLaterReads(string identifierName)
    {
        PropertyInfo property = GetIdentifierProperty(identifierName);
        byte[] expected = Read(property).ToArray();

        ReadOnlyMemory<byte> handedOut = Read(property);
        Assert.True(MemoryMarshal.TryGetArray(handedOut, out ArraySegment<byte> segment));
        Assert.NotNull(segment.Array);
        segment.Array[segment.Offset] ^= 0xFF;

        Assert.Equal(expected, Read(property).ToArray());
    }

    [Theory]
    [MemberData(nameof(IdentifierNames))]
    public void Identifier_DoesNotHandOutTheSameArrayTwice(string identifierName)
    {
        PropertyInfo property = GetIdentifierProperty(identifierName);

        Assert.True(MemoryMarshal.TryGetArray(Read(property), out ArraySegment<byte> first));
        Assert.True(MemoryMarshal.TryGetArray(Read(property), out ArraySegment<byte> second));

        Assert.NotSame(first.Array, second.Array);
    }

    [Fact]
    public void Identifiers_HaveTheirDocumentedValues()
    {
        Assert.Equal([0xA0, 0x00, 0x00, 0x05, 0x27, 0x47, 0x11, 0x17], ApplicationIds.Management.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01, 0x01], ApplicationIds.Otp.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01], ApplicationIds.FidoU2f.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x06, 0x47, 0x2F, 0x00, 0x01], ApplicationIds.Fido2.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x01], ApplicationIds.Oath.ToArray());
        Assert.Equal([0xD2, 0x76, 0x00, 0x01, 0x24, 0x01], ApplicationIds.OpenPgp.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x03, 0x08, 0x00, 0x00, 0x10, 0x00], ApplicationIds.Piv.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x07, 0x01], ApplicationIds.YubiHsmAuth.ToArray());
        Assert.Equal([0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00], ApplicationIds.SecurityDomain.ToArray());
    }

    private static PropertyInfo GetIdentifierProperty(string identifierName)
    {
        PropertyInfo? property = typeof(ApplicationIds).GetProperty(
            identifierName,
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(property);
        return property;
    }

    private static ReadOnlyMemory<byte> Read(PropertyInfo property) =>
        Assert.IsType<ReadOnlyMemory<byte>>(property.GetValue(null));
}
