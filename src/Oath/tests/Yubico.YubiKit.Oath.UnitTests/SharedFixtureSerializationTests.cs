using Xunit.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Oath.UnitTests;

public class SharedFixtureSerializationTests
{
    [Fact]
    public void Placeholder_RoundTripsUnsignedCapabilityFiltersWithoutBindingDevice()
    {
        const uint enabled = 0x80000010u;
        const uint fipsCapable = 0x80000200u;
        const uint fipsApproved = 0x80000100u;
        var original = YubiKeyTestState.CreateFilteredPlaceholder(new FilterCriteria
        {
            Capability = (DeviceCapabilities)enabled,
            FipsCapable = (DeviceCapabilities)fipsCapable,
            FipsApproved = (DeviceCapabilities)fipsApproved,
        });
        var serialized = new FixtureSerializationInfo();
        original.Serialize(serialized);

        var restored = new YubiKeyTestState();
        restored.Deserialize(serialized);
        var roundTrip = new FixtureSerializationInfo();
        restored.Serialize(roundTrip);

        Assert.True(restored.IsPlaceholder);
        Assert.Equal(enabled, roundTrip.GetValue<uint>("FilterCapability"));
        Assert.Equal(fipsCapable, roundTrip.GetValue<uint>("FilterFipsCapable"));
        Assert.Equal(fipsApproved, roundTrip.GetValue<uint>("FilterFipsApproved"));
    }

    private sealed class FixtureSerializationInfo : IXunitSerializationInfo
    {
        private readonly Dictionary<string, object?> _values = [];

        public void AddValue(string key, object? value, Type? type = null) => _values.Add(key, value);

        public T? GetValue<T>(string key) => (T?)_values[key];

        public object? GetValue(string key, Type type) => _values[key];
    }
}