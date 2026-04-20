// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.YubiHsm.IntegrationTests.Helpers;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.YubiHsm.IntegrationTests.TestExtensions;

/// <summary>
///     Extensions that help integration tests acquire <see cref="HsmAuthSession" />
///     instances while ensuring connections are disposed correctly.
/// </summary>
public static class HsmAuthTestStateExtensions
{

    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with an <see cref="HsmAuthSession" />.
        ///     Optionally resets the HsmAuth applet before running the test action.
        /// </summary>
        /// <param name="action">The async action to execute with the session.</param>
        /// <param name="resetBeforeUse">
        ///     When <c>true</c>, resets the HsmAuth applet before running the test action.
        ///     Defaults to <c>true</c>.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for authentication.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task WithHsmAuthSessionAsync(
            Func<HsmAuthSession, Task> action,
            bool resetBeforeUse = true,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default) =>
            state.WithConnectionAsync(async connection =>
            {
                var sharedConnection = new SharedSmartCardConnection(connection);

                if (resetBeforeUse)
                {
                    await using var resetSession = await HsmAuthSession.CreateAsync(
                            sharedConnection,
                            configuration,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    await resetSession.ResetAsync(cancellationToken).ConfigureAwait(false);
                }

                await using var session = await HsmAuthSession.CreateAsync(
                        sharedConnection,
                        configuration,
                        scpKeyParams,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await action(session).ConfigureAwait(false);
            }, cancellationToken);
    }

}
