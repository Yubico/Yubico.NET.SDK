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
using System.Globalization;
using Yubico.Core.Logging;
using Yubico.YubiKey.Oath.Commands;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Oath;

/// <summary>
///     The main entry-point for all OATH related operations.
/// </summary>
public sealed partial class OathSession : ApplicationSession
{
    // ReSharper disable once InconsistentNaming
    internal OathApplicationData _oathData; // Internal for testing

    /// <summary>
    ///     Create an instance of <c>OathSession</c> class, the object that represents
    ///     the OATH application on the YubiKey.
    /// </summary>
    /// <remarks>
    ///     Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword.
    ///     For example,
    ///     <code language="csharp">
    ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
    ///     using (var oath = new OathSession(yubiKeyToUse))
    ///     {
    ///         /* Perform OATH operations. */
    ///     }
    /// </code>
    /// </remarks>
    /// <param name="yubiKey">
    ///     The object that represents the actual YubiKey which will perform the operations.
    /// </param>
    /// <param name="keyParameters">If supplied, the parameters used open the SCP connection.</param>
    /// <exception cref="ArgumentNullException">
    ///     The <c>yubiKey</c> argument is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The <c>SelectApplicationData</c> received from the <c>yubiKey</c> is null.
    /// </exception>
    public OathSession(IYubiKeyDevice yubiKey, ScpKeyParameters? keyParameters = null)
        : base(Log.GetLogger<OathSession>(), yubiKey, YubiKeyApplication.Oath, keyParameters)
    {
        if (Connection.SelectApplicationData is not OathApplicationData data)
        {
            throw new InvalidOperationException(nameof(Connection.SelectApplicationData));
        }

        _oathData = data;
    }

    /// <summary>
    ///     Indicates whether the OATH application on the YubiKey is
    ///     password-protected or not, whether password verification is required
    ///     before operations can be executed.
    /// </summary>
    public bool IsPasswordProtected => !_oathData.Challenge.IsEmpty;

    /// <summary>
    ///     The Delegate this class will call when it needs a password to unlock the OATH application.
    /// </summary>
    /// <remarks>
    ///     The delegate provided will read the <c>KeyEntryData</c> which contains the information needed
    ///     to determine what to collect and methods to submit what was collected. The delegate will return
    ///     <c>true</c> for success or <c>false</c> for "cancel". A cancel will usually happen when the user
    ///     has clicked a "Cancel" button.
    ///     <p>
    ///         Note that the SDK will call the <c>KeyCollector</c> with a <c>Request</c> of <c>Release</c>
    ///         when the process completes. In this case, the <c>KeyCollector</c> MUST NOT throw an exception.
    ///         The <c>Release</c> is called from inside a <c>finally</c> block, and it is a bad idea to throw
    ///         exceptions from inside <c>finally</c>.
    ///     </p>
    /// </remarks>
    public Func<KeyEntryData, bool>? KeyCollector { get; set; }

    /// <summary>
    ///     Resets the YubiKey's OATH application back to a factory default state.
    /// </summary>
    /// <remarks>
    ///     This will remove the password if one set and delete all credentials stored on the YubiKey.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     The <c>ResetCommand</c> failed.
    /// </exception>
    public void ResetApplication()
    {
        var resetOathResponse = Connection.SendCommand(new ResetCommand());
        if (resetOathResponse.Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException(resetOathResponse.StatusMessage);
        }

        var selectOathResponse = Connection.SendCommand(new SelectOathCommand());
        _oathData = selectOathResponse.GetData();
    }

    private void EnsureKeyCollector()
    {
        if (KeyCollector is null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.MissingKeyCollector));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            KeyCollector = null;
            base.Dispose(disposing);
        }
    }
}
