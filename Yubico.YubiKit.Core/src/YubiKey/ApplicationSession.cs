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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

public abstract class ApplicationSession : IApplicationSession
{
    protected ILogger Logger { get; }
    protected IProtocol? Protocol { get; set; }

    public FirmwareVersion FirmwareVersion { get; protected set; } = new();
    public bool IsInitialized { get; protected set; }
    public bool IsAuthenticated { get; protected set; }

    protected ApplicationSession()
    {
        Logger = YubiKitLogging.CreateLogger(GetType().FullName ?? GetType().Name);
    }

    public bool IsSupported(Feature feature) => FirmwareVersion >= feature.Version;

    public void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Protocol?.Dispose();
            Protocol = null;
        }
    }
}


