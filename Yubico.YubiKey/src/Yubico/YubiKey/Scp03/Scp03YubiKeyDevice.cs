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

using System;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey;

[Obsolete("This class is obsolete and will be removed in a future release.")]
internal class Scp03YubiKeyDevice : YubiKeyDevice
{
    public Scp03YubiKeyDevice(
        YubiKeyDevice device,
        StaticKeys staticKeys)
        : base(
            device.GetSmartCardDevice(),
            null,
            null,
            device)
    {
        StaticKeys = staticKeys.GetCopy();
    }

    public StaticKeys StaticKeys { get; }

    [Obsolete("Obsolete")]
    internal override IYubiKeyConnection? Connect(
        YubiKeyApplication? application,
        byte[]? applicationId,
        StaticKeys? scp03Keys)
    {
        if (!HasSmartCard)
        {
            return null;
        }

        if (scp03Keys != null && !StaticKeys.AreKeysSame(scp03Keys))
        {
            return null;
        }

        if (application is not null)
        {
            return new Scp03Connection(GetSmartCardDevice(), (YubiKeyApplication)application, StaticKeys);
        }

        if (applicationId is not null)
        {
            return new Scp03Connection(GetSmartCardDevice(), applicationId, StaticKeys);
        }

        return null;
    }
}
