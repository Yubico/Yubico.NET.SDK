using System.Reflection;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class PublicReturnContractTests
{
    private static readonly IReadOnlyDictionary<ReturnContractViolation, string> ReviewedExceptions = new Dictionary<ReturnContractViolation, string>
    {
        [Tuple("Yubico.YubiKit.Core.Cryptography.Oids.GetOidsByKeyType(Yubico.YubiKit.Core.Cryptography.KeyType): System.ValueTuple<System.String, System.String>", "System.ValueTuple<System.String, System.String>")] =
            "Existing low-level OID lookup returns its paired identifiers structurally.",
        [Mutable("Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.CmDevice.Children(): System.Collections.Generic.IList<Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.CmDevice>", "System.Collections.Generic.IList<Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.CmDevice>")] =
            "Existing Windows device-enumeration interop surface is outside the applet result contracts.",
        [Mutable("Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.CmDevice.GetList(System.Guid): System.Collections.Generic.IList<Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.CmDevice>", "System.Collections.Generic.IList<Yubico.YubiKit.Core.Native.Windows.Cfgmgr32.CmDevice>")] =
            "Existing Windows device-enumeration interop surface is outside the applet result contracts.",
        [Mutable("Yubico.YubiKit.Core.ProductAtrs.AllYubiKeys: System.Collections.Generic.IList<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.AnswerToReset>", "System.Collections.Generic.IList<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.AnswerToReset>")] =
            "Existing mutable ATR catalog declaration is unrelated low-level device metadata.",
        [Mutable("Yubico.YubiKit.Core.ProductAtrs.NfcYubiKeys: System.Collections.Generic.IList<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.AnswerToReset>", "System.Collections.Generic.IList<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.AnswerToReset>")] =
            "Existing mutable ATR catalog declaration is unrelated low-level device metadata.",
        [Mutable("Yubico.YubiKit.Core.ProductAtrs.UsbYubiKeys: System.Collections.Generic.IList<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.AnswerToReset>", "System.Collections.Generic.IList<Yubico.YubiKit.Core.Protocols.SmartCard.Apdu.AnswerToReset>")] =
            "Existing mutable ATR catalog declaration is unrelated low-level device metadata.",
        [Mutable("Yubico.YubiKit.Core.Utilities.MultiString.GetStrings(System.Byte[], System.Text.Encoding): System.String[]", "System.String[]")] =
            "Existing native multi-string decoder returns a detached string array.",
        [Mutable("Yubico.YubiKit.Core.Utilities.TlvHelper.DecodeDictionary(System.ReadOnlyMemory<System.Byte>): System.Collections.Generic.IDictionary<System.Int32, System.ReadOnlyMemory<System.Byte>>", "System.Collections.Generic.IDictionary<System.Int32, System.ReadOnlyMemory<System.Byte>>")] =
            "Existing low-level TLV decoder intentionally builds a caller-consumed mutable map.",
        [Mutable("Yubico.YubiKit.Core.Utilities.TlvHelper.DecodeDictionary(System.ReadOnlySpan<System.Byte>): System.Collections.Generic.IDictionary<System.Int32, System.ReadOnlyMemory<System.Byte>>", "System.Collections.Generic.IDictionary<System.Int32, System.ReadOnlyMemory<System.Byte>>")] =
            "Existing low-level TLV decoder intentionally builds a caller-consumed mutable map.",
        [Tuple("Yubico.YubiKit.Fido2.Extensions.PreviewSignCbor.DecodeRegistrationOutput(System.Formats.Cbor.CborReader): System.ValueTuple<System.Int32, System.Nullable<System.Int32>>", "System.ValueTuple<System.Int32, System.Nullable<System.Int32>>")] =
            "Existing CBOR helper returns the two decoded protocol fields as a pair.",
        [Tuple("Yubico.YubiKit.Fido2.Pin.IPinUvAuthProtocol.Encapsulate(System.Collections.Generic.IReadOnlyDictionary<System.Int32, System.Object>): System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>", "System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>")] =
            "Existing low-level PIN/UV protocol seam returns an encoded map and binary shared secret together.",
        [Mutable("Yubico.YubiKit.Fido2.Pin.IPinUvAuthProtocol.Encapsulate(System.Collections.Generic.IReadOnlyDictionary<System.Int32, System.Object>): System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>", "System.Collections.Generic.Dictionary<System.Int32, System.Object>")] =
            "Existing low-level PIN/UV protocol seam transfers an encoded mutable map.",
        [Tuple("Yubico.YubiKit.Fido2.Pin.PinUvAuthProtocolV1.Encapsulate(System.Collections.Generic.IReadOnlyDictionary<System.Int32, System.Object>): System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>", "System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>")] =
            "Existing low-level PIN/UV protocol implementation returns an encoded map and binary shared secret together.",
        [Mutable("Yubico.YubiKit.Fido2.Pin.PinUvAuthProtocolV1.Encapsulate(System.Collections.Generic.IReadOnlyDictionary<System.Int32, System.Object>): System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>", "System.Collections.Generic.Dictionary<System.Int32, System.Object>")] =
            "Existing low-level PIN/UV protocol implementation transfers an encoded mutable map.",
        [Tuple("Yubico.YubiKit.Fido2.Pin.PinUvAuthProtocolV2.Encapsulate(System.Collections.Generic.IReadOnlyDictionary<System.Int32, System.Object>): System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>", "System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>")] =
            "Existing low-level PIN/UV protocol implementation returns an encoded map and binary shared secret together.",
        [Mutable("Yubico.YubiKit.Fido2.Pin.PinUvAuthProtocolV2.Encapsulate(System.Collections.Generic.IReadOnlyDictionary<System.Int32, System.Object>): System.ValueTuple<System.Collections.Generic.Dictionary<System.Int32, System.Object>, System.Byte[]>", "System.Collections.Generic.Dictionary<System.Int32, System.Object>")] =
            "Existing low-level PIN/UV protocol implementation transfers an encoded mutable map.",
        [Tuple("Yubico.YubiKit.OpenPgp.OpenPgpAid.Version: System.ValueTuple<System.Int32, System.Int32>", "System.ValueTuple<System.Int32, System.Int32>")] =
            "Existing AID parser exposes the protocol's two-part version as a pair.",
        [Mutable("Yubico.YubiKit.OpenPgp.PrivateKeyTemplate.GetComponents(): Yubico.YubiKit.Core.Utilities.Tlv[]", "Yubico.YubiKit.Core.Utilities.Tlv[]")] =
            "Existing protected template hook transfers disposable TLV components to the base serializer.",
        [Mutable("Yubico.YubiKit.OpenPgp.RsaKeyTemplate.GetComponents(): Yubico.YubiKit.Core.Utilities.Tlv[]", "Yubico.YubiKit.Core.Utilities.Tlv[]")] =
            "Existing protected RSA template override transfers disposable TLV components to the base serializer."
    };

    [Fact]
    public void ShippingAssemblies_PublicReturnsDoNotExposeMutableCollectionsOrTuples()
    {
        IReadOnlyList<Assembly> shippingAssemblies = PublicReturnContractScanner.GetMarkedShippingAssemblies(Assembly.GetExecutingAssembly());
        Assert.NotEmpty(shippingAssemblies);

        IReadOnlyList<ReturnContractViolation> scannedViolations = PublicReturnContractScanner.ScanAssemblies(shippingAssemblies);
        ReturnContractViolation[] staleExceptions = ReviewedExceptions.Keys.Except(scannedViolations).ToArray();
        Assert.True(
            staleExceptions.Length == 0,
            $"Remove stale reviewed return-contract exceptions:{Environment.NewLine}{string.Join(Environment.NewLine, staleExceptions.Select(static violation => $"- {violation.Signature}: {violation.Message}"))}");

        ReturnContractViolation[] violations = scannedViolations
            .Where(violation => !ReviewedExceptions.ContainsKey(violation))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Public return-contract violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations.Select(static violation => $"- {violation.Signature}: {violation.Message}"))}");
    }

    private static ReturnContractViolation Mutable(string signature, string offendingType) =>
        new(signature, ReturnContractViolationKind.MutableCollection, offendingType);

    private static ReturnContractViolation Tuple(string signature, string offendingType) =>
        new(signature, ReturnContractViolationKind.Tuple, offendingType);
}