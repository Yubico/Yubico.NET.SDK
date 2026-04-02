using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management.UnitTests;

public class FirmwareVersionTests
{
    [Fact]
    public void LessThan()
    {
        Assert.True(new FirmwareVersion(5).IsLessThan(6, 0, 0));
        Assert.True(new FirmwareVersion(5, 9, 9).IsLessThan(6, 0, 0));

        Assert.False(new FirmwareVersion(6).IsLessThan(6, 0, 0));
        Assert.False(new FirmwareVersion(6, 9).IsLessThan(6, 0, 0));
    }

    [Fact]
    public void IsAtleast()
    {
        Assert.False(new FirmwareVersion(5).IsAtLeast(6, 0, 0));
        Assert.False(new FirmwareVersion(5, 9, 9).IsAtLeast(6, 0, 0));

        Assert.True(new FirmwareVersion(6).IsAtLeast(6, 0, 0));
        Assert.True(new FirmwareVersion(6, 9).IsAtLeast(6, 0, 0));
    }


    [Fact]
    public void IsAlphaOrBeta_ReturnsTrue_ForZeroVersion()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);
        Assert.True(alphaKey.IsAlphaOrBeta);
    }

    [Fact]
    public void IsAlphaOrBeta_ReturnsFalse_ForNonZeroVersion()
    {
        Assert.False(new FirmwareVersion(5, 0, 0).IsAlphaOrBeta);
        Assert.False(new FirmwareVersion(0, 1, 0).IsAlphaOrBeta);
        Assert.False(new FirmwareVersion(0, 0, 1).IsAlphaOrBeta);
    }

    [Fact]
    public void AlphaOrBeta_IsAtLeast_AlwaysReturnsTrue()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);

        // Alpha/beta should be "at least" any version
        Assert.True(alphaKey.IsAtLeast(1, 0, 0));
        Assert.True(alphaKey.IsAtLeast(5, 0, 0));
        Assert.True(alphaKey.IsAtLeast(5, 7, 2));
        Assert.True(alphaKey.IsAtLeast(255, 255, 255));
    }

    [Fact]
    public void AlphaOrBeta_IsAtLeast_WithFirmwareVersion_AlwaysReturnsTrue()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);

        Assert.True(alphaKey.IsAtLeast(new FirmwareVersion(1, 0, 0)));
        Assert.True(alphaKey.IsAtLeast(new FirmwareVersion(5, 7, 2)));
        Assert.True(alphaKey.IsAtLeast(FirmwareVersion.V5_8_0));
    }

    [Fact]
    public void AlphaOrBeta_IsLessThan_AlwaysReturnsFalse()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);

        // Alpha/beta should never be "less than" any version
        Assert.False(alphaKey.IsLessThan(1, 0, 0));
        Assert.False(alphaKey.IsLessThan(5, 0, 0));
        Assert.False(alphaKey.IsLessThan(255, 255, 255));
    }

    [Fact]
    public void AlphaOrBeta_IsLessThan_WithFirmwareVersion_AlwaysReturnsFalse()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);

        Assert.False(alphaKey.IsLessThan(new FirmwareVersion(1, 0, 0)));
        Assert.False(alphaKey.IsLessThan(new FirmwareVersion(5, 7, 2)));
        Assert.False(alphaKey.IsLessThan(FirmwareVersion.V5_8_0));
    }

    [Fact]
    public void AlphaOrBeta_CompareTo_ReturnsGreaterThanAnyVersion()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);

        Assert.True(alphaKey.CompareTo(new FirmwareVersion(5, 0, 0)) > 0);
        Assert.True(alphaKey.CompareTo(new FirmwareVersion(255, 255, 255)) > 0);
    }

    [Fact]
    public void AlphaOrBeta_CompareTo_TwoAlphaKeysAreEqual()
    {
        var alphaKey1 = new FirmwareVersion(0, 0, 0);
        var alphaKey2 = new FirmwareVersion(0, 0, 0);

        Assert.Equal(0, alphaKey1.CompareTo(alphaKey2));
        Assert.True(alphaKey1.Equals(alphaKey2));
        Assert.True(alphaKey1 == alphaKey2);
    }

    [Fact]
    public void RegularVersion_CompareTo_AlphaOrBeta_ReturnsLessThan()
    {
        var regularVersion = new FirmwareVersion(5, 7, 2);
        var alphaKey = new FirmwareVersion(0, 0, 0);

        Assert.True(regularVersion.CompareTo(alphaKey) < 0);
        Assert.True(regularVersion < alphaKey);
        Assert.True(alphaKey > regularVersion);
    }

    [Fact]
    public void AlphaOrBeta_OperatorComparisons()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);
        var v5 = new FirmwareVersion(5, 0, 0);

        Assert.True(alphaKey > v5);
        Assert.True(alphaKey >= v5);
        Assert.False(alphaKey < v5);
        Assert.False(alphaKey <= v5);
        Assert.False(alphaKey == v5);
        Assert.True(alphaKey != v5);
    }

    [Fact]
    public void AlphaOrBeta_ToString_ReturnsZeroVersion()
    {
        var alphaKey = new FirmwareVersion(0, 0, 0);
        Assert.Equal("0.0.0", alphaKey.ToString());
    }



    [Fact]
    public void GreaterThan_NullLeft_ReturnsFalse()
    {
        FirmwareVersion? left = null;
        var right = new FirmwareVersion(5, 0, 0);

        Assert.False(left > right);
    }

    [Fact]
    public void GreaterThan_NullRight_ReturnsTrue()
    {
        var left = new FirmwareVersion(5, 0, 0);
        FirmwareVersion? right = null;

        Assert.True(left > right);
    }

    [Fact]
    public void GreaterThan_BothNull_ReturnsFalse()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.False(left > right);
    }

    [Fact]
    public void LessThan_NullLeft_ReturnsTrue_WhenRightIsNotNull()
    {
        FirmwareVersion? left = null;
        var right = new FirmwareVersion(5, 0, 0);

        Assert.True(left < right);
    }

    [Fact]
    public void LessThan_NullRight_ReturnsFalse()
    {
        var left = new FirmwareVersion(5, 0, 0);
        FirmwareVersion? right = null;

        Assert.False(left < right);
    }

    [Fact]
    public void LessThan_BothNull_ReturnsFalse()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.False(left < right);
    }

    [Fact]
    public void GreaterThanOrEqual_NullLeft_NullRight_ReturnsTrue()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.True(left >= right);
    }

    [Fact]
    public void GreaterThanOrEqual_NullLeft_NonNullRight_ReturnsFalse()
    {
        FirmwareVersion? left = null;
        var right = new FirmwareVersion(5, 0, 0);

        Assert.False(left >= right);
    }

    [Fact]
    public void GreaterThanOrEqual_NonNullLeft_NullRight_ReturnsTrue()
    {
        var left = new FirmwareVersion(5, 0, 0);
        FirmwareVersion? right = null;

        Assert.True(left >= right);
    }

    [Fact]
    public void LessThanOrEqual_NullLeft_ReturnsTrue()
    {
        FirmwareVersion? left = null;
        var right = new FirmwareVersion(5, 0, 0);

        Assert.True(left <= right);
    }

    [Fact]
    public void LessThanOrEqual_NullRight_ReturnsFalse()
    {
        var left = new FirmwareVersion(5, 0, 0);
        FirmwareVersion? right = null;

        Assert.False(left <= right);
    }

    [Fact]
    public void LessThanOrEqual_BothNull_ReturnsTrue()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.True(left <= right);
    }

    [Fact]
    public void Equality_BothNull_ReturnsTrue()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.True(left == right);
    }

    [Fact]
    public void Equality_LeftNull_ReturnsFalse()
    {
        FirmwareVersion? left = null;
        var right = new FirmwareVersion(5, 0, 0);

        Assert.False(left == right);
    }

    [Fact]
    public void Equality_RightNull_ReturnsFalse()
    {
        var left = new FirmwareVersion(5, 0, 0);
        FirmwareVersion? right = null;

        Assert.False(left == right);
    }

    [Fact]
    public void Inequality_BothNull_ReturnsFalse()
    {
        FirmwareVersion? left = null;
        FirmwareVersion? right = null;

        Assert.False(left != right);
    }



    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var version = new FirmwareVersion(5, 0, 0);

        Assert.True(version.CompareTo((FirmwareVersion?)null) > 0);
        Assert.True(version.CompareTo((object?)null) > 0);
    }

    [Fact]
    public void CompareTo_SameInstance_ReturnsZero()
    {
        var version = new FirmwareVersion(5, 0, 0);

        Assert.Equal(0, version.CompareTo(version));
    }

    [Fact]
    public void CompareTo_InvalidType_ThrowsArgumentException()
    {
        var version = new FirmwareVersion(5, 0, 0);

        Assert.Throws<ArgumentException>(() => version.CompareTo("not a version"));
        Assert.Throws<ArgumentException>(() => version.CompareTo(42));
    }

    [Fact]
    public void CompareTo_EqualVersions_ReturnsZero()
    {
        var v1 = new FirmwareVersion(5, 7, 2);
        var v2 = new FirmwareVersion(5, 7, 2);

        Assert.Equal(0, v1.CompareTo(v2));
    }

    [Fact]
    public void CompareTo_DifferentMajor()
    {
        var lower = new FirmwareVersion(4, 0, 0);
        var higher = new FirmwareVersion(5, 0, 0);

        Assert.True(lower.CompareTo(higher) < 0);
        Assert.True(higher.CompareTo(lower) > 0);
    }

    [Fact]
    public void CompareTo_DifferentMinor()
    {
        var lower = new FirmwareVersion(5, 6, 0);
        var higher = new FirmwareVersion(5, 7, 0);

        Assert.True(lower.CompareTo(higher) < 0);
        Assert.True(higher.CompareTo(lower) > 0);
    }

    [Fact]
    public void CompareTo_DifferentPatch()
    {
        var lower = new FirmwareVersion(5, 7, 1);
        var higher = new FirmwareVersion(5, 7, 2);

        Assert.True(lower.CompareTo(higher) < 0);
        Assert.True(higher.CompareTo(lower) > 0);
    }



    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var version = new FirmwareVersion(5, 0, 0);

        Assert.False(version.Equals((FirmwareVersion?)null));
        Assert.False(version.Equals((object?)null));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var version = new FirmwareVersion(5, 0, 0);

        Assert.False(version.Equals("5.0.0"));
        Assert.False(version.Equals(500));
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var v1 = new FirmwareVersion(5, 7, 2);
        var v2 = new FirmwareVersion(5, 7, 2);

        Assert.True(v1.Equals(v2));
        Assert.True(v1.Equals((object)v2));
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var v1 = new FirmwareVersion(5, 7, 2);
        var v2 = new FirmwareVersion(5, 7, 2);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        var v1 = new FirmwareVersion(5, 7, 2);
        var v2 = new FirmwareVersion(5, 7, 3);

        // Note: Hash collisions are possible, but unlikely for consecutive versions
        Assert.NotEqual(v1.GetHashCode(), v2.GetHashCode());
    }



    [Fact]
    public void FromString_ValidVersion_ReturnsCorrectVersion()
    {
        var version = FirmwareVersion.FromString("5.7.2");

        Assert.NotNull(version);
        Assert.Equal(5, version.Major);
        Assert.Equal(7, version.Minor);
        Assert.Equal(2, version.Patch);
    }

    [Fact]
    public void FromString_InvalidFormat_ReturnsNull()
    {
        Assert.Null(FirmwareVersion.FromString("5.7"));
        Assert.Null(FirmwareVersion.FromString("5.7.2.1"));
        Assert.Null(FirmwareVersion.FromString("5"));
        Assert.Null(FirmwareVersion.FromString(""));
        Assert.Null(FirmwareVersion.FromString("a.b.c"));
    }

    [Fact]
    public void FromString_OutOfRange_ReturnsNull()
    {
        Assert.Null(FirmwareVersion.FromString("256.0.0"));
        Assert.Null(FirmwareVersion.FromString("0.256.0"));
        Assert.Null(FirmwareVersion.FromString("0.0.256"));
        Assert.Null(FirmwareVersion.FromString("-1.0.0"));
    }

    [Fact]
    public void FromString_ZeroVersion_ReturnsAlphaOrBeta()
    {
        var version = FirmwareVersion.FromString("0.0.0");

        Assert.NotNull(version);
        Assert.True(version.IsAlphaOrBeta);
    }



    [Fact]
    public void FromBytes_ValidBytes_ReturnsCorrectVersion()
    {
        var bytes = new byte[] { 5, 7, 2 };
        var version = FirmwareVersion.FromBytes(bytes);

        Assert.Equal(5, version.Major);
        Assert.Equal(7, version.Minor);
        Assert.Equal(2, version.Patch);
    }

    [Fact]
    public void FromBytes_InvalidLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FirmwareVersion.FromBytes([5, 7]));
        Assert.Throws<ArgumentException>(() => FirmwareVersion.FromBytes([5, 7, 2, 1]));
        Assert.Throws<ArgumentException>(() => FirmwareVersion.FromBytes([]));
    }



    [Fact]
    public void Constructor_DefaultValues()
    {
        var version = new FirmwareVersion(5);

        Assert.Equal(5, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
    }

    [Fact]
    public void Constructor_AllValues()
    {
        var version = new FirmwareVersion(5, 7, 2);

        Assert.Equal(5, version.Major);
        Assert.Equal(7, version.Minor);
        Assert.Equal(2, version.Patch);
    }

    [Fact]
    public void Constructor_MaxValues()
    {
        var version = new FirmwareVersion(255, 255, 255);

        Assert.Equal(255, version.Major);
        Assert.Equal(255, version.Minor);
        Assert.Equal(255, version.Patch);
    }

    [Fact]
    public void Constructor_Parameterless()
    {
        var version = new FirmwareVersion();

        Assert.Equal(0, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
        Assert.True(version.IsAlphaOrBeta);
    }



    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        var version = new FirmwareVersion(5, 7, 2);
        Assert.Equal("5.7.2", version.ToString());
    }

    [Fact]
    public void ToString_MaxValues()
    {
        var version = new FirmwareVersion(255, 255, 255);
        Assert.Equal("255.255.255", version.ToString());
    }



    [Fact]
    public void Default_IsAlphaOrBeta()
    {
        Assert.True(FirmwareVersion.Default.IsAlphaOrBeta);
        Assert.Equal(0, FirmwareVersion.Default.Major);
        Assert.Equal(0, FirmwareVersion.Default.Minor);
        Assert.Equal(0, FirmwareVersion.Default.Patch);
    }

    [Fact]
    public void StaticVersions_HaveCorrectValues()
    {
        Assert.Equal(5, FirmwareVersion.V5_3_0.Major);
        Assert.Equal(3, FirmwareVersion.V5_3_0.Minor);
        Assert.Equal(0, FirmwareVersion.V5_3_0.Patch);

        Assert.Equal(5, FirmwareVersion.V5_4_3.Major);
        Assert.Equal(4, FirmwareVersion.V5_4_3.Minor);
        Assert.Equal(3, FirmwareVersion.V5_4_3.Patch);

        Assert.Equal(5, FirmwareVersion.V5_7_2.Major);
        Assert.Equal(7, FirmwareVersion.V5_7_2.Minor);
        Assert.Equal(2, FirmwareVersion.V5_7_2.Patch);

        Assert.Equal(5, FirmwareVersion.V5_8_0.Major);
        Assert.Equal(8, FirmwareVersion.V5_8_0.Minor);
        Assert.Equal(0, FirmwareVersion.V5_8_0.Patch);
    }



    [Fact]
    public void Operators_AreConsistentWithCompareTo()
    {
        var v1 = new FirmwareVersion(5, 7, 0);
        var v2 = new FirmwareVersion(5, 7, 2);
        var v3 = new FirmwareVersion(5, 7, 2);

        // v1 < v2
        Assert.True(v1.CompareTo(v2) < 0);
        Assert.True(v1 < v2);
        Assert.True(v1 <= v2);
        Assert.False(v1 > v2);
        Assert.False(v1 >= v2);
        Assert.False(v1 == v2);
        Assert.True(v1 != v2);

        // v2 == v3
        Assert.Equal(0, v2.CompareTo(v3));
        Assert.False(v2 < v3);
        Assert.True(v2 <= v3);
        Assert.False(v2 > v3);
        Assert.True(v2 >= v3);
        Assert.True(v2 == v3);
        Assert.False(v2 != v3);

        // v2 > v1
        Assert.True(v2.CompareTo(v1) > 0);
        Assert.False(v2 < v1);
        Assert.False(v2 <= v1);
        Assert.True(v2 > v1);
        Assert.True(v2 >= v1);
    }

}