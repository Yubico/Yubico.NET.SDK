// Copyright (c) Yubico AB

using Microsoft.Extensions.Logging;
using System;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard
{
    internal static class SmartCardLoggerExtensions
    {
        public static IDisposable BeginTransactionScope(this ILogger logger, IDisposable transactionScope) =>
            logger.BeginScope("Transaction[{TransactionID}]", transactionScope.GetHashCode());

        public static void SCardApiCall(this ILogger logger, string apiName, uint result)
        {
            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                logger.LogInformation("{APIName} called successfully.", apiName);
            }
            else
            {
                logger.LogError("{APIName} called and FAILED. Result = {Result}", apiName, result);
            }
        }

        public static void CardReset(this ILogger logger) =>
            logger.LogWarning("The smart card was reset.");
    }
}
