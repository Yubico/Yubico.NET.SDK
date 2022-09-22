// Copyright 2021 Yubico AB
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
    /// This lists the possible actions or information the caller is requesting.
    /// </summary>
    /// <remarks>
    /// This is used in conjunction with the <see cref="KeyEntryData"/> class.
    /// When the SDK needs a key, PIN, password, or some other user-supplied
    /// secret element, it will call the application-supplied, key-collecting
    /// delegate. Inside the <c>KeyEntryData</c> class is a property indicating
    /// what the SDK is requesting the delegate to collect.
    /// <p>
    /// This enum is the list of possible elements the SDK can request of the
    /// delegate.
    /// </p>
    /// </remarks>
    public enum KeyEntryRequest
    {
        /// <summary>
        /// Indicates that the SDK has successfully used the element(s) requested
        /// and the caller can now release any resources related to obtaining the
        /// data.
        /// </summary>
        /// <remarks>
        /// Note that a delegate MUST NEVER throw an exception if the
        /// <c>Request</c> is <c>Release</c>. The <c>Release</c> is called from
        /// inside a <c>finally</c> block, and it is a bad idea to throw
        /// exceptions from inside <c>finally</c>.
        /// </remarks>
        Release = 0,

        /// <summary>
        /// Indicates that the SDK is requesting the PIV PIN to verify.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect one
        /// PIN and submit it using the <c>SubmitValue</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at verifying
        /// the PIN failed and the <c>KeyCollector</c> should try again to obtain
        /// the value (unless the user decides to cancel).
        /// </p>
        /// </remarks>
        VerifyPivPin = 1,

        /// <summary>
        /// Indicates that the SDK is requesting the current PIV PIN and a new
        /// PIN, in order to change the PIN from the current to the new. Collect
        /// both the current and a new PIN.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect two
        /// PINs and submit them using the <c>SubmitValues</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at changing
        /// the PIN failed and the <c>KeyCollector</c> should try again to obtain
        /// the values (unless the user decides to cancel).
        /// </p>
        /// <p>
        /// Note that the most likely reason a change will fail is because the
        /// current PIN was incorrect, but it can also fail if the new PIN is not
        /// valid (e.g. too short).
        /// </p>
        /// </remarks>
        ChangePivPin = 2,

        /// <summary>
        /// Indicates that the SDK is requesting the PIV PUK and a new PIN. This
        /// is the first call. This is used to recover the PIN using the PUK.
        /// Collect both the current PUK and a new PIN.
        /// </summary>
        /// <remarks>
        /// After collecting the PUK and a new PIN, submit them using the
        /// <c>SubmitValues</c> method, with the PUK as the <c>currentValue</c>
        /// and the PIN as the <c>newValue</c>.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at resetting
        /// the PIN failed and the <c>KeyCollector</c> should try again to obtain
        /// the values (unless the user decides to cancel).
        /// </p>
        /// <p>
        /// Note that the most likely reason a reset will fail is because the PUK
        /// was incorrect, but it can also fail if the new PIN is not valid (e.g.
        /// too short).
        /// </p>
        /// </remarks>
        ResetPivPinWithPuk = 3,

        /// <summary>
        /// Indicates that the SDK is requesting the current PIV PUK and a new
        /// PUK, in order to change the PUK from the current to the new. Collect
        /// both the current and a new PUK.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect two
        /// PUKs and submit them using the <c>SubmitValues</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at changing
        /// the PUK failed and the <c>KeyCollector</c> should try again to obtain
        /// the values (unless the user decides to cancel).
        /// </p>
        /// </remarks>
        ChangePivPuk = 4,

        /// <summary>
        /// Indicates that the SDK is requesting the current PIV management key
        /// in order to authenticate.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect one
        /// management key and submit it using the <c>SubmitValue</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at
        /// authenticating the management key failed and the <c>KeyCollector</c>
        /// should try again to obtain the value (unless the user decides to
        /// cancel).
        /// </p>
        /// </remarks>
        AuthenticatePivManagementKey = 5,

        /// <summary>
        /// Indicates that the SDK is requesting the current PIV management key
        /// and a new PIV management key, in order to change the key from the
        /// current to the new. Collect both the current and a new management key.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect two
        /// keys and submit them using the <c>SubmitValues</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at changing
        /// the management key failed and the <c>KeyCollector</c> should try
        /// again to obtain the values (unless the user decides to cancel).
        /// </p>
        /// <p>
        /// Note that the most likely reason a change will fail is because the
        /// current management key was incorrect, but it can also fail if the new
        /// management key is not valid (e.g. too short).
        /// </p>
        /// </remarks>
        ChangePivManagementKey = 6,

        /// <summary>
        /// Indicates that the SDK is requesting the OATH password to verify.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect one
        /// password and submit it using the <c>SubmitValue</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at verifying
        /// the password failed and the <c>KeyCollector</c> should try again to obtain
        /// the value (unless the user decides to cancel).
        /// </p>
        /// </remarks>
        VerifyOathPassword = 7,

        /// <summary>
        /// Indicates that the SDK is requesting a new password.
        /// Collect a new password.
        /// </summary>
        /// <remarks>
        /// When the <c>Request</c> is this value, the delegate should collect a password
        /// and submit it using the <c>SubmitValues</c> method.
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>false</c>, the
        /// <c>KeyEntryData</c> is making the initial request.
        /// </p>
        /// <p>
        /// When the <c>KeyEntryData.Request</c> is this value and the
        /// <c>KeyEntryData.IsRetry</c> property is <c>true</c>, the
        /// <c>KeyEntryData</c> is reporting that a previous attempt at setting
        /// the password failed and the <c>KeyCollector</c> should try again to obtain
        /// the values (unless the user decides to cancel).
        /// </p>
        /// </remarks>
        SetOathPassword = 8,

        /// <summary>
        /// The YubiKey is requesting touch for user presence verification.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the <c>Request</c> is this value, the delegate does not need to collect
        /// any passwords or keys. This is simply used as a means to alert the application
        /// that the YubiKey is awaiting a touch. Typically, you will want to respond to
        /// this request by alerting your user that they need to physically touch the
        /// YubiKey.
        /// </para>
        /// <para>
        /// In addition, when the SDK calls a KeyCollector with this request, it
        /// will ignore the return value. That is, it is not possible to cancel
        /// this request.
        /// </para>
        /// <para>
        /// Ideally, you should not block this call. However, to ensure the proper function
        /// of the SDK, this request will be issued on a separate thread from the one that
        /// originated this call.
        /// </para>
        /// </remarks>
        TouchRequest = 9,

        /// <summary>
        /// Indicates that the SDK is setting the FIDO U2F PIN. The YubiKey is
        /// not set with a U2F PIN yet, so collect only a new PIN.
        /// </summary>
        SetU2fPin = 10,

        /// <summary>
        /// Indicates that the SDK is requesting the current FIDO U2F PIN and a
        /// new PIN, in order to change the PIN from the current to the new.
        /// Collect both the current and a new PIN.
        /// </summary>
        ChangeU2fPin = 11,

        /// <summary>
        /// Indicates that the SDK is verifying the FIDO U2F PIN. Collect the
        /// current PIN.
        /// </summary>
        VerifyU2fPin = 12,

        /// <summary>
        /// Indicates that the SDK is setting the FIDO2 PIN. The YubiKey is
        /// not set with a FIDO2 PIN yet, so only collect a new PIN.
        /// </summary>
        SetFido2Pin = 13,

        /// <summary>
        /// Indicates that the SDK is requesting the current FIDO2 PIN and a
        /// new PIN, in order to change the PIN from the current to the new.
        /// Collect both the current and new PINs.
        /// </summary>
        ChangeFido2Pin = 14,

        /// <summary>
        /// Indicates that the SDK is verifying the FIDO2 PIN. Collect the
        /// current PIN.
        /// </summary>
        VerifyFido2Pin = 15,
    }
}
