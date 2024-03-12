// Copyright 2023 Yubico AB
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
using System.Threading;
using System.Threading.Tasks;

namespace Yubico.YubiKey
{
    // Use this class to allow the caller to cancel a touch or fingerprint
    // operation, and to communicate to the caller that is is time to Release.
    // We are going to call the KeyCollector indicating that the user needs to
    // touch or supply a fingerprint. We run these calls on a separate thread so
    // that the YubiKey commands are not blocked.
    // The SDK will create an instance of this class, which creates a new thread
    // that calls the KeyCollector.
    // While the SDK is performing its operation, it can calls SdkUpdate with the
    // KeyEntryRequest it wants the sub-thread to use, and, if necessary, a new
    // BioEnrollSampleResult object.
    // At that point, the Task created will call the KeyCollector again. If the
    // updated Request is Release, the sub-thread will make the appropriate call
    // and then quit.
    internal class TouchFingerprintTask : IDisposable
    {
        // This is the command for which we are creating the task.
        private readonly byte _commandByte;

        private readonly ICancelConnection? _connection;
        private readonly Task _notifyTask;
        private bool _disposed;

        // This field holds a boolean that indicates whether the sub-thread
        // should call the KeyCollector again.
        // If false, keep waiting.
        // If true, then call the KeyCollector with the _keyEntryData.
        // Note that two threads will have access to this property. The intention
        // is that the main thread (the one on which the SDK is performing the
        // operation) will write and the "sub-thread" that contacts the
        // KeyCollector will read and write.
        // Hence, there is a lock.
        private bool _isSdkUpdate = true;
        private readonly object _updateLock = new object();

        // The SDK will call SdkUpdate, which will create a new KeyEntryData and
        // copy the old into the new.
        // The sub-thread will use the new object.
        // We're using different KeyEntryData objects, one for each call to the
        // KeyCollector, to avoid collisions.
        private KeyEntryData _keyEntryData;

        // If there is no SignalCancel call, this is how we report cancellation.
        // That is, set this to true so that we have a second way to notify the
        // caller that the operation is canceled.
        // Note that we won't set this property if there is a SignalCancel, even
        // if the caller requests cancellation.
        // Note that two threads will have access to this property. However, the
        // main thread (the one on which the SDK is performing the operation)
        // will read only. The "sub-thread" that contacts the KeyCollector will
        // write to this property if there's an exception.
        // So while there is a "race condition", it is not dangerous.
        public bool IsUserCanceled { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private TouchFingerprintTask()
        {
            throw new NotImplementedException();
        }

        // Build a new instance of this class.
        // This will create a thread that will make the call to the KeyCollector
        // with the Request of Touch or Fingerprint. Then back on the main thread
        // we will call on the YubiKey to do the work that will need the touch or
        // fingerprint (the regular operation). In this way, the call to the
        // KeyCollector won't block the regular operation.
        // The YubiKey operation running on the main thread can call SdkUpdate to
        // tell this object to either make another call to the KeyCollector or to
        // cancel.
        // Notice that this constructor includes an input keyEntryData. That is,
        // it is the responsibility of the caller to make sure that
        // keyEntryData.Request is set to the appropriate initial value.
        public TouchFingerprintTask(
            Func<KeyEntryData, bool> keyCollector,
            KeyEntryData keyEntryData,
            IYubiKeyConnection connection,
            byte commandByte)
        {
            _commandByte = commandByte;
            _connection = null;
            IsUserCanceled = false;
            _isSdkUpdate = true;
            _disposed = false;

            // This is the first call for touch or fingerprints, so there will be
            // nothing other than Request we need to copy over.
            _keyEntryData = new KeyEntryData(UserCancel)
            {
                Request = keyEntryData.Request
            };
            _notifyTask = new Task(() => RunKeyCollectorTask(keyCollector));
            _ = _notifyTask.ContinueWith((t) => HandleTaskException(t), TaskScheduler.Current);

            if (connection is ICancelConnection cancelConnection)
            {
                if (cancelConnection.LoadQueryCancel(IsCanceled))
                {
                    _connection = cancelConnection;
                }
            }

            if (_connection is null)
            {
                _notifyTask.Start();
            }
        }

        private void HandleTaskException(Task task) =>
            task.Exception?.Handle((e) => { UserCancel(); return true; });

        // If the caller is performing the same command this TouchFingerprintTask
        // object is concerned with, then return the IsUserCanceled.
        public bool IsCanceled(byte commandByte)
        {
            if (commandByte == _commandByte)
            {
                if (_notifyTask.Status == TaskStatus.Created)
                {
                    _notifyTask.Start();
                }

                return IsUserCanceled;
            }

            return false;
        }

        // To be called from the main thread, the thread the SDK is on while
        // performing the operations that require touch or fingerprints.
        // The SDK will call this method to signal that the KeyCollector
        // needs to be called again, or that it has completed the operation that
        // required touch or fingerprint.
        // The keyEntryData.Request should be either EnrollFingerprint or Release.
        // Any other value entered will be considered Release.
        public void SdkUpdate(KeyEntryData keyEntryData)
        {
            KeyEntryRequest request = keyEntryData.Request == KeyEntryRequest.EnrollFingerprint
                ? KeyEntryRequest.EnrollFingerprint : KeyEntryRequest.Release;

            lock (_updateLock)
            {
                _isSdkUpdate = true;
                _keyEntryData = new KeyEntryData(UserCancel)
                {
                    Request = request,
                    RetriesRemaining = keyEntryData.RetriesRemaining,
                    IsRetry = keyEntryData.IsRetry,
                    LastBioEnrollSampleResult = keyEntryData.LastBioEnrollSampleResult
                };
            }

            if (request == KeyEntryRequest.Release && !(_connection is null))
            {
                _ = _connection.LoadQueryCancel(null);
            }
        }

        private void UserCancel() => IsUserCanceled = true;

        private void RunKeyCollectorTask(Func<KeyEntryData, bool> keyCollector)
        {
            bool makeCall;
            bool isRelease;
            KeyEntryData keyEntryData;
            do
            {
                lock (_updateLock)
                {
                    makeCall = _isSdkUpdate;
                    keyEntryData = _keyEntryData;
                    isRelease = keyEntryData.Request == KeyEntryRequest.Release;
                    _isSdkUpdate = false;
                }

                if (makeCall)
                {
                    _ = keyCollector(keyEntryData);
                }
                else
                {
                    Thread.Sleep(250);
                }
            } while (!isRelease);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Microsoft says there's no need to dispose Tasks. However, the
                // compiler won't compile this code unless we do.
                // But an undocumented feature of Task is that one cannot Dispose
                // it unless the state is RanToCompletion, Faulted, or Canceled.
                switch (_notifyTask.Status)
                {
                    default:
                        break;

                    case TaskStatus.RanToCompletion:
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        _notifyTask.Dispose();
                        break;
                }

                _disposed = true;
            }
        }
    }
}
