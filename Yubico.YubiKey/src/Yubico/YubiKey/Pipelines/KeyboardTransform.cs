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

using System;
using System.Globalization;
using System.Threading;
using Yubico.YubiKey.Otp;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;
using System.Diagnostics;
using Yubico.Core.Logging;

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// Represents an ApduPipeline backed by a direct connection to the
    /// YubiOTP application.
    /// </summary>
    internal class KeyboardTransform : IApduTransform
    {
        private readonly IHidConnection _hidConnection;

        private readonly Logger _log = Log.GetLogger();

        /// <summary>
        /// An event which is fired if the YubiKey indicates it is waiting for touch. Event handlers
        /// must return as quickly as possible. If there are longer running tasks which are triggered
        /// as a result of this event, they should be run on a separate thread.
        /// </summary>
        public event EventHandler<EventArgs>? TouchPending;

        public const byte ConfigInstruction = 0x01;

        public KeyboardTransform(IHidConnection hidConnection)
        {
            _hidConnection = hidConnection;
        }

        /// <summary>
        /// Sets up the pipeline; should be called only once, before any `Invoke` calls.
        /// </summary>
        public void Setup()
        {

        }

        /// <summary>
        /// Passes the supplied command into the pipeline, and returns the final response.
        /// </summary>
        /// <remarks>
        /// The HID Keyboard interface predates the CCID-style APDU interface. While most commands
        /// have been built around sending a slot request, the original "status" command remains a
        /// separate entity. Unfortunately that means there must be some information leakage about
        /// the command definitions themselves in the implementation of this keyboard connection class.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        /// A <see cref="CommandApdu"/> with an unexpected <see cref="CommandApdu.Ins"/> member was used.
        /// </exception>
        public ResponseApdu Invoke(CommandApdu commandApdu, Type commandType, Type responseType)
        {
            var frameReader = new KeyboardFrameReader();

            switch (commandApdu.Ins)
            {
                case Otp.OtpConstants.ReadStatusInstruction:
                    _log.LogInformation("Reading the OTP status.");
                    frameReader.AddStatusReport(new KeyboardReport(_hidConnection.GetReport()));
                    break;
                case Otp.OtpConstants.RequestSlotInstruction:
                    bool configInstruction = responseType.IsAssignableFrom(typeof(Otp.Commands.ReadStatusResponse));
                    _log.LogInformation($"Handling an OTP slot request {commandApdu.P1}. Configuring = {configInstruction}");
                    HandleSlotRequestInstruction(commandApdu, frameReader, configInstruction);
                    break;
                default:
                    throw new NotSupportedException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidKeyboardInstruction,
                            commandApdu.Ins));
            }

            return new ResponseApdu(frameReader.GetData(), SWConstants.Success);
        }

        /// <summary>
        /// Cleans up the pipeline; should be called only once, after all `Invoke` calls.
        /// </summary>
        public void Cleanup()
        {

        }

        /// <summary>
        /// Sends a command request to the YubiKey. This is done by making a request to a certain "slot"
        /// in the OTP application.
        /// </summary>
        /// <param name="apdu">
        /// The meta-apdu representation of the command we're trying to send.
        /// </param>
        /// <param name="frameReader">
        /// A frameReader instance to receive the data read from the YubiKey.
        /// </param>
        /// <param name="configInstruction">
        /// A boolean flag to indicate whether this is a command that sends configuration data, or
        /// a command that exercises certain non-NVRAM altering functionality.
        /// </param>
        private void HandleSlotRequestInstruction(CommandApdu apdu, KeyboardFrameReader frameReader, bool configInstruction)
        {
            KeyboardReport? report = null;
            foreach (KeyboardReport featureReport in apdu.GetHidReports())
            {
                _log.LogInformation("Wait for write pending...");

                report = WaitForWriteResponse();

                _log.LogInformation("Got write response [{Report}]", report);
                _log.SensitiveLogInformation("Sending report [{Report}]", featureReport);

                _hidConnection.SetReport(featureReport.ToArray());
            }

            if (report is null)
            {
                throw new KeyboardConnectionException(ExceptionMessages.KeyboardNoReply);
            }

            if (!configInstruction)
            {
                // This is the point we will also detect touch.
                _log.LogInformation("Wait for read pending...");
                report = WaitForReadPending();

                do
                {
                    _log.SensitiveLogInformation("Adding feature report [{Report}]", report);
                    if (!frameReader.TryAddFeatureReport(report))
                    {
                        ResetReadMode();
                        if (!frameReader.IsEndOfReadChain)
                        {
                            throw new KeyboardConnectionException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.KeyboardUnexpectedEndOfBuffer,
                                    frameReader.UnexpectedEOR,
                                    frameReader.IsEndOfReadChain));
                        }
                        break;
                    }

                    report = new KeyboardReport(_hidConnection.GetReport());

                } while (true);
            }
            else
            {
                // Make sure the last write finished and the command is done.
                report = WaitForWriteResponse();
                frameReader.AddStatusReport(report);
            }
        }

        /// <summary>
        /// Polls the keyboard device to wait for the WritePending flag to clear.
        /// </summary>
        /// <returns>
        /// A report with the WritePending flag cleared. Once in this state, the YubiKey is ready
        /// to receive more data, or to wait for read operations.
        /// </returns>
        private KeyboardReport WaitForWriteResponse() =>
            WaitFor(
                r => !r.WritePending,
                checkForTouch: false,
                shortTimeout: true,
                ExceptionMessages.KeyboardTimeout);

        /// <summary>
        /// Polls the keyboard device to wait for the ReadPending flag to be present.
        /// </summary>
        /// <returns><see cref="KeyboardReport"/> from the YubiKey.</returns>
        private KeyboardReport WaitForReadPending() =>
            WaitFor(
                r => r.ReadPending,
                checkForTouch: true,
                shortTimeout: true,
                ExceptionMessages.KeyboardTimeout);

        private KeyboardReport WaitFor(
            Func<KeyboardReport, bool> stopCondition,
            bool checkForTouch,
            bool shortTimeout,
            string timeoutMessage)
        {
            // When waiting for touch, the YubiKey times out after 15 seconds.
            // Once that happens, the error message is the same as all error
            // messages. In order to present the user a more relevant message,
            // we will use a timer instead of a retry count, and we will bail
            // after 14 seconds.
            // In the previous code, the short timeout used a retry count. It
            // would start with a 1ms sleep time, and double it each retry for
            // ten retries. This winds up being 1023ms. We will keep the sleep
            // doubling logic, but just timeout after 1023ms.
            int timeLimitMs = shortTimeout ? 1023 : 14000;
            int sleepDurationMs = shortTimeout ? 1 : 250;
            int growthFactor = shortTimeout ? 2 : 1;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeLimitMs)
            {
                Thread.Sleep(sleepDurationMs);
                sleepDurationMs *= growthFactor;

                var report = new KeyboardReport(_hidConnection.GetReport());
                _log.SensitiveLogInformation("Received report [{Report}]", report);

                if (checkForTouch && report.TouchPending)
                {
                    // We should only fire the touch event once, and then wait.
                    TouchPending?.Invoke(this, EventArgs.Empty);

                    // Wait for the touch flag to clear
                    return WaitFor(
                        r => !r.TouchPending,
                        checkForTouch: false,
                        shortTimeout: false,
                        ExceptionMessages.UserInteractionTimeout);
                }

                if (stopCondition(report))
                {
                    _log.SensitiveLogInformation("Stop condition encountered: [{Report}]", report);
                    return report;
                }
            }

            ResetReadMode();
            _log.LogWarning($"Timed out after {stopwatch.ElapsedMilliseconds}ms.");
            throw new KeyboardConnectionException(timeoutMessage);
        }

        private void ResetReadMode()
        {
            var resetReport = new KeyboardReport()
            {
                Flags = KeyboardReportFlags.WritePending,
                SequenceNumber = 0xF,
            };

            _log.LogInformation("Reset read mode [{Report}]", resetReport);
            _hidConnection.SetReport(resetReport.ToArray());

            // Not strictly necessary, but it's in the spec, so here it is.
            _ = _hidConnection.GetReport();
        }
    }
}
