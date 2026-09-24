using System.Reflection;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

internal sealed record PublicSyncBoundaryRow(string Entry, string PublicType, string PublicSymbol,
    string Owner, string Symbol, string Evidence);

internal static class PublicSyncBoundaryRegistry
{
    // Independent of the rows: removing an entry cannot silently reduce the supported scope.
    private static readonly (string Entry, string PublicType, string PublicSymbol, string Owner, string Symbol)[] Required =
    [
        ("input open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToIOReports", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidInterface", "ConnectToIOReports"),
        ("input get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "GetReport"),
        ("input set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "SetReport"),
        ("input dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "Dispose"),
        ("feature open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToFeatureReports", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidInterface", "ConnectToFeatureReports"),
        ("feature get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "GetReport"),
        ("feature set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "SetReport"),
        ("feature dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "Dispose"),
        ("smart begin", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransaction", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "BeginTransaction"),
        ("smart async default", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync"),
        ("smart scope end", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.SmartCard.PcscConnectionNativeState+TransactionScope", "Dispose"),
        ("smart dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "Dispose"),
        ("listener start", "Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener", "Start", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Start"),
        ("listener stop", "Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener", "Stop", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Stop"),
        ("manager shutdown", "Yubico.YubiKit.Core.Devices.YubiKeyManager", "Shutdown", "Yubico.YubiKit.Core.Devices.YubiKeyManager", "Shutdown"),
        ("HID scan dispatch", "Yubico.YubiKit.Core.Transports.Hid.FindHidInterfaces", "FindAllAsync", "Yubico.YubiKit.Core.Transports.Hid.FindHidInterfaces", "FindAllAsync")
    ];

    // These are reachability links, not assertions of native drain. Behavioral evidence is run separately.
    internal static readonly PublicSyncBoundaryRow[] Rows =
    [
        new("input open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToIOReports", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidInterface", "ConnectToIOReports", "MacOSHidIOCompatibilityTests.FailedConstructorPreservesOpenFailureAndReleasesDevice"),
        new("input get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "GetReport", "MacOSHidIOCompatibilityTests.TimeoutDetachesReadAndLateReportIsAvailableOnRetry"),
        new("input set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "SetReport", "MacOSHidIOCompatibilityTests.ArbitraryOutputRetainsBorrowUntilWorkerReturnsAndIsCleared"),
        new("input dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "Dispose", "MacOSHidIOCompatibilityTests.PendingReadWakesOnDisposeButReleaseWaitsForAcknowledgment"),
        new("feature open", "Yubico.YubiKit.Core.Transports.Hid.IHidInterface", "ConnectToFeatureReports", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidInterface", "ConnectToFeatureReports", "MacOSHidFeatureFacadeTests.ConstructorFailure_UsesCheckedPartialOpenRelease"),
        new("feature get", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "GetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "GetReport", "MacOSHidFeatureFacadeTests.NativeGetError_IsPreservedAndPartialBufferCleared"),
        new("feature set", "Yubico.YubiKit.Core.Transports.Hid.IHidConnection", "SetReport", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "SetReport", "MacOSHidFeatureCompatibilityTests.PendingExpertSetDrainsBeforeAsyncDispose_AndConcurrentDisposeSharesClose"),
        new("feature dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "Dispose", "MacOSHidFeatureCompatibilityTests.UnprovenCloseRetainsNativeObjectAndSharesFailure"),
        new("smart begin", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransaction", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "BeginTransaction", "PcscConnectionLifetimeTests.BuiltInPcscConnection_ConcurrentBeginWhileFirstIsHeld_IsRefusedBeforeSecondSubmission"),
        new("smart async default", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync", "PcscConnectionLifetimeTests.AsyncTransaction_ExternalDefaultFallback_BlocksCallerUntilSynchronousBeginReturns"),
        new("smart scope end", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.SmartCard.PcscConnectionNativeState+TransactionScope", "Dispose", "PcscConnectionLifetimeTests.BuiltInPcscConnection_TransactionEndAndShutdownDuringTransmit_AreOrderedAndCoalesced"),
        new("smart dispose", "System.IDisposable", "Dispose", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "Dispose", "PcscConnectionLifetimeTests.BuiltInPcscConnection_SynchronousDispose_DrainsHeldNativeTransmitBeforeReleasingContext"),
        new("listener start", "Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener", "Start", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Start", "MacOSHidListenerLifetimeTests.Start_RegistersAndSchedulesBeforeOpen_ThenRunsAndClosesBeforeRelease"),
        new("listener stop", "Yubico.YubiKit.Core.Transports.Hid.HidDeviceListener", "Stop", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Stop", "MacOSHidListenerLifetimeTests.CallbackSelfStop_DoesNotJoinItself_AndDrainsBeforeRelease"),
        // A parked publication proves late results are silenced after bounded disposal, not native drain.
        new("manager shutdown", "Yubico.YubiKit.Core.Devices.YubiKeyManager", "Shutdown", "Yubico.YubiKit.Core.Devices.YubiKeyManager", "Shutdown", "YubiKeyDeviceManagerTests.DisposeAsync_WithAPublicationResumingAfterDisposeReturned_EmitsNothing"),
        // Task.Run is a per-call dispatch, not a bounded executor or a cancellable native operation.
        new("HID scan dispatch", "Yubico.YubiKit.Core.Transports.Hid.FindHidInterfaces", "FindAllAsync", "Yubico.YubiKit.Core.Transports.Hid.FindHidInterfaces", "FindAllAsync", "FindHidInterfacesBoundaryTests.WithheldScanReturnsPendingWithoutBlockingInvocationAndCancellationWaitsForEnumeration")
    ];

    internal static IReadOnlyList<string> Validate(IEnumerable<PublicSyncBoundaryRow> input)
    {
        var rows = input.ToArray();
        var errors = new List<string>();
        var core = typeof(UsbSmartCardConnection).Assembly;
        foreach (var required in Required)
        {
            var matches = rows.Where(row => row.Entry == required.Entry).ToArray();
            if (matches.Length != 1)
            {
                errors.Add($"missing entry: {required.Entry} (found {matches.Length})");
                continue;
            }
            var row = matches[0];
            if (row.PublicType != required.PublicType || row.PublicSymbol != required.PublicSymbol)
                errors.Add($"wrong public symbol: {row.Entry}");
            if (row.Owner != required.Owner || row.Symbol != required.Symbol)
                errors.Add($"wrong owner: {row.Entry}");
            var publicType = Type.GetType(required.PublicType) ?? core.GetType(required.PublicType);
            var ownerType = core.GetType(required.Owner);
            if (!HasMethod(publicType, required.PublicSymbol))
                errors.Add($"missing public symbol: {row.Entry}");
            if (!HasMethod(ownerType, required.Symbol))
                errors.Add($"missing internal owner symbol: {row.Entry}");
            if (publicType is not null && ownerType is not null && !publicType.IsAssignableFrom(ownerType))
                errors.Add($"owner does not implement public contract: {row.Entry}");
            if (!HasEvidence(row.Evidence))
                errors.Add($"stale evidence: {row.Entry}");
        }
        foreach (var row in rows.Where(row => !Required.Any(item => item.Entry == row.Entry)))
            errors.Add($"unexpected entry: {row.Entry}");
        return errors;
    }

    private static bool HasMethod(Type? type, string method) => type is not null &&
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Any(candidate => candidate.Name == method);

    private static bool HasEvidence(string evidence)
    {
        if (evidence.StartsWith("No held-native ", StringComparison.Ordinal))
            return true;

        var separator = evidence.LastIndexOf('.');
        if (separator <= 0 || separator == evidence.Length - 1)
            return false;

        var testType = typeof(PublicSyncBoundaryRegistryTests).Assembly.GetTypes()
            .SingleOrDefault(type => type.Name == evidence[..separator]);
        return testType?.GetMethod(evidence[(separator + 1)..])?.GetCustomAttributes<FactAttribute>()
            .Any(AdapterContractRegistry.IsRunnable) == true;
    }
}
