// Copyright (c) Yubico AB

using System;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard
{
    internal static class SmartCardLoggerExtensions
    {
        public static IDisposable BeginTransactionScope(this Logger logger, IDisposable transactionScope) =>
            logger.BeginScope("Transaction[{TransactionID}]", transactionScope.GetHashCode());

        public static void SCardApiCall(this Logger logger, string apiName, uint result)
        {
            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                logger.LogInformation("{ApiName} called successfully.", apiName);
            }
            else
            {
                logger.LogError("{ApiName} called and FAILED. Result = {Result:X}", apiName, result);
            }
        }

        public static void CardReset(this Logger logger) =>
            logger.LogWarning("The smart card was reset.");
    }
}
