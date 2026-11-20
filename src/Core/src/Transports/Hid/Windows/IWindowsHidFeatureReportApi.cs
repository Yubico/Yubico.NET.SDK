// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Mechanical synchronous feature-report access for one Windows OTP owner.</summary>
/// <remarks>
/// One instance records resources at the owner's direction, without scheduling or physical claims.
/// Arrays include a zero report-ID prefix and are borrowed only synchronously through method return,
/// including exceptional return. No native operation may retain them after return.
/// Descriptor lengths include the prefix; connection payload sizes exclude it. The typed OTP
/// connection separately requires eight payload bytes.
/// </remarks>
internal interface IWindowsHidFeatureReportApi
{
    /// <summary>Opens with zero desired access and reads feature metadata; never falls back to FIDO read/write access.</summary>
    /// <remarks>Records acquired resources before each fallible step so CloseOnce can check cleanup even after Open throws.</remarks>
    WindowsHidFeatureReportMetadata Open(string devicePath);

    /// <summary>Gets a feature into the full descriptor-sized native report array.</summary>
    /// <remarks>HidD success provides no actual transfer count; the result does not invent one.</remarks>
    WindowsHidFeatureResult GetFeature(byte[] nativeReport);

    /// <summary>Sets a full descriptor-sized native report, borrowed only until return.</summary>
    WindowsHidFeatureResult SetFeature(byte[] nativeReport);

    /// <summary>Closes once after synchronous native calls return and reports checked release evidence.</summary>
    /// <remarks>Failed or unexecuted release is not proof; never retry close. No-resource failed opens may prove release.</remarks>
    WindowsHidRelease CloseOnce();
}

/// <summary>Feature descriptor metadata; length includes the report-ID byte.</summary>
/// <param name="UsagePage">HID usage page.</param>
/// <param name="Usage">HID usage.</param>
/// <param name="FeatureReportByteLength">Full native feature length.</param>
internal readonly record struct WindowsHidFeatureReportMetadata(ushort UsagePage, ushort Usage, ushort FeatureReportByteLength);

/// <summary>Synchronous HidD result with captured native error and no transfer-count claim.</summary>
/// <param name="ErrorCode">Zero for success, otherwise the captured native failure.</param>
internal readonly record struct WindowsHidFeatureResult(uint ErrorCode);