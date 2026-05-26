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

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Yubico.YubiKey;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    internal sealed class AuthenticatorSelectionCoordinator
    {
        private readonly object _lockObject = new object();
        private readonly Dictionary<IYubiKeyDevice, SignalUserCancel> _cancelDelegates =
            new Dictionary<IYubiKeyDevice, SignalUserCancel>(DeviceReferenceComparer.Instance);
        private readonly HashSet<IYubiKeyDevice> _cancelSignaled =
            new HashSet<IYubiKeyDevice>(DeviceReferenceComparer.Instance);

        private IYubiKeyDevice? _selectedDevice;

        public bool HasWinner
        {
            get
            {
                lock (_lockObject)
                {
                    return _selectedDevice is not null;
                }
            }
        }

        public IYubiKeyDevice? SelectedDevice
        {
            get
            {
                lock (_lockObject)
                {
                    return _selectedDevice;
                }
            }
        }

        public void CaptureCancel(IYubiKeyDevice device, SignalUserCancel signalUserCancel)
        {
            if (device is null || signalUserCancel is null)
            {
                return;
            }

            bool cancelNow = false;
            lock (_lockObject)
            {
                if (_selectedDevice is null)
                {
                    if (!_cancelDelegates.ContainsKey(device))
                    {
                        _cancelDelegates.Add(device, signalUserCancel);
                    }
                }
                else if (!ReferenceEquals(device, _selectedDevice) && _cancelSignaled.Add(device))
                {
                    cancelNow = true;
                }
            }

            if (cancelNow)
            {
                signalUserCancel();
            }
        }

        public bool TrySelectWinner(IYubiKeyDevice device)
        {
            List<SignalUserCancel> loserCancels = new List<SignalUserCancel>();
            lock (_lockObject)
            {
                if (_selectedDevice is not null)
                {
                    return false;
                }

                _selectedDevice = device;
                AddLoserCancels(loserCancels);
            }

            InvokeCancels(loserCancels);
            return true;
        }

        public void CancelLosers()
        {
            List<SignalUserCancel> loserCancels = new List<SignalUserCancel>();
            lock (_lockObject)
            {
                if (_selectedDevice is null)
                {
                    return;
                }

                AddLoserCancels(loserCancels);
            }

            InvokeCancels(loserCancels);
        }

        public bool IsExpectedLoserCancellation(IYubiKeyDevice device)
        {
            lock (_lockObject)
            {
                return _selectedDevice is not null && !ReferenceEquals(device, _selectedDevice);
            }
        }

        private void AddLoserCancels(List<SignalUserCancel> loserCancels)
        {
            foreach (KeyValuePair<IYubiKeyDevice, SignalUserCancel> current in _cancelDelegates)
            {
                if (!ReferenceEquals(current.Key, _selectedDevice) && _cancelSignaled.Add(current.Key))
                {
                    loserCancels.Add(current.Value);
                }
            }
        }

        private static void InvokeCancels(List<SignalUserCancel> loserCancels)
        {
            foreach (SignalUserCancel current in loserCancels)
            {
                current();
            }
        }

        private sealed class DeviceReferenceComparer : IEqualityComparer<IYubiKeyDevice>
        {
            internal static readonly DeviceReferenceComparer Instance = new DeviceReferenceComparer();

            public bool Equals(IYubiKeyDevice? x, IYubiKeyDevice? y) => ReferenceEquals(x, y);

            public int GetHashCode(IYubiKeyDevice obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
