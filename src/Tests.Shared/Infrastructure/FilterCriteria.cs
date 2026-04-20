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

using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Encapsulates all filter criteria for YubiKey device selection in tests.
///     Used by <see cref="WithYubiKeyAttribute"/> to specify requirements and by
///     <see cref="YubiKeyTestInfrastructure.FilterDevices"/> to apply them.
/// </summary>
/// <remarks>
///     This record consolidates filter parameters to avoid duplication across
///     attributes, test state, and filtering methods. It is immutable and
///     serializable for xUnit test data transport.
/// </remarks>
public sealed record FilterCriteria
{
    /// <summary>
    ///     Gets the minimum firmware version required (e.g., "5.7.2").
    ///     Null means no minimum firmware requirement.
    /// </summary>
    public string? MinFirmware { get; init; }

    /// <summary>
    ///     Gets the required form factor.
    ///     <see cref="FormFactor.Unknown"/> means any form factor is accepted.
    /// </summary>
    public FormFactor FormFactor { get; init; } = FormFactor.Unknown;

    /// <summary>
    ///     Gets whether USB transport is required.
    /// </summary>
    public bool RequireUsb { get; init; }

    /// <summary>
    ///     Gets whether NFC transport is required.
    /// </summary>
    public bool RequireNfc { get; init; }

    /// <summary>
    ///     Gets the required connection type.
    ///     <see cref="ConnectionType.Unknown"/> means any connection type is accepted.
    /// </summary>
    public ConnectionType ConnectionType { get; init; } = ConnectionType.Unknown;

    /// <summary>
    ///     Gets the required capability (must be enabled on the device).
    ///     <see cref="DeviceCapabilities.None"/> means no capability requirement.
    /// </summary>
    public DeviceCapabilities Capability { get; init; } = DeviceCapabilities.None;

    /// <summary>
    ///     Gets the required FIPS-capable capability.
    ///     <see cref="DeviceCapabilities.None"/> means no FIPS-capable requirement.
    /// </summary>
    public DeviceCapabilities FipsCapable { get; init; } = DeviceCapabilities.None;

    /// <summary>
    ///     Gets the required FIPS-approved capability.
    ///     <see cref="DeviceCapabilities.None"/> means no FIPS-approved requirement.
    /// </summary>
    public DeviceCapabilities FipsApproved { get; init; } = DeviceCapabilities.None;

    /// <summary>
    ///     Gets the custom filter type implementing <see cref="IYubiKeyFilter"/>.
    ///     Null means no custom filter is applied.
    /// </summary>
    /// <remarks>
    ///     For serialization, use <see cref="CustomFilterTypeName"/> which stores
    ///     the assembly-qualified type name.
    /// </remarks>
    public Type? CustomFilterType { get; init; }

    /// <summary>
    ///     Gets or sets the assembly-qualified name of the custom filter type.
    ///     Used for xUnit serialization since <see cref="Type"/> cannot be directly serialized.
    /// </summary>
    public string? CustomFilterTypeName { get; init; }

    /// <summary>
    ///     Creates a new <see cref="FilterCriteria"/> with default values (no filtering).
    /// </summary>
    public FilterCriteria()
    {
    }

    /// <summary>
    ///     Gets whether any filter criteria are specified.
    /// </summary>
    public bool HasCriteria =>
        MinFirmware is not null ||
        FormFactor != FormFactor.Unknown ||
        RequireUsb ||
        RequireNfc ||
        ConnectionType != ConnectionType.Unknown ||
        Capability != DeviceCapabilities.None ||
        FipsCapable != DeviceCapabilities.None ||
        FipsApproved != DeviceCapabilities.None ||
        CustomFilterType is not null ||
        CustomFilterTypeName is not null;

    /// <summary>
    ///     Resolves the <see cref="CustomFilterType"/> from <see cref="CustomFilterTypeName"/>
    ///     if not already set. Call this after deserialization.
    /// </summary>
    /// <returns>A new <see cref="FilterCriteria"/> with the resolved type, or this instance if unchanged.</returns>
    public FilterCriteria ResolveCustomFilterType()
    {
        if (CustomFilterType is not null || string.IsNullOrEmpty(CustomFilterTypeName))
            return this;

        var resolvedType = Type.GetType(CustomFilterTypeName);
        return this with { CustomFilterType = resolvedType };
    }

    /// <summary>
    ///     Creates a copy with the <see cref="CustomFilterTypeName"/> set from <see cref="CustomFilterType"/>.
    ///     Call this before serialization.
    /// </summary>
    /// <returns>A new <see cref="FilterCriteria"/> with the type name set.</returns>
    public FilterCriteria PrepareForSerialization()
    {
        if (CustomFilterType is null || CustomFilterTypeName is not null)
            return this;

        return this with { CustomFilterTypeName = CustomFilterType.AssemblyQualifiedName };
    }

    /// <summary>
    ///     Builds a human-readable description of the filter criteria.
    /// </summary>
    /// <returns>A comma-separated list of active criteria, or "None (all devices)" if no criteria.</returns>
    public string GetDescription()
    {
        var criteria = new List<string>();

        if (MinFirmware is not null)
            criteria.Add($"MinFirmware >= {MinFirmware}");

        if (FormFactor != FormFactor.Unknown)
            criteria.Add($"FormFactor = {FormFactor}");

        if (RequireUsb)
            criteria.Add("Transport = USB");

        if (RequireNfc)
            criteria.Add("Transport = NFC");

        if (ConnectionType != ConnectionType.Unknown)
            criteria.Add($"ConnectionType = {ConnectionType}");

        if (Capability != DeviceCapabilities.None)
            criteria.Add($"Capability = {Capability}");

        if (FipsCapable != DeviceCapabilities.None)
            criteria.Add($"FipsCapable = {FipsCapable}");

        if (FipsApproved != DeviceCapabilities.None)
            criteria.Add($"FipsApproved = {FipsApproved}");

        if (CustomFilterType is not null)
        {
            var filter = YubiKeyTestInfrastructure.InstantiateCustomFilter(CustomFilterType);
            criteria.Add(filter is not null
                ? $"CustomFilter = {filter.GetDescription()}"
                : $"CustomFilter = {CustomFilterType.Name} (failed to instantiate)");
        }

        return criteria.Count > 0 ? string.Join(", ", criteria) : "None (all devices)";
    }

    /// <summary>
    ///     Builds a short description suitable for test display names.
    /// </summary>
    /// <returns>A comma-separated list of active criteria in short form, or "All" if no criteria.</returns>
    public string GetShortDescription()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(MinFirmware))
            parts.Add($"FW>={MinFirmware}");

        if (FormFactor != FormFactor.Unknown)
            parts.Add($"FF={FormFactor}");

        if (RequireUsb)
            parts.Add("USB");

        if (RequireNfc)
            parts.Add("NFC");

        if (ConnectionType != ConnectionType.Unknown)
            parts.Add($"Conn={ConnectionType}");

        if (Capability != DeviceCapabilities.None)
            parts.Add($"Cap={Capability}");

        if (FipsCapable != DeviceCapabilities.None)
            parts.Add($"FipsCap={FipsCapable}");

        if (FipsApproved != DeviceCapabilities.None)
            parts.Add($"FipsAppr={FipsApproved}");

        if (CustomFilterType is not null)
            parts.Add($"Filter={CustomFilterType.Name}");

        return parts.Count > 0 ? string.Join(",", parts) : "All";
    }
}
