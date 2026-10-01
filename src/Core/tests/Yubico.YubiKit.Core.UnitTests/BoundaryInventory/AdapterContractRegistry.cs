using System.Reflection;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

internal sealed record AdapterContractProfile(string Role, string Method);
internal sealed record AdapterContractRow(string Route, string Operation, string Owner, string Symbol, AdapterContractProfile[] Profiles);

internal static class AdapterContractRegistry
{
    // Independent of the JSON rows: deleting one cannot shrink the gate.
    private static readonly (string Route, string Operation, string Owner, string Symbol, string[] Roles)[] Required =
    [
        ("macOS FIDO", "open", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSFidoHidConnection", "OpenAsync", ["pending", "failure", "cancel before"]),
        ("macOS FIDO", "send", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSFidoHidConnection", "SendAsync", ["overlap", "drain"]),
        ("macOS FIDO", "receive", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSFidoHidConnection", "ReceiveAsync", ["terminal"]),
        ("macOS FIDO", "dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSFidoHidConnection", "DisposeAsync", ["drain", "failure"]),
        ("macOS OTP", "open", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSOtpHidConnection", "OpenAsync", ["pending", "failure"]),
        ("macOS OTP", "send", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSOtpHidConnection", "SendAsync", ["overlap", "cancel during"]),
        ("macOS OTP", "receive", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSOtpHidConnection", "ReceiveAsync", ["failure", "cancel during"]),
        ("macOS OTP", "dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSOtpHidConnection", "DisposeAsync", ["failure"]),
        ("portable PCSC", "open", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "InitializeAsync", ["failure"]),
        ("portable PCSC", "transmit", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "TransmitAndReceiveAsync", ["overlap", "cancel before", "cancel during"]),
        ("portable PCSC", "transaction begin", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "BeginTransactionAsync", ["pending", "cancel before", "cancel during"]),
        ("portable PCSC", "transaction end", "Yubico.YubiKit.Core.Transports.SmartCard.PcscConnectionNativeState+TransactionScope", "DisposeAsync", ["drain", "failure"]),
        ("portable PCSC", "dispose", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "DisposeAsync", ["drain", "failure", "native worker"]),
        ("macOS direct input (sync)", "open", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", ".ctor", ["constructor failure"]),
        ("macOS direct input (sync)", "get", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "GetReport", ["timeout then retry", "buffer ownership"]),
        ("macOS direct input (sync)", "set", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "SetReport", ["native drain", "failure"]),
        ("macOS direct input (sync)", "dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidIOReportConnection", "Dispose", ["native drain", "shared close"]),
        ("macOS direct feature (sync)", "open", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", ".ctor", ["constructor failure"]),
        ("macOS direct feature (sync)", "get", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "GetReport", ["buffer ownership", "failure"]),
        ("macOS direct feature (sync)", "set", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "SetReport", ["native worker", "native drain"]),
        ("macOS direct feature (sync)", "dispose", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidFeatureReportConnection", "Dispose", ["shared close", "failure"]),
        ("macOS listener (sync)", "start", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Start", ["registration", "restart after timeout"]),
        ("macOS listener (sync)", "stop", "Yubico.YubiKit.Core.Transports.Hid.MacOS.MacOSHidDeviceListener", "Stop", ["callback drain", "shared stop after timeout"]),
        // Built-in PC/SC behavior only. Custom ISmartCardConnection implementations' native
        // lifetime behavior is not verified; the default async fallback only verifies caller blocking.
        ("portable PCSC (sync)", "transaction begin", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "BeginTransaction", ["native wait"]),
        ("portable PCSC (sync)", "transaction end", "Yubico.YubiKit.Core.Transports.SmartCard.PcscConnectionNativeState+TransactionScope", "Dispose", ["native drain"]),
        ("portable PCSC (sync)", "dispose", "Yubico.YubiKit.Core.Transports.SmartCard.UsbSmartCardConnection", "Dispose", ["native drain"]),
        ("portable PCSC (sync)", "async default begin", "Yubico.YubiKit.Core.Transports.SmartCard.ISmartCardConnection", "BeginTransactionAsync", ["caller block"])
    ];

    internal static IReadOnlyList<string> Validate(IEnumerable<AdapterContractRow> input)
    {
        var rows = input.ToArray();
        var errors = new List<string>();
        var keys = new HashSet<(string, string)>();
        var seenProfiles = new HashSet<string>(StringComparer.Ordinal);
        var core = typeof(UsbSmartCardConnection).Assembly;
        var tests = typeof(AdapterContractRegistryTests).Assembly;

        foreach (var row in rows)
        {
            var key = (row.Route, row.Operation);
            if (row.Route.StartsWith("Windows HID", StringComparison.Ordinal) ||
                row.Route.StartsWith("Linux HID", StringComparison.Ordinal))
                errors.Add($"deferred route cannot be verified: {row.Route}");
            if (!Required.Any(item => item.Route == key.Route && item.Operation == key.Operation))
                errors.Add($"unexpected operation: {row.Route}/{row.Operation}");
            if (!keys.Add(key))
                errors.Add($"duplicate operation: {row.Route}/{row.Operation}");
            ValidateOwner(row, core, errors);
            ValidateProfiles(row, tests, seenProfiles, errors);
        }

        foreach (var required in Required)
            if (!keys.Contains((required.Route, required.Operation)))
                errors.Add($"missing required operation: {required.Route}/{required.Operation}");

        return errors;
    }

    private static void ValidateOwner(AdapterContractRow row, Assembly core, List<string> errors)
    {
        var expected = Required.FirstOrDefault(item => item.Route == row.Route && item.Operation == row.Operation);
        if (expected.Owner is not null && row.Owner != expected.Owner)
            errors.Add($"wrong owner: {row.Route}/{row.Operation}/{row.Owner}");
        if (expected.Symbol is not null && row.Symbol != expected.Symbol)
            errors.Add($"wrong symbol: {row.Route}/{row.Operation}/{row.Symbol}");
        var owner = core.GetType(row.Owner);
        if (owner is null)
        {
            errors.Add($"missing owner: {row.Owner}");
            return;
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        if (string.IsNullOrWhiteSpace(row.Symbol) ||
            !(row.Symbol == ".ctor" ? owner.GetConstructors(flags).Length > 0 :
                owner.GetMethods(flags).Any(method => method.Name == row.Symbol)))
            errors.Add($"missing symbol: {row.Owner}.{row.Symbol}");
    }

    private static void ValidateProfiles(AdapterContractRow row, Assembly tests,
        HashSet<string> seenProfiles, List<string> errors)
    {
        var required = Required.FirstOrDefault(item => item.Route == row.Route && item.Operation == row.Operation).Roles;
        if (row.Profiles is null || row.Profiles.Length == 0)
        {
            errors.Add($"empty profile: {row.Route}/{row.Operation}");
            return;
        }

        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in row.Profiles)
        {
            ValidateProfile(row, profile, required, roles, seenProfiles, tests, errors);
        }

        ValidateRequiredRoles(row, required, roles, errors);
    }

    private static void ValidateProfile(AdapterContractRow row, AdapterContractProfile profile, string[]? required,
        HashSet<string> roles, HashSet<string> seenProfiles, Assembly tests, List<string> errors)
    {
        if (!roles.Add(profile.Role))
            errors.Add($"duplicate role: {row.Route}/{row.Operation}/{profile.Role}");
        if (!seenProfiles.Add(profile.Method))
            errors.Add($"duplicate profile: {profile.Method}");
        if (required is null || !required.Contains(profile.Role))
            errors.Add($"unexpected role: {row.Route}/{row.Operation}/{profile.Role}");
        ValidateTestMethod(profile.Method, tests, errors);
    }

    private static void ValidateRequiredRoles(AdapterContractRow row, string[]? required,
        HashSet<string> roles, List<string> errors)
    {
        if (required is null)
            return;
        foreach (var role in required)
            if (!roles.Contains(role))
                errors.Add($"missing role: {row.Route}/{row.Operation}/{role}");
    }

    private static void ValidateTestMethod(string profile, Assembly tests, List<string> errors)
    {
        var separator = profile?.LastIndexOf('.') ?? -1;
        var type = separator > 0 ? tests.GetType(profile[..separator]) : null;
        var name = separator > 0 ? profile[(separator + 1)..] : "";
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        if (type is null || !type.GetMethods(flags).Any(method => method.Name == name &&
            method.GetCustomAttributes(false).OfType<FactAttribute>().Any(IsRunnable)))
            errors.Add($"stale profile: {profile}");
    }

    internal static bool IsRunnable(FactAttribute attribute) =>
        attribute.Skip is null && !attribute.Explicit &&
        attribute.SkipUnless is null && attribute.SkipWhen is null &&
        attribute.SkipExceptions is not { Length: > 0 };
}
