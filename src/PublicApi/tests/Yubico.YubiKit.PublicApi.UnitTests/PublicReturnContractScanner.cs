using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Yubico.YubiKit.PublicApi.UnitTests;

internal enum ReturnContractViolationKind
{
    MutableCollection,
    Tuple
}

internal sealed record ReturnContractViolation(
    string Signature,
    ReturnContractViolationKind Kind,
    string OffendingType)
{
    internal string Message => Kind switch
    {
        ReturnContractViolationKind.MutableCollection => $"returns mutable collection shape {OffendingType}",
        ReturnContractViolationKind.Tuple => $"returns public tuple shape {OffendingType}",
        _ => throw new InvalidOperationException($"Unknown return-contract violation kind {Kind}.")
    };
}

internal static class PublicReturnContractScanner
{
    private const string ShippingAssemblyMetadataKey = "PublicApiShippingAssembly";

    private static readonly HashSet<Type> MutableGenericDeclarations =
    [
        typeof(ICollection<>),
        typeof(IList<>),
        typeof(IDictionary<,>),
        typeof(ISet<>),
        typeof(List<>),
        typeof(Dictionary<,>),
        typeof(HashSet<>),
        typeof(LinkedList<>),
        typeof(Queue<>),
        typeof(Stack<>),
        typeof(SortedDictionary<,>),
        typeof(SortedList<,>),
        typeof(Collection<>),
        typeof(ObservableCollection<>)
    ];

    private static readonly HashSet<Type> MutableDeclarations =
    [
        typeof(ICollection),
        typeof(IList),
        typeof(IDictionary)
    ];

    private static readonly HashSet<Type> FrameworkReadOnlyGenericDeclarations =
    [
        typeof(ReadOnlyCollection<>),
        typeof(ReadOnlyDictionary<,>),
        typeof(ReadOnlyObservableCollection<>),
        typeof(ReadOnlySet<>),
        typeof(FrozenDictionary<,>),
        typeof(FrozenSet<>),
        typeof(ImmutableArray<>),
        typeof(ImmutableList<>),
        typeof(ImmutableDictionary<,>),
        typeof(ImmutableHashSet<>),
        typeof(ImmutableSortedDictionary<,>),
        typeof(ImmutableSortedSet<>)
    ];

    internal static IReadOnlyList<ReturnContractViolation> ScanType(Type type)
    {
        var violations = new List<ReturnContractViolation>();

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.DeclaringType?.Assembly == type.Assembly && IsExternallyVisible(method, type)))
        {
            if (!method.IsSpecialName)
                ScanShape(method.ReturnType, type.Assembly, FormatSignature(method), violations, [], []);
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(property => property.DeclaringType?.Assembly == type.Assembly && IsExternallyVisible(property, type)))
            ScanShape(property.PropertyType, type.Assembly, FormatSignature(property), violations, [], []);

        return violations;
    }

    internal static IReadOnlyList<ReturnContractViolation> ScanAssemblies(IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(static assembly => assembly.GetExportedTypes())
            .SelectMany(ScanType)
            .Distinct()
            .OrderBy(static violation => violation.Signature, StringComparer.Ordinal)
            .ThenBy(static violation => violation.Message, StringComparer.Ordinal)
            .ToArray();

    internal static IReadOnlyList<Assembly> GetMarkedShippingAssemblies(Assembly markerAssembly) =>
        markerAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(static attribute => attribute.Key == ShippingAssemblyMetadataKey)
            .Select(static attribute => attribute.Value)
            .OfType<string>()
            .Select(Assembly.Load)
            .DistinctBy(static assembly => assembly.GetName().Name, StringComparer.Ordinal)
            .OrderBy(static assembly => assembly.GetName().Name, StringComparer.Ordinal)
            .ToArray();

    internal static string FormatSignature(MethodInfo method)
    {
        string genericArguments = method.IsGenericMethodDefinition
            ? $"<{string.Join(", ", method.GetGenericArguments().Select(FormatType))}>"
            : string.Empty;
        string parameters = string.Join(", ", method.GetParameters().Select(static parameter => FormatType(parameter.ParameterType)));
        return $"{FormatType(method.DeclaringType ?? throw new InvalidOperationException("Method has no declaring type."))}.{method.Name}{genericArguments}({parameters}): {FormatType(method.ReturnType)}";
    }

    private static string FormatSignature(PropertyInfo property) =>
        $"{FormatType(property.DeclaringType ?? throw new InvalidOperationException("Property has no declaring type."))}.{property.Name}: {FormatType(property.PropertyType)}";

    private static void ScanShape(
        Type type,
        Assembly owningAssembly,
        string signature,
        ICollection<ReturnContractViolation> violations,
        HashSet<Type> visitedShapes,
        HashSet<Type> expandedOwnedTypes)
    {
        if (!visitedShapes.Add(type))
            return;

        Type shape = UnwrapAsync(type);
        if (shape != type && !visitedShapes.Add(shape))
            return;

        if (IsMutableCollection(shape))
        {
            violations.Add(new ReturnContractViolation(
                signature,
                ReturnContractViolationKind.MutableCollection,
                FormatType(shape)));
        }

        if (IsTuple(shape))
            violations.Add(new ReturnContractViolation(signature, ReturnContractViolationKind.Tuple, FormatType(shape)));

        if (shape.IsArray)
        {
            ScanShape(shape.GetElementType() ?? throw new InvalidOperationException("Array has no element type."), owningAssembly, signature, violations, visitedShapes, expandedOwnedTypes);
            return;
        }

        if (shape.IsGenericType)
        {
            foreach (Type argument in shape.GetGenericArguments())
                ScanShape(argument, owningAssembly, signature, violations, visitedShapes, expandedOwnedTypes);
        }

        if (shape.Assembly != owningAssembly || shape.IsGenericParameter || shape.IsPrimitive || shape.IsEnum)
            return;

        Type traversalIdentity = shape.IsGenericType ? shape.GetGenericTypeDefinition() : shape;
        if (!expandedOwnedTypes.Add(traversalIdentity))
            return;

        foreach (PropertyInfo property in shape.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(property => property.DeclaringType?.Assembly == owningAssembly))
            ScanShape(property.PropertyType, owningAssembly, FormatSignature(property), violations, visitedShapes, expandedOwnedTypes);
    }

    internal static Type UnwrapAsync(Type type)
    {
        Type current = type;
        while (current.IsGenericType &&
            current.GetGenericTypeDefinition() is Type definition &&
            (definition == typeof(Task<>) || definition == typeof(ValueTask<>)))
        {
            current = current.GetGenericArguments()[0];
        }

        return current;
    }

    private static bool IsMutableCollection(Type type)
    {
        if (type.IsArray)
            return type.GetElementType() != typeof(byte);

        if (IsFrameworkReadOnlyCollection(type))
            return false;

        for (Type? current = type; current is not null; current = current.BaseType)
        {
            Type declaration = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (MutableDeclarations.Contains(declaration) || MutableGenericDeclarations.Contains(declaration))
                return true;
        }

        if (type.GetInterfaces().Any(IsMutableCollectionDeclaration))
            return true;

        return false;
    }

    private static bool IsTuple(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().FullName is string name &&
        (name.StartsWith("System.ValueTuple`", StringComparison.Ordinal) ||
            name.StartsWith("System.Tuple`", StringComparison.Ordinal));

    private static bool IsFrameworkReadOnlyCollection(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            Type declaration = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (FrameworkReadOnlyGenericDeclarations.Contains(declaration))
                return true;
        }

        return false;
    }

    private static bool IsMutableCollectionDeclaration(Type type)
    {
        Type declaration = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return MutableDeclarations.Contains(declaration) || MutableGenericDeclarations.Contains(declaration);
    }

    private static bool IsExternallyVisible(MethodBase method, Type surfacedType) =>
        method.IsPublic || (!surfacedType.IsSealed && (method.IsFamily || method.IsFamilyOrAssembly));

    private static bool IsExternallyVisible(PropertyInfo property, Type surfacedType) =>
        property.GetAccessors(nonPublic: true).Any(accessor => IsExternallyVisible(accessor, surfacedType));

    private static string FormatType(Type type)
    {
        if (type.IsArray)
            return $"{FormatType(type.GetElementType() ?? throw new InvalidOperationException("Array has no element type."))}[]";

        if (type.IsGenericParameter)
            return type.Name;

        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        string name = type.GetGenericTypeDefinition().FullName ?? type.GetGenericTypeDefinition().Name;
        int arityMarker = name.IndexOf('`', StringComparison.Ordinal);
        if (arityMarker >= 0)
            name = name[..arityMarker];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
    }
}