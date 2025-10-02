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

namespace Yubico.YubiKit.Core.YubiKey;

public abstract class ApplicationSession : IApplicationSession
{
    #region IApplicationSession Members

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature)) throw new NotSupportedException($"{feature.Name} is not supported on this YubiKey.");
    }

    public bool IsSupported(Feature feature) =>
        true; // TODO get from Management Session, select, and parse version info

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO release managed resources here
        }
    }
}

public interface IApplicationSession : IDisposable
{
    bool IsSupported(Feature feature);
    void EnsureSupports(Feature feature);
}