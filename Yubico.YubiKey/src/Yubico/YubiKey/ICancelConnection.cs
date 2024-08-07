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

namespace Yubico.YubiKey
{
    /// <summary>
    /// This delegate defines the signature of a method that can be called to
    /// determine if an operation should cancel. The caller supplies a
    /// commandByte and the Query responds with <c>true</c> (the command should be
    /// canceled), or <c>false</c> (there is no current request that the command
    /// be canceled).
    /// </summary>
    /// <param name="commandByte">
    /// The command the caller is executing.
    /// </param>
    /// <returns>
    /// A boolean, indicating whether the Query call is calling for the command
    /// to be canceled or not.
    /// </returns>
    public delegate bool QueryCancel(byte commandByte);

    /// <summary>
    /// IYubiKeyConnection classes that implement this interface can cancel
    /// operations "mid-command".
    /// </summary>
    /// <remarks>
    /// If a class that implements the IYubiKeyConnection interface also
    /// implements this interface, then it is possible to communicate a cancel.
    /// That is, it is possible an for the Connection to determine if it should
    /// cancel a command.
    /// <para>
    /// Generally in the SDK, commands are called using a Connection. For
    /// example, a Session object (PivSession, Fido2Session, etc.), will contain a
    /// Connection property. Calling a command would look something like this:
    /// <code language="csharp">
    ///     var cmd = new SomeOperationCommand();
    ///     SomeOperationResponse rsp = session.Connection.SendCommand(cmd);
    /// </code>
    /// If the Connection implements this interface, it is possible to set it
    /// with a QueryCancel delegate. The Connection object (or some object that
    /// the Connection creates) will call this delegate on a regular basis to
    /// determine if the operation has been canceled.
    /// </para>
    /// <para>
    /// We think of a call to the YubiKey as synchronous, meaning the caller must
    /// wait until the YubiKey completes the operation. However, depending on the
    /// communication protocol, it is possible that sending a command into the
    /// YubiKey involves sending the initial command and then polling the YubiKey
    /// to determine if it is still working.
    /// </para>
    /// <para>
    /// For example, with FIDO2, a command is sent using a "SetReport" call and
    /// then polling with a "GetReport" call (a while loop). When it gets a
    /// "GetReport" call, the YubiKey responds with either "KeepAlive" or the
    /// result of the operation. If "KeepAlive", poll again (run through the loop
    /// again).
    /// </para>
    /// <para>
    /// If a Fido2 Connection object has been set with a QueryCancel, then during
    /// the loop that performs the polling, it can also call the QueryCancel. If
    /// QueryCancel is null, there's nothing to check. But if not null, it can
    /// check to see if the operation should be canceled. If so, it can send a
    /// new "SetReport" indicating cancel. If not, just call "GetReport" and run
    /// through the loop again.
    /// </para>
    /// <para>
    /// So who is calling the LoadQueryCancel? It is almost certainly going to be
    /// an object the SDK builds to call a KeyCollector on another thread. In
    /// fact, it will likely only ever be used by the
    /// Yubico.YubiKey.TouchFingerprintTask class.
    /// </para>
    /// <para>
    /// For example, suppose the SDK begins an operation that requires touch to
    /// complete. It will contact the KeyCollector to let it know (which will in
    /// turn let the end user know) that touch is needed. This will need to be on
    /// a separate thread so that the call to the YubiKey is not blocked. Hence,
    /// it will use the TouchFingerprintTask class.
    /// </para>
    /// <para>
    /// The KeyCollector might want to offer the opportunity for the user to
    /// cancel. If so, there needs to be a way for that cancel to make its way
    /// from the user to the Connection object that is in contact with the
    /// YubiKey. The TouchFingerprintTask class is in contact with the
    /// KeyCollector (and hence user), so the KeyCollector can pass on the cancel
    /// message. The QueryCancel delegate is how the TouchFingerprint object can
    /// be in contact with the Connection.
    /// </para>
    /// <para>
    /// When the operation is complete, it is the responsibility of the entity
    /// that loaded the QueryCancel to unload it (call the LoadQueryCancel with
    /// null). This will likely be done during Release. That is, when the SDK is
    /// done with an operation, it will call the KeyCollector with Release. At
    /// that point, the entity that originally loaded the QueryCancel
    /// (TouchFingerprintTask object) knows to unload.
    /// </para>
    /// <para>
    /// Note that it would be possible for the Connection to supply a delegate to
    /// the TouchFingerprint object. In that way, when the user cancels, the
    /// KeyCollector contacts the TouchFingerprintTask, which then calls the
    /// Connection delegate (callback) to indicate cancel. However, that system
    /// proved problematic. There were too many race conditions and too many
    /// cases where it was not possible to accurately manage the lifetime of the
    /// Cancel callback. The main problem was that the TouchFingerprintTask
    /// object along with the KeyCollector was where the Cancel logic (including
    /// lifetime) was located. The TouchFingerprintTask needed to send
    /// information to the Connection so that it could replicate the logic. But
    /// that wasn't enough, there needed to be updates to the info sent so that
    /// the Connection could accurately execute the logic.
    /// </para>
    /// <para>
    /// Hence, it makes more sense for the Connection to call the entity that is
    /// executing the cancel logic. The Connection does not need to know the
    /// logic, it just has to call to get the latest state of that logic.
    /// </para>
    /// </remarks>
    internal interface ICancelConnection
    {
        /// <summary>
        /// Allows the caller to load a <see cref="QueryCancel"/> delegate onto
        /// this object. If the input is null, this "unloads" the delegate.
        /// </summary>
        /// <param name="queryCancel">
        /// The delegate this object should load.
        /// </param>
        /// <returns>
        /// A boolean, <c>true</c> if the delegate is loaded, <c>false</c>
        /// otherwise.
        /// </returns>
        public bool LoadQueryCancel(QueryCancel? queryCancel);
    }
}
