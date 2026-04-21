// Copyright (c) Yubico AB

using System;
using Microsoft.Extensions.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard
{
    internal static partial class SmartCardLoggerExtensions
    {
        public static IDisposable? BeginTransactionScope(this ILogger logger, IDisposable transactionScope) =>
            logger.BeginScope("Transaction[{TransactionID}]", transactionScope.GetHashCode());

        public static void SCardApiCall(this ILogger logger, string apiName, uint result)
        {
            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                logger.LogInformation("{ApiName} called successfully.", apiName);
            }
            else
            {
                logger.LogError(
                    "{ApiName} called and FAILED. Result = {Result:X} {Message}",
                    apiName,
                    result,
                    SCardException.GetErrorString(result));
            }
        }

        /// <summary>
        /// Logs an SCard API call result, with optional severity downgrade for known-recoverable errors.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="apiName">The name of the SCard API that was called.</param>
        /// <param name="result">The result code returned by the API.</param>
        /// <param name="knownRecoverable">
        /// When <c>true</c> and the result is not <see cref="ErrorCode.SCARD_S_SUCCESS"/>,
        /// logs at Debug severity instead of Error severity.
        /// Use this flag for errors that are expected in recovery paths (e.g., SCARD_E_INVALID_HANDLE
        /// during context re-establishment after an RDS disconnect) to avoid flooding production logs
        /// with error-level entries.
        /// </param>
        public static void SCardApiCall(this ILogger logger, string apiName, uint result, bool knownRecoverable)
        {
            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                logger.LogInformation("{ApiName} called successfully.", apiName);
            }
            else
            {
                if (knownRecoverable)
                {
                    logger.LogDebug(
                        "{ApiName} called and FAILED (known recoverable). Result = {Result:X} {Message}",
                        apiName,
                        result,
                        SCardException.GetErrorString(result));
                }
                else
                {
                    logger.LogError(
                        "{ApiName} called and FAILED. Result = {Result:X} {Message}",
                        apiName,
                        result,
                        SCardException.GetErrorString(result));
                }
            }
        }

        public static void CardReset(this ILogger logger) =>
            logger.LogWarning("The smart card was reset.");
    }
}
