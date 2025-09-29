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

using System.Threading.Channels;

namespace Yubico.YubiKit.Core.Devices;

public interface IDeviceChannel
{
    Task PublishAsync(IEnumerable<IYubiKey> devices, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IEnumerable<IYubiKey>> ConsumeAsync(CancellationToken cancellationToken = default);
    void Complete();
}

public class DeviceChannel : IDeviceChannel, IDisposable
{
    private readonly Channel<IEnumerable<IYubiKey>> _channel = Channel.CreateUnbounded<IEnumerable<IYubiKey>>();
    private bool _disposed = false;

    public async Task PublishAsync(IEnumerable<IYubiKey> devices, CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        try
        {
            await _channel.Writer.WriteAsync(devices, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Writer is completed - ignore
        }
    }

    public IAsyncEnumerable<IEnumerable<IYubiKey>> ConsumeAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        if (!_disposed)
        {
            _channel.Writer.TryComplete();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Complete();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}