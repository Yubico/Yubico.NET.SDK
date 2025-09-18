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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.InterIndustry.Commands;

/// <summary>
///     Selects a smart card application.
/// </summary>
public abstract class BaseSelectApplicationCommand<TSelectResponse> : ISelectApplicationCommand<TSelectResponse>
    where TSelectResponse : ISelectApplicationResponse<ISelectApplicationData>
{
    private const byte INS_SELECT = 0xA4;

    // A value of 4 means selection will be done using a DF name (in this case, the application
    // identifier).
    private const byte P1_SELECT_BY_DF_NAME = 0b0100;

    private readonly byte[] _applicationId;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelectApplicationCommand" /> class.
    /// </summary>
    /// <param name="applicationId">The byte representation of an application identifier.</param>
    protected BaseSelectApplicationCommand(byte[] applicationId)
    {
        _applicationId = applicationId;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelectApplicationCommand" /> class.
    /// </summary>
    /// <param name="yubiKeyApplication">
    ///     The YubiKey application. `YubiKeyApplication.InterIndustry` is not a valid option.
    /// </param>
    protected BaseSelectApplicationCommand(YubiKeyApplication yubiKeyApplication)
    {
        if (yubiKeyApplication == YubiKeyApplication.InterIndustry)
        {
            throw new ArgumentException(ExceptionMessages.CantSelectInterIndustry);
        }

        _applicationId = yubiKeyApplication.GetIso7816ApplicationId();
    }

    #region ISelectApplicationCommand<TSelectResponse> Members

    /// <summary>
    ///     Gets the YubiKeyApplication (e.g. PIV, OATH, etc.) that this command applies to.
    /// </summary>
    /// <value>
    ///     The value will always be `YubiKeyApplication.InterIndustry` for this command.
    /// </value>
    public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

    /// <summary>
    ///     Creates a CommandApdu instance that instructs the smart card to select the previously
    ///     specified application.
    /// </summary>
    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Ins = INS_SELECT,
            P1 = P1_SELECT_BY_DF_NAME,
            Data = _applicationId
        };

    /// <summary>
    ///     Creates the appropriate response class that can parse the ResponseApdu.
    /// </summary>
    /// <param name="responseApdu">The ResponseApdu from the YubiKey.</param>
    /// <returns></returns>
    public abstract TSelectResponse CreateResponseForApdu(ResponseApdu responseApdu);

    #endregion
}
