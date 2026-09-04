using System.Reflection;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Oath;
using Yubico.YubiKit.OpenPgp;
using Yubico.YubiKit.Piv;
using Yubico.YubiKit.SecurityDomain;
using Yubico.YubiKit.YubiHsm;
using Yubico.YubiKit.YubiOtp;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class AppletSessionShapeTests
{
    internal static readonly (Type Session, Type Contract, string FactoryName)[] Sessions =
    [
        (typeof(ManagementSession), typeof(IManagementSession), "CreateManagementSessionAsync"),
        (typeof(PivSession), typeof(IPivSession), "CreatePivSessionAsync"),
        (typeof(FidoSession), typeof(IFidoSession), "CreateFidoSessionAsync"),
        (typeof(OathSession), typeof(IOathSession), "CreateOathSessionAsync"),
        (typeof(OpenPgpSession), typeof(IOpenPgpSession), "CreateOpenPgpSessionAsync"),
        (typeof(SecurityDomainSession), typeof(ISecurityDomainSession), "CreateSecurityDomainSessionAsync"),
        (typeof(YubiOtpSession), typeof(IYubiOtpSession), "CreateYubiOtpSessionAsync"),
        (typeof(HsmAuthSession), typeof(IHsmAuthSession), "CreateHsmAuthSessionAsync")
    ];

    [Fact]
    public void AppletSessions_AreSealedAndImplementCompletePublicContracts()
    {
        var violations = new List<string>();

        foreach (var (session, contract, _) in Sessions)
        {
            if (!session.IsSealed)
                violations.Add($"{session.Name} is not sealed");

            if (!contract.IsAssignableFrom(session))
                violations.Add($"{session.Name} does not implement {contract.Name}");

            var contractMembers = contract.GetMembers().Concat(contract.GetInterfaces().SelectMany(static i => i.GetMembers())).ToArray();
            foreach (var member in session.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (member.MemberType is not (MemberTypes.Method or MemberTypes.Property) ||
                    member is MethodInfo { IsSpecialName: true })
                    continue;

                bool represented = member switch
                {
                    PropertyInfo property => contractMembers.OfType<PropertyInfo>().Any(p =>
                        p.Name == property.Name && p.PropertyType == property.PropertyType),
                    MethodInfo method => contractMembers.OfType<MethodInfo>().Any(m => SameSignature(m, method)),
                    _ => true
                };

                if (!represented)
                    violations.Add($"{session.Name}.{member.Name} is missing from {contract.Name}");
            }
        }

        Assert.Empty(violations);
    }

    internal static bool SameSignature(MethodInfo left, MethodInfo right) =>
        left.Name == right.Name &&
        left.GetGenericArguments().Length == right.GetGenericArguments().Length &&
        SameTypeShape(left.ReturnType, right.ReturnType) &&
        left.GetParameters().Zip(right.GetParameters()).All(static parameters =>
            SameTypeShape(parameters.First.ParameterType, parameters.Second.ParameterType) &&
            parameters.First.Name == parameters.Second.Name &&
            parameters.First.IsOptional == parameters.Second.IsOptional &&
            Equals(parameters.First.RawDefaultValue, parameters.Second.RawDefaultValue)) &&
        left.GetParameters().Length == right.GetParameters().Length;

    private static bool SameTypeShape(Type left, Type right)
    {
        if (left.IsGenericParameter || right.IsGenericParameter)
            return left.IsGenericParameter && right.IsGenericParameter &&
                left.GenericParameterPosition == right.GenericParameterPosition;

        if (left.IsGenericType || right.IsGenericType)
            return left.IsGenericType && right.IsGenericType &&
                left.GetGenericTypeDefinition() == right.GetGenericTypeDefinition() &&
                left.GetGenericArguments().Zip(right.GetGenericArguments()).All(static types =>
                    SameTypeShape(types.First, types.Second));

        return left == right;
    }

    internal static IEnumerable<MethodInfo> GetDeviceExtensionMethods(Type session) =>
        session.Assembly.GetTypes()
            .SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(static method => method.GetParameters() is [{ ParameterType: var firstType }, ..] &&
                firstType == typeof(IYubiKey));
}