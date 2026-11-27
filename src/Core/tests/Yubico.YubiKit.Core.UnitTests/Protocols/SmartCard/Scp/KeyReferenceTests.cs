// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Scp;

public sealed class KeyReferenceTests
{
    [Fact]
    public void Encoding_UsesCurrentFieldsWithoutChangingEarlierResults()
    {
        var original = new KeyReference(0x13, 0x02);
        var priorMemory = original.AsMemory();
        var priorSpan = original.AsSpan();
        var updated = original with { Kvn = 0x03 };

        AssertEncoding(original, [0x13, 0x02]);
        AssertEncoding(updated, [0x13, 0x03]);
        AssertEncoding(new KeyReference { Kid = 0x13, Kvn = 0x03 }, [0x13, 0x03]);
        Assert.Equal(new byte[] { 0x13, 0x02 }, priorMemory.ToArray());
        Assert.Equal(new byte[] { 0x13, 0x02 }, priorSpan.ToArray());
    }

    [Fact]
    public void ZeroInitialization_EncodesTheExplicitZeroPair_NotTheFactoryReference()
    {
        KeyReference zeroInitialized = default;
        var parameterless = new KeyReference();
        var explicitZero = new KeyReference(0, 0);

        AssertEncoding(zeroInitialized, [0x00, 0x00]);
        AssertEncoding(parameterless, [0x00, 0x00]);
        AssertEncoding(explicitZero, [0x00, 0x00]);
        Assert.Equal(explicitZero, zeroInitialized);
        Assert.Equal(explicitZero, parameterless);
        Assert.Equal(explicitZero.GetHashCode(), zeroInitialized.GetHashCode());
        Assert.Equal(explicitZero.GetHashCode(), parameterless.GetHashCode());
        AssertEncoding(KeyReference.Default, [0x01, 0xFF]);
        Assert.NotEqual(explicitZero, KeyReference.Default);
    }

    [Fact]
    public void EqualReferences_HashEquallyAndSupportCollectionLookup()
    {
        var first = new KeyReference(0x13, 0x03);
        var equal = new KeyReference(0x13, 0x03);
        var different = new KeyReference(0x13, 0x02);
        var references = new HashSet<KeyReference> { first };

        Assert.Equal(first, equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.True(references.TryGetValue(equal, out var stored));
        Assert.Equal(first, stored);
        Assert.NotEqual(first, different);
        Assert.False(references.TryGetValue(different, out _));
    }

    private static void AssertEncoding(KeyReference reference, byte[] expected)
    {
        Assert.Equal(expected, reference.AsMemory().ToArray());
        Assert.Equal(expected, reference.AsSpan().ToArray());
    }
}