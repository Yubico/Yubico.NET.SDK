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

using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Iso7816;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Yubico.YubiKey;

public class YubiKeyDeviceEqualityTests
{
    private YubiKeyDevice CreateDevice(
        int? serialNumber = null,
        string? scPath = null,
        string? hidFidoPath = null,
        string? hidKeyboardPath = null,
        FirmwareVersion? firmwareVersion = null)
    {
        var deviceInfo = new YubiKeyDeviceInfo
        {
            SerialNumber = serialNumber,
            FirmwareVersion = firmwareVersion ?? new FirmwareVersion(5, 4, 3),
            AvailableUsbCapabilities = YubiKeyCapabilities.All,
            EnabledUsbCapabilities = YubiKeyCapabilities.All
        };

        var smartCard = scPath != null ? new MockSmartCardDevice(scPath) : null;
        var hidFido = hidFidoPath != null ? new MockHidFidoDevice(hidFidoPath) : null;
        var hidKeyboard = hidKeyboardPath != null ? new MockHidKeyboardDevice(hidKeyboardPath) : null;

        return new YubiKeyDevice(smartCard, hidKeyboard, hidFido, deviceInfo);
    }

    [Fact]
    public void Equals_SameSerialNumbers_ReturnsTrue()
    {
        var device1 = CreateDevice(serialNumber: 12345);
        var device2 = CreateDevice(serialNumber: 12345);

        Assert.True(device1.Equals(device2));
        Assert.True(device2.Equals(device1));
        Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentSerialNumbers_ReturnsFalse()
    {
        var device1 = CreateDevice(serialNumber: 12345);
        var device2 = CreateDevice(serialNumber: 67890);

        Assert.False(device1.Equals(device2));
        Assert.False(device2.Equals(device1));
    }

    [Fact]
    public void Equals_OneSerialNumberNull_ReturnsFalse()
    {
        var device1 = CreateDevice(serialNumber: 12345);
        var device2 = CreateDevice(serialNumber: null, scPath: "path1");

        Assert.False(device1.Equals(device2));
        Assert.False(device2.Equals(device1));
    }

    [Fact]
    public void GetHashCode_ContractViolation_ExposesOriginalBug()
    {
        // This exposes the firmware version bug in original GetHashCode
        var device1 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(1, 0, 0));
        var device2 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(2, 0, 0));

        bool areEqual = device1.Equals(device2);
        bool hashCodesEqual = device1.GetHashCode() == device2.GetHashCode();

        // Objects are NOT equal (different firmware), but hash codes ARE equal (bug!)
        Assert.False(areEqual); // "Devices with different firmware should not be equal"
        Assert.False(hashCodesEqual); // "This demonstrates the hash code bug"
    }

    [Fact]
    public void Equals_BothSerialNumbersNull_SamePathsAndFirmware_ReturnsTrue()
    {
        var firmware = new FirmwareVersion(5, 4, 3);
        var device1 = CreateDevice(scPath: "sc1", hidFidoPath: "hf1", hidKeyboardPath: "hk1",
            firmwareVersion: firmware);
        var device2 = CreateDevice(scPath: "sc1", hidFidoPath: "hf1", hidKeyboardPath: "hk1",
            firmwareVersion: firmware);

        Assert.True(device1.Equals(device2));
        Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
    }

    [Fact]
    public void CompareTo_SerialNumbers_OrdersCorrectly()
    {
        var device1 = CreateDevice(serialNumber: 100);
        var device2 = CreateDevice(serialNumber: 200);
        var device3 = CreateDevice(serialNumber: 100);

        Assert.True(device1.CompareTo(device2) < 0);
        Assert.True(device2.CompareTo(device1) > 0);
        Assert.Equal(0, device1.CompareTo(device3));
    }

    [Fact]
    public void CompareTo_NoSerialNumbers_OrdersBySmartCardPath()
    {
        var device1 = CreateDevice(scPath: "path_a");
        var device2 = CreateDevice(scPath: "path_b");
        var device3 = CreateDevice(scPath: "path_a");

        Assert.True(device1.CompareTo(device2) < 0);
        Assert.True(device2.CompareTo(device1) > 0);
        Assert.Equal(0, device1.CompareTo(device3));
    }

    [Theory]
    [InlineData("path_a", "path_b", -1)]
    [InlineData("path_b", "path_a", 1)]
    [InlineData("path_same", "path_same", 0)]
    public void CompareTo_NoSerialNumbers_PathOrdering_Theory(
        string path1,
        string path2,
        int expectedSign)
    {
        var device1 = CreateDevice(scPath: path1);
        var device2 = CreateDevice(scPath: path2);

        var result = device1.CompareTo(device2);
        Assert.Equal(Math.Sign(expectedSign), Math.Sign(result));
    }

    [Fact]
    public void HashSet_BehaviorWith_BuggyHashCode()
    {
        var device1 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(1, 0));
        var device2 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(2, 0));
        var device3 = CreateDevice(serialNumber: 123);

        var hashSet = new HashSet<YubiKeyDevice> { device1, device2, device3 };

        // Should be 3 devices, but might only be 2 due to hash collision
        Assert.Equal(3, hashSet.Count);
        Assert.Contains(device1, hashSet);
        Assert.Contains(device2, hashSet); // This might fail due to hash code bug
        Assert.Contains(device3, hashSet);
    }

    [Fact]
    public void Operators_ConsistentWithMethods()
    {
        var device1 = CreateDevice(serialNumber: 100);
        var device2 = CreateDevice(serialNumber: 200);
        var device3 = CreateDevice(serialNumber: 100);

        Assert.Equal(device1.Equals(device2), device1 == device2);
        Assert.Equal(!device1.Equals(device2), device1 != device2);
        Assert.Equal(device1.Equals(device3), device1 == device3);

        Assert.Equal(device1.CompareTo(device2) < 0, device1 < device2);
        Assert.Equal(device1.CompareTo(device2) <= 0, device1 <= device2);
        Assert.Equal(device2.CompareTo(device1) > 0, device2 > device1);
        Assert.Equal(device2.CompareTo(device1) >= 0, device2 >= device1);
    }


    [Fact]
    public void Operators_Equality_ConsistentWithEquals()
    {
        var device1 = CreateDevice(serialNumber: 100);
        var device2 = CreateDevice(serialNumber: 200);
        var device3 = CreateDevice(serialNumber: 100);
        var device4 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(1, 0));
        var device5 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(2, 0));

        // Test equality operators match Equals method
        Assert.Equal(device1.Equals(device2), device1 == device2);
        Assert.Equal(device1.Equals(device3), device1 == device3);
        Assert.Equal(device4.Equals(device5), device4 == device5);

        // Test inequality operators
        Assert.Equal(!device1.Equals(device2), device1 != device2);
        Assert.Equal(!device1.Equals(device3), device1 != device3);
        Assert.Equal(!device4.Equals(device5), device4 != device5);
    }

    [Fact]
    public void Operators_Comparison_ConsistentWithCompareTo()
    {
        var device1 = CreateDevice(serialNumber: 100);
        var device2 = CreateDevice(serialNumber: 200);
        var device3 = CreateDevice(serialNumber: 100);
        var device4 = CreateDevice(scPath: "path_a");
        var device5 = CreateDevice(scPath: "path_b");

        // Less than
        Assert.Equal(device1.CompareTo(device2) < 0, device1 < device2);
        Assert.Equal(device4.CompareTo(device5) < 0, device4 < device5);

        // Less than or equal
        Assert.Equal(device1.CompareTo(device2) <= 0, device1 <= device2);
        Assert.Equal(device1.CompareTo(device3) <= 0, device1 <= device3);

        // Greater than
        Assert.Equal(device2.CompareTo(device1) > 0, device2 > device1);
        Assert.Equal(device5.CompareTo(device4) > 0, device5 > device4);

        // Greater than or equal
        Assert.Equal(device2.CompareTo(device1) >= 0, device2 >= device1);
        Assert.Equal(device1.CompareTo(device3) >= 0, device1 >= device3);
    }

    [Fact]
    public void Operators_NullHandling()
    {
        var device = CreateDevice(serialNumber: 123);
        YubiKeyDevice nullDevice = null!;

        // Null comparisons
        Assert.True(nullDevice == null!);
        Assert.False(nullDevice! == device);
        Assert.False(device == nullDevice!);

        Assert.False(nullDevice! != null!);
        Assert.True(nullDevice! != device);
        Assert.True(device != nullDevice!);

        // Null ordering
        Assert.True(nullDevice! < device);
        Assert.True(nullDevice! <= device);
        Assert.False(nullDevice! > device);
        Assert.False(nullDevice! >= device);

        Assert.False(device < nullDevice!);
        Assert.False(device <= nullDevice!);
        Assert.True(device > nullDevice!);
        Assert.True(device >= nullDevice!);
    }

    [Fact]
    public void HashSet_ContractViolation_DemonstratesBug()
    {
        // Different devices that should be in set, but hash code bug might cause issues
        var device1 = CreateDevice(scPath: "same", hidFidoPath: "fido1", firmwareVersion: new FirmwareVersion(1, 0));
        var device2 = CreateDevice(scPath: "same", hidFidoPath: "fido1", firmwareVersion: new FirmwareVersion(2, 0));
        var device3 = CreateDevice(serialNumber: 123);
        var device4 = CreateDevice(serialNumber: 456);

#pragma warning disable IDE0028
        var hashSet = new HashSet<YubiKeyDevice> { device1, device3 };
#pragma warning restore IDE0028

        // Add devices that should be unique
        hashSet.Add(device2); // Different firmware, should be unique
        hashSet.Add(device4); // Different serial, should be unique

        Assert.Equal(4, hashSet.Count); // Should have 4 unique devices
        Assert.Contains(device1, hashSet);
        Assert.Contains(device2, hashSet); // This might fail due to hash collision
        Assert.Contains(device3, hashSet);
        Assert.Contains(device4, hashSet);

        // Test removal works correctly
        Assert.True(hashSet.Remove(device1));
        Assert.False(hashSet.Remove(device1)); // Already removed
        Assert.Equal(3, hashSet.Count);
    }

    [Fact]
    public void Dictionary_BehaviorWithBuggyHashCode()
    {
        var device1 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(1, 0));
        var device2 = CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(2, 0));
        var device3 = CreateDevice(serialNumber: 789);

#pragma warning disable IDE0028
        var dictionary = new Dictionary<YubiKeyDevice, string>();
#pragma warning restore IDE0028

        // Add devices as keys
        dictionary[device1] = "Device1";
        dictionary[device2] = "Device2"; // Different device, should not overwrite
        dictionary[device3] = "Device3";

        Assert.Equal(3, dictionary.Count); // Should have 3 entries

        // Test retrieval
        Assert.Equal("Device1", dictionary[device1]);
        Assert.Equal("Device2", dictionary[device2]); // This might fail due to hash collision
        Assert.Equal("Device3", dictionary[device3]);

        // Test key existence
        Assert.True(dictionary.ContainsKey(device1));
        Assert.True(dictionary.ContainsKey(device2));
        Assert.True(dictionary.ContainsKey(device3));
    }

    [Fact]
    public void Equality_Reflexive()
    {
        var device = CreateDevice(serialNumber: 123);

#pragma warning disable CS1718 // Comparison made to same variable
        Assert.True(device.Equals(device));
        Assert.True(device == device);
        Assert.False(device != device);
#pragma warning restore CS1718

    }

    [Fact]
    public void Equality_Symmetric()
    {
        var device1 = CreateDevice(serialNumber: 123);
        var device2 = CreateDevice(serialNumber: 123);
        var device3 = CreateDevice(serialNumber: 456);

        // Equal devices
        Assert.Equal(device1.Equals(device2), device2.Equals(device1));
        Assert.Equal(device1 == device2, device2 == device1);

        // Unequal devices
        Assert.Equal(device1.Equals(device3), device3.Equals(device1));
        Assert.Equal(device1 == device3, device3 == device1);
    }

    [Fact]
    public void Equality_Transitive()
    {
        var device1 = CreateDevice(serialNumber: 123);
        var device2 = CreateDevice(serialNumber: 123);
        var device3 = CreateDevice(serialNumber: 123);

        Assert.True(device1.Equals(device2));
        Assert.True(device2.Equals(device3));
        Assert.True(device1.Equals(device3)); // Transitivity

        // Operator consistency
        Assert.True(device1 == device2);
        Assert.True(device2 == device3);
        Assert.True(device1 == device3);
    }

    [Fact]
    public void CompareTo_ConsistentWithEquals()
    {
        var testCases = new[]
        {
            (CreateDevice(serialNumber: 123), CreateDevice(serialNumber: 123)),
            (CreateDevice(serialNumber: 123), CreateDevice(serialNumber: 456)),
            (CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(1, 0)),
                CreateDevice(scPath: "same", firmwareVersion: new FirmwareVersion(1, 0))),
            (CreateDevice(scPath: "path1"), CreateDevice(scPath: "path2")),
        };

        foreach (var (device1, device2) in testCases)
        {
            bool areEqual = device1.Equals(device2);
            int comparison = device1.CompareTo(device2);

            Assert.Equal(areEqual, comparison == 0); // CompareTo == 0 ⟺ Equals == true
        }
    }

    [Fact]
    public void CompareTo_AntiSymmetric()
    {
        var device1 = CreateDevice(serialNumber: 100);
        var device2 = CreateDevice(serialNumber: 200);

        int comparison1to2 = device1.CompareTo(device2);
        int comparison2to1 = device2.CompareTo(device1);

        Assert.Equal(-Math.Sign(comparison1to2), Math.Sign(comparison2to1));
    }

    [Fact]
    public void CompareTo_Transitive()
    {
        var device1 = CreateDevice(serialNumber: 100);
        var device2 = CreateDevice(serialNumber: 200);
        var device3 = CreateDevice(serialNumber: 300);

        Assert.True(device1.CompareTo(device2) < 0);
        Assert.True(device2.CompareTo(device3) < 0);
        Assert.True(device1.CompareTo(device3) < 0); // Transitivity
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(100, 200, -1)]
    [InlineData(200, 100, 1)]
    public void CompareTo_SerialNumberOrdering_Theory(
        int serial1,
        int serial2,
        int expectedSign)
    {
        var device1 = CreateDevice(serialNumber: serial1);
        var device2 = CreateDevice(serialNumber: serial2);

        var result = device1.CompareTo(device2);
        Assert.Equal(Math.Sign(expectedSign), Math.Sign(result));
    }

    [Fact]
    public void CompareTo_NullArgument_ThrowsException()
    {
        var device = CreateDevice(serialNumber: 123);

        Assert.Throws<ArgumentNullException>(() => device.CompareTo(null!));
    }

    [Fact]
    public void GetHashCode_EqualObjects_SameHashCode()
    {
        var device1 = CreateDevice(serialNumber: 123);
        var device2 = CreateDevice(serialNumber: 123);

        Assert.True(device1.Equals(device2));
        Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Consistency()
    {
        var device = CreateDevice(serialNumber: 123);

        int hash1 = device.GetHashCode();
        int hash2 = device.GetHashCode();
        int hash3 = device.GetHashCode();

        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void SortedSet_Behavior()
    {
        var device1 = CreateDevice(serialNumber: 300);
        var device2 = CreateDevice(serialNumber: 100);
        var device3 = CreateDevice(serialNumber: 200);

        var sortedSet = new SortedSet<YubiKeyDevice> { device1, device2, device3 };

        var ordered = sortedSet.ToArray();
        Assert.Equal(device2, ordered[0]); // Serial 100
        Assert.Equal(device3, ordered[1]); // Serial 200
        Assert.Equal(device1, ordered[2]); // Serial 300
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var device = CreateDevice(serialNumber: 123);

        Assert.False(device.Equals(null!));
        Assert.False(device.Equals((object)null!));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var device = CreateDevice(serialNumber: 123);
        var otherObject = "not a yubikey device";

        Assert.False(device.Equals(otherObject));
    }

    // Mock implementations
    private class MockSmartCardDevice : ISmartCardDevice
    {
        public string Path { get; }
        public string? ParentDeviceId { get; }
        public DateTime LastAccessed { get; }

        public MockSmartCardDevice(
            string path) => Path = path;

        public void Dispose() { }

        public bool IsNfcTransport() => false;

        // Minimal implementation of required members
        public AnswerToReset? Atr { get; }
        public SmartCardConnectionKind Kind { get; }

        public ISmartCardConnection Connect()
        {
            throw new NotImplementedException();
        }
    }

    private class MockHidFidoDevice : IHidDevice
    {
        public string Path { get; }
        public string? ParentDeviceId { get; }
        public DateTime LastAccessed { get; }

        public MockHidFidoDevice(
            string path) => Path = path;

        public void Dispose() { }
        public bool IsFido() => true;
        public bool IsKeyboard() => false;
        public short VendorId { get; }
        public short ProductId { get; }
        public short Usage { get; }
        public HidUsagePage UsagePage { get; }

        public IHidConnection ConnectToFeatureReports()
        {
            throw new NotImplementedException();
        }

        public IHidConnection ConnectToIOReports()
        {
            throw new NotImplementedException();
        }
    }

    private class MockHidKeyboardDevice : IHidDevice
    {
        public string Path { get; }
        public string? ParentDeviceId { get; }
        public DateTime LastAccessed { get; }

        public MockHidKeyboardDevice(
            string path) => Path = path;

        public void Dispose() { }
        public bool IsFido() => false;
        public bool IsKeyboard() => true;
        public short VendorId { get; }
        public short ProductId { get; }
        public short Usage { get; }
        public HidUsagePage UsagePage { get; }

        public IHidConnection ConnectToFeatureReports()
        {
            throw new NotImplementedException();
        }

        public IHidConnection ConnectToIOReports()
        {
            throw new NotImplementedException();
        }
    }
}
