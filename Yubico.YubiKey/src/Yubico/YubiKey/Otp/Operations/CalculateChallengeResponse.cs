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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations;

/// <summary>
///     Operation class for sending an HMAC-SHA1, TOTP, or Yubico OTP challenge
///     to an OTP application slot on a YubiKey and receiving its response.
/// </summary>
public class CalculateChallengeResponse : OperationBase<CalculateChallengeResponse>
{
    internal CalculateChallengeResponse(IYubiKeyConnection connection, IOtpSession session, Slot slot)
        : base(connection, session, slot)
    {
    }

    /// <inheritdoc />
    protected override void PreLaunchOperation()
    {
        var exceptions = new List<Exception>();

        // This operation will work for YubiOtp and Challenge-Response, so
        // the caller will need to specify.
        if (_algorithm == ChallengeResponseAlgorithm.None)
        {
            exceptions.Add(new InvalidOperationException(ExceptionMessages.MustChooseAlgorithm));
        }

        // If the user set a challenge, then _isTotp is set to false instead
        // of null and _challenge is populated. If the user set TOTP, then the
        // challenge is populated with a TOTP challenge. So if _isTotp has a
        // value, then _challenge will be good.
        if (!_isTotp.HasValue)
        {
            exceptions.Add(new InvalidOperationException(ExceptionMessages.MustChooseTotpOrChallenge));
        }
        else if (_isTotp.Value && _algorithm == ChallengeResponseAlgorithm.YubicoOtp)
        {
            exceptions.Add(new InvalidOperationException(ExceptionMessages.YubicoOtpNotCompatible));
        }
        else if (!_isTotp.Value)
        {
            switch (_algorithm)
            {
                case ChallengeResponseAlgorithm.HmacSha1:
                    if (_challenge.Length < 1 || _challenge.Length > MaxHmacChallengeSize)
                    {
                        exceptions.Add(new InvalidOperationException(ExceptionMessages.HmacChallengeTooLong));
                    }

                    break;
                case ChallengeResponseAlgorithm.YubicoOtp:
                    if (_challenge.Length != YubicoOtpChallengeSize)
                    {
                        exceptions.Add(
                            new InvalidOperationException(ExceptionMessages.YubicoOtpChallengeLengthInvalid));
                    }

                    break;
            }
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? exceptions[0]
                : new AggregateException(ExceptionMessages.MultipleExceptions, exceptions);
        }
    }

    /// <inheritdoc />
    protected override void ExecuteOperation()
    {
        // This only needs to be called once, so short-circuit if it has
        // already been called.
        if (_dataBytes.Length == 0)
        {
            // Subscribe to the event in case touch is required.
            KeyboardConnection? kb = null;
            if (Connection is KeyboardConnection connection)
            {
                kb = connection;
                kb.TouchEvent += OnTouch;
            }

            void OnTouch(object sender, EventArgs e)
            {
                _ = Task.Run(_touchNotify);
            }

            try
            {
                // If anything is wrong, then that's on PreLaunchOperation(). We
                // should be able to assume that things are good if we're here.
                var cmd = new ChallengeResponseCommand(
                    OtpSlot!.Value,
                    _algorithm,
                    _challenge);

                var response = Connection.SendCommand(cmd);
                if (response.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiKeyOperationFailed,
                            response.StatusMessage));
                }

                _dataBytes = response.GetData();

                if (_algorithm == ChallengeResponseAlgorithm.HmacSha1)
                {
                    byte offset = (byte)(_dataBytes.Span[^1] & 0x0f);

                    // The ykman code reads this as a uint, but masks the top bit. I'll just do the same
                    // thing, but treat it as an int since that's CLS-compliant.
                    _dataInt = (int)BinaryPrimitives.ReadUInt32BigEndian(_dataBytes[offset..].Span) & 0x7fffffff;
                }
            }
            finally
            {
                // Unhook the touch monitor.
                if (kb is not null)
                {
                    kb.TouchEvent -= OnTouch;
                }
            }
        }
    }

    /// <summary>
    ///     Get the raw bytes for the OTP (one-time password).
    /// </summary>
    /// <returns><see cref="ReadOnlyMemory{T}" /> collection of bytes.</returns>
    public ReadOnlyMemory<byte> GetDataBytes()
    {
        // We're calling Execute here because the base class orchestrates
        // validation through it.
        Execute();

        return _dataBytes;
    }

    /// <summary>
    ///     Get the OTP (one-time password) as an <see langword="int" />.
    /// </summary>
    /// <returns>Single int representing the OTP.</returns>
    public int GetDataInt()
    {
        if (_algorithm != ChallengeResponseAlgorithm.HmacSha1)
        {
            throw new InvalidOperationException(ExceptionMessages.IntOrCodeOnlyWithHmac);
        }

        // We're calling Execute here because the base class orchestrates
        // validation through it.
        Execute();

        return _dataInt;
    }

    /// <summary>
    ///     Get the OTP code as a string representation of numeric digits.
    /// </summary>
    /// <param name="digits">The number of digits in the string (default is 6).</param>
    /// <returns>A <see cref="string" /> representation of the OTP.</returns>
    public string GetCode(int digits = 6)
    {
        if (digits < MinOtpDigits || digits > MaxOtpDigits)
        {
            throw new ArgumentOutOfRangeException(ExceptionMessages.OtpCodeDigitRange, nameof(digits));
        }

        return (GetDataInt() % (uint)Math.Pow(10, digits))
            .ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    /// <summary>
    ///     Accepts the challenge phrase as a <see langword="byte" /> array.
    /// </summary>
    /// <param name="challenge">A <see langword="byte" /> array.</param>
    /// <returns>The <see cref="CalculateChallengeResponse" /> instance</returns>
    public CalculateChallengeResponse UseChallenge(byte[] challenge)
    {
        if (_isTotp ?? false)
        {
            throw new InvalidOperationException(ExceptionMessages.BothTotpAndChallenge);
        }

        _isTotp = false;
        _challenge = challenge;

        return this;
    }

    /// <summary>
    ///     Instructs the operation to use TOTP instead of a <see langword="byte" /> array
    ///     for the challenge.
    /// </summary>
    /// <remarks>
    ///     UseYubiOtp(false) must be called along with UseTotp() in order for the YubiKey
    ///     to calculate the response code using the HMAC-SHA1 algorithm.
    /// </remarks>
    /// <returns>The <see cref="CalculateChallengeResponse" /> instance</returns>
    public CalculateChallengeResponse UseTotp()
    {
        if (!(_isTotp ?? true))
        {
            throw new InvalidOperationException(ExceptionMessages.BothTotpAndChallenge);
        }

        _isTotp = true;

        // ToUnixTimeSeconds returns a long, obviously in host order. The
        // challenge needs to be in network order.
        _challenge = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(
            _challenge,
            DateTimeOffset.Now.ToUnixTimeSeconds() / _period);

        return this;
    }

    /// <summary>
    ///     Sets the time period in seconds that a TOTP challenge is good for.
    /// </summary>
    /// <param name="seconds">Time resolution in seconds for the challenge.</param>
    /// <returns>The <see cref="CalculateChallengeResponse" /> instance</returns>
    public CalculateChallengeResponse WithPeriod(int seconds)
    {
        _period = seconds;
        return this;
    }

    /// <summary>
    ///     Set an <see cref="Action" /> delegate to notify users to touch the YubiKey button.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This delegate will be launched as a <see cref="Task" />. The SDK will
    ///         not wait or otherwise track the completion of the delegate. It is
    ///         meant as a simple notifier.
    ///     </para>
    ///     <para>
    ///         It is important to take into consideration that it will execute on
    ///         an unknown thread, so if you are using it to do a notification on a
    ///         graphical user interface, then you should be sure that you marshall
    ///         the call to the appropriate thread.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     Here is a very simple example of performing a challenge-response
    ///     operation on a YubiKey.
    ///     <code language="csharp">
    /// using (var otpSess = new OtpSession(_yubiKey))
    /// {
    ///     string otp = otp.CalculateChallengeResponse(_slot)
    ///         .UseTouchNotifier(() => Console.WriteLine("Press the YubiKey button."))
    ///         .UseTotp()
    ///         .GetCode();
    /// }
    /// </code>
    ///     As mentioned in the remarks section, showing a prompt in a GUI
    ///     application requires a little bit more work. Here is an example of
    ///     calling a notifier method.
    ///     <code language="csharp">
    /// using (var otpSess = new OtpSession(_yubiKey))
    /// {
    ///     string otp = otpSess.CalculateChallengeResponse(_slot)
    ///         .UseTouchNotifier(() => _appWindow.AlertUser())
    ///         .UseTotp()
    ///         .GetCode();
    ///     _appWindow.SetOtpCode(otp);
    /// }
    /// </code>
    ///     Here is how the notifier would handle making sure the notification
    ///     is handled on the correct thread.
    ///     <code language="csharp">
    /// public void AlertUser()
    /// {
    ///     if (!Dispatcher.CheckAccess())
    ///     {
    ///         Dispatcher.Invoke(() => AlertUser());
    ///     }
    ///     MessageBox.Show(this, "Press the YubiKey button.");
    /// }
    /// </code>
    /// </example>
    /// <param name="notifier"><see cref="Action" /> delegate.</param>
    /// <returns>The <see cref="CalculateChallengeResponse" /> instance</returns>
    public CalculateChallengeResponse UseTouchNotifier(Action notifier)
    {
        _touchNotify = notifier;
        return this;
    }

    /// <summary>
    ///     Sets the operation to use the Yubico OTP or HMAC-SHA1 algorithm to calculate the response.
    /// </summary>
    /// <remarks>
    ///     There is no default algorithm. You must either call this method with a
    ///     <see langword="true" /> parameter, which will configure the YubiKey to use
    ///     Yubico OTP as the algorithm, or a <see langword="false" /> parameter, which
    ///     will configure the YubiKey to use the HMAC-SHA1 algorithm (for both HMAC-SHA1
    ///     and TOTP challenges).
    /// </remarks>
    /// <param name="setting">
    ///     A <see langword="bool" /> specifying whether to use the Yubico OTP or HMAC-SHA1 algorithm.
    /// </param>
    /// <returns>The <see cref="CalculateChallengeResponse" /> instance</returns>
    public CalculateChallengeResponse UseYubiOtp(bool setting)
    {
        _algorithm =
            setting
                ? ChallengeResponseAlgorithm.YubicoOtp
                : ChallengeResponseAlgorithm.HmacSha1;

        return this;
    }

    #region Size Constants

    /// <summary>
    ///     Maximum length in bytes for an HMAC challenge.
    /// </summary>
    public const int MaxHmacChallengeSize = 64;

    /// <summary>
    ///     Size in bytes for a Yubico OTP challenge.
    /// </summary>
    public const int YubicoOtpChallengeSize = 6;

    /// <summary>
    ///     Minimum digits for an OTP (one-time password).
    /// </summary>
    public const int MinOtpDigits = 6;

    /// <summary>
    ///     Maximum digits for an OTP (one-time password).
    /// </summary>
    public const int MaxOtpDigits = 10;

    #endregion

    #region Private Fields

    private byte[] _challenge = Array.Empty<byte>();
    private bool? _isTotp;
    private int _period = 30;
    private ChallengeResponseAlgorithm _algorithm;
    private Action _touchNotify = () => Debug.WriteLine("YubiKey SDK: Default Touch Prompt");
    private ReadOnlyMemory<byte> _dataBytes;
    private int _dataInt;

    #endregion
}
