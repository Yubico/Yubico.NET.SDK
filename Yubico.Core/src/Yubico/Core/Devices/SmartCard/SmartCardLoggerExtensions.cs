// Copyright (c) Yubico AB

using System;
using Microsoft.Extensions.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard;

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

    public static void CardReset(this ILogger logger) => logger.LogWarning("The smart card was reset.");
}
