// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
///     Constructor and disposal lifetime of the macOS feature-report connection.
/// </summary>
public class MacOSHidConnectionLifetimeTests
{
    private const long EntryId = 4242;

    [Fact]
    public void FeatureReportConnection_WhenOpenThrows_ReleasesTheDevice()
    {
        var lifetime = new RecordingLifetime { ThrowOnOpen = true };

        _ = Assert.Throws<PlatformApiException>(() => new MacOSHidFeatureReportConnection(EntryId, lifetime));

        Assert.Contains(lifetime.CreatedDevice, lifetime.Released);
        Assert.Empty(lifetime.Closed); // A non-exclusive failed open never established an open device.
    }

    [Fact]
    public void FeatureReportConnection_WhenDisposed_ReleasesTheDevice()
    {
        var lifetime = new RecordingLifetime();
        var connection = new MacOSHidFeatureReportConnection(EntryId, lifetime);

        connection.Dispose();

        Assert.Contains(lifetime.CreatedDevice, lifetime.Closed);
        Assert.Contains(lifetime.CreatedDevice, lifetime.Released);
    }

    private sealed class RecordingLifetime : IIOKitDeviceLifetime
    {
        private nint _nextHandle = 0x1000;

        public bool ThrowOnCreate { get; init; }
        public bool ThrowOnOpen { get; init; }

        public nint CreatedDevice { get; private set; }

        public List<nint> Released { get; } = [];
        public List<nint> Closed { get; } = [];

        public nint CreateDevice(long entryId)
        {
            if (ThrowOnCreate) throw new PlatformApiException("simulated device creation failure");

            CreatedDevice = _nextHandle++;
            return CreatedDevice;
        }

        public void OpenDevice(nint device)
        {
            if (ThrowOnOpen) throw new PlatformApiException("simulated device open failure");
        }

        public int OpenDeviceResult(nint device) => ThrowOnOpen ? -1 : 0;

        public void CloseDevice(nint device) => Closed.Add(device);

        public bool CloseDeviceChecked(nint device) { CloseDevice(device); return true; }

        public int GetFeatureReport(nint device, byte[] buffer, ref long length) => throw new NotSupportedException();

        public int SetFeatureReport(nint device, byte[] buffer) => throw new NotSupportedException();

        public void ReleaseCFObject(nint cfObject) => Released.Add(cfObject);

        public nint CreateRunLoopMode(string name) => throw new NotSupportedException();

        public int GetIntProperty(nint device, string propertyName) => 64;

        public void RegisterInputReportCallback(
            nint device,
            byte[] buffer,
            int bufferLength,
            nint callback,
            nint context)
        { }

        public void RegisterRemovalCallback(nint device, nint callback, nint context)
        { }
    }
}
