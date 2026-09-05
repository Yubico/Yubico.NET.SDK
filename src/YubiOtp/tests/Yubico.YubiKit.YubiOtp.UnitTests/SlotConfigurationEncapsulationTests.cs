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

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class SlotConfigurationEncapsulationTests
{
    [Theory]
    [InlineData("_fixed")]
    [InlineData("_uid")]
    [InlineData("_key")]
    [InlineData("_fixedSize")]
    public void BufferStorage_IsPrivate(string fieldName)
    {
        FieldInfo field = GetField(fieldName);

        Assert.True(field.IsPrivate);
    }

    [Fact]
    public void SetFixed_WhenValueIsTooLong_ThrowsArgumentException()
    {
        using var configuration = new TestSlotConfiguration();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.WriteFixed(new byte[YubiOtpConstants.FixedSize + 1]));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains($"at most {YubiOtpConstants.FixedSize} bytes", exception.Message);
    }

    [Fact]
    public void SetUid_WhenValueHasWrongLength_ThrowsArgumentException()
    {
        using var configuration = new TestSlotConfiguration();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.WriteUid(new byte[YubiOtpConstants.UidSize - 1]));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains($"exactly {YubiOtpConstants.UidSize} bytes", exception.Message);
    }

    [Fact]
    public void SetKey_WhenValueHasWrongLength_ThrowsArgumentException()
    {
        using var configuration = new TestSlotConfiguration();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => configuration.WriteKey(new byte[YubiOtpConstants.KeySize - 1]));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains($"exactly {YubiOtpConstants.KeySize} bytes", exception.Message);
    }

    [Fact]
    public void Dispose_ZeroesAllConfigurationBuffers()
    {
        var configuration = new TestSlotConfiguration();
        configuration.WriteFixed(CreateFilledBuffer(YubiOtpConstants.FixedSize));
        configuration.WriteUid(CreateFilledBuffer(YubiOtpConstants.UidSize));
        configuration.WriteKey(CreateFilledBuffer(YubiOtpConstants.KeySize));

        byte[] fixedBuffer = GetBuffer(configuration, "_fixed");
        byte[] uidBuffer = GetBuffer(configuration, "_uid");
        byte[] keyBuffer = GetBuffer(configuration, "_key");

        configuration.Dispose();

        Assert.All(fixedBuffer, value => Assert.Equal(0, value));
        Assert.All(uidBuffer, value => Assert.Equal(0, value));
        Assert.All(keyBuffer, value => Assert.Equal(0, value));
    }

    private static FieldInfo GetField(string fieldName) =>
        typeof(SlotConfiguration).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"SlotConfiguration field '{fieldName}' was not found.");

    private static byte[] GetBuffer(SlotConfiguration configuration, string fieldName) =>
        Assert.IsType<byte[]>(GetField(fieldName).GetValue(configuration));

    private static byte[] CreateFilledBuffer(int length)
    {
        var buffer = new byte[length];
        buffer.AsSpan().Fill(0xA5);
        return buffer;
    }

    private sealed class TestSlotConfiguration : SlotConfiguration
    {
        public void WriteFixed(ReadOnlySpan<byte> value) => SetFixed(value);

        public void WriteUid(ReadOnlySpan<byte> value) => SetUid(value);

        public void WriteKey(ReadOnlySpan<byte> value) => SetKey(value);
    }
}