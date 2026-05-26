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
using Yubico.YubiKey;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    internal sealed class AuthenticatorSelectionCoordinator
    {
        private readonly object _lockObject = new object();
        private readonly List<(IYubiKeyDevice Device, SignalUserCancel Cancel)> _cancelRegistrations =
            new List<(IYubiKeyDevice Device, SignalUserCancel Cancel)>();

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

        public void CaptureCancel(IYubiKeyDevice device, SignalUserCancel? signalUserCancel)
        {
            if (signalUserCancel is null)
            {
                return;
            }

            bool cancelNow = false;
            lock (_lockObject)
            {
                if (_selectedDevice is null)
                {
                    if (!HasCancelRegistration(device))
                    {
                        _cancelRegistrations.Add((device, signalUserCancel));
                    }
                }
                else if (!ReferenceEquals(device, _selectedDevice))
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
            SignalUserCancel[] loserCancels;
            lock (_lockObject)
            {
                if (_selectedDevice is not null)
                {
                    return false;
                }

                _selectedDevice = device;
                loserCancels = TakeLoserCancels();
            }

            InvokeCancels(loserCancels);
            return true;
        }

        public void CancelLosers()
        {
            SignalUserCancel[] loserCancels;
            lock (_lockObject)
            {
                if (_selectedDevice is null)
                {
                    return;
                }

                loserCancels = TakeLoserCancels();
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

        private bool HasCancelRegistration(IYubiKeyDevice device)
        {
            foreach ((IYubiKeyDevice currentDevice, _) in _cancelRegistrations)
            {
                if (ReferenceEquals(currentDevice, device))
                {
                    return true;
                }
            }

            return false;
        }

        private SignalUserCancel[] TakeLoserCancels()
        {
            var loserCancels = new List<SignalUserCancel>();
            for (int index = _cancelRegistrations.Count - 1; index >= 0; index--)
            {
                (IYubiKeyDevice currentDevice, SignalUserCancel currentCancel) = _cancelRegistrations[index];
                if (!ReferenceEquals(currentDevice, _selectedDevice))
                {
                    loserCancels.Add(currentCancel);
                    _cancelRegistrations.RemoveAt(index);
                }
            }

            return loserCancels.ToArray();
        }

        private static void InvokeCancels(SignalUserCancel[] loserCancels)
        {
            foreach (SignalUserCancel current in loserCancels)
            {
                current();
            }
        }
    }
}
