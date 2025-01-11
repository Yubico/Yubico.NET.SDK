using System;
using Yubico.Core.Logging;
using Yubico.PlatformInterop;
namespace Yubico.Core.Devices.SmartCard
{
    internal static partial class SmartCardLoggerExtensions
    {
        [Obsolete("Obsolete")]
        public static IDisposable BeginTransactionScope(this Logger logger, IDisposable transactionScope) =>
            logger.BeginScope("Transaction[{TransactionID}]", transactionScope.GetHashCode())!;

        [Obsolete("Obsolete")]
        public static void SCardApiCall(this Logger logger, string apiName, uint result)
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

        [Obsolete("Obsolete, use the corresponding ILogger")]
        public static void CardReset(this Logger logger) =>
            logger.LogWarning("The smart card was reset.");
    }
}
