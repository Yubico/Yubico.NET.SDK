using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class PublicReturnContractScannerTests
{
    [Fact]
    public void Scan_DetectsDictionarySubclassDirectlyAndOnNestedProperty()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(MutableFixtureApi));

        Assert.Contains(violations, violation =>
            violation.Signature.Contains(nameof(MutableFixtureApi.GetDirect), StringComparison.Ordinal) &&
            violation.Message.Contains("Dictionary", StringComparison.Ordinal));
        Assert.Contains(violations, violation =>
            violation.Signature.Contains($"{nameof(NestedMutableResult)}.{nameof(NestedMutableResult.Items)}", StringComparison.Ordinal) &&
            violation.Message.Contains("Dictionary", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_AllowsFrameworkReadOnlyWrappers()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(ReadOnlyFixtureApi));

        Assert.Empty(violations);
    }

    [Fact]
    public void Scan_AllowsReadOnlyWrapperSubclassAndFrameworkReadOnlySets()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(AdditionalReadOnlyFixtureApi));

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(typeof(Task<byte[]>))]
    [InlineData(typeof(ValueTask<byte[]>))]
    public void UnwrapAsync_UnwrapsTaskAndValueTask(Type asyncType)
    {
        Assert.Equal(typeof(byte[]), PublicReturnContractScanner.UnwrapAsync(asyncType));
    }

    [Fact]
    public void Scan_FlagsUnknownCustomDictionaryImplementation()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(CustomDictionaryFixtureApi));

        Assert.Contains(violations, violation =>
            violation.Kind == ReturnContractViolationKind.MutableCollection &&
            violation.Signature.Contains(nameof(CustomDictionaryFixtureApi.Entries), StringComparison.Ordinal) &&
            violation.OffendingType.Contains(nameof(CustomDictionary), StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_DetectsTupleNestedInAsyncCollectionShape()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(TupleFixtureApi));

        Assert.Contains(violations, violation =>
            violation.Signature.Contains(nameof(TupleFixtureApi.GetValuesAsync), StringComparison.Ordinal) &&
            violation.Message.Contains("ValueTuple<System.String, System.Int32>", StringComparison.Ordinal));
    }

    [Fact]
    public void Scan_DetectsSystemTupleNestedInCollectionShape()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(SystemTupleFixtureApi));

        ReturnContractViolation violation = Assert.Single(violations);
        Assert.Equal(ReturnContractViolationKind.Tuple, violation.Kind);
        Assert.Contains("System.Tuple<System.Int32, System.String>", violation.OffendingType, StringComparison.Ordinal);
    }

    [Fact]
    public void Scan_AcceptsBinaryByteArrayButFlagsOtherArrays()
    {
        IReadOnlyList<ReturnContractViolation> binaryViolations = PublicReturnContractScanner.ScanType(typeof(BinaryArrayFixtureApi));
        IReadOnlyList<ReturnContractViolation> mutableViolations = PublicReturnContractScanner.ScanType(typeof(MutableArrayFixtureApi));

        Assert.Empty(binaryViolations);
        ReturnContractViolation violation = Assert.Single(mutableViolations);
        Assert.Equal(ReturnContractViolationKind.MutableCollection, violation.Kind);
        Assert.Equal("System.Int32[]", violation.OffendingType);
    }

    [Fact]
    public void Scan_IncludesExternallyVisibleProtectedMembersOnly()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(VisibilityFixtureApi));

        Assert.Contains(violations, violation => violation.Signature.Contains("ProtectedValues", StringComparison.Ordinal));
        Assert.Contains(violations, violation => violation.Signature.Contains(nameof(VisibilityFixtureApi.ProtectedInternalValues), StringComparison.Ordinal));
        Assert.DoesNotContain(violations, violation => violation.Signature.Contains("InternalValues", StringComparison.Ordinal) &&
            !violation.Signature.Contains(nameof(VisibilityFixtureApi.ProtectedInternalValues), StringComparison.Ordinal));
        Assert.DoesNotContain(violations, violation => violation.Signature.Contains("PrivateProtectedValues", StringComparison.Ordinal));
    }

    [Fact]
    public void ReviewedTupleViolation_DoesNotExemptMutableShapeOnSameMember()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(PreciseExceptionFixtureApi));
        ReturnContractViolation tuple = Assert.Single(violations, violation => violation.Kind == ReturnContractViolationKind.Tuple);
        var stale = new ReturnContractViolation(tuple.Signature, ReturnContractViolationKind.MutableCollection, "System.Collections.Generic.List<System.Int32>");
        var reviewed = new Dictionary<ReturnContractViolation, string>
        {
            [tuple] = "Reviewed tuple only.",
            [stale] = "No longer present."
        };

        ReturnContractViolation unreviewed = Assert.Single(violations, violation => !reviewed.ContainsKey(violation));
        Assert.Equal(ReturnContractViolationKind.MutableCollection, unreviewed.Kind);
        Assert.Equal(stale, Assert.Single(reviewed.Keys.Except(violations)));
    }

    [Fact]
    public void Scan_BoundsRecursiveOwnedModelTraversal()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(RecursiveFixtureApi));

        Assert.Empty(violations);
    }

    [Fact]
    public void Scan_BoundsExpandingGenericOwnedModelTraversal()
    {
        IReadOnlyList<ReturnContractViolation> violations = PublicReturnContractScanner.ScanType(typeof(GenericRecursiveFixtureApi));

        Assert.Empty(violations);
    }

    [Fact]
    public void FormatSignature_DistinguishesOverloadsStably()
    {
        MethodInfo[] overloads = typeof(OverloadFixtureApi).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        string[] signatures = overloads.Select(PublicReturnContractScanner.FormatSignature).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(
        [
            $"{typeof(OverloadFixtureApi).FullName}.GetValue(System.Int32): System.String",
            $"{typeof(OverloadFixtureApi).FullName}.GetValue(System.String): System.String"
        ], signatures);
    }

    private sealed class DictionarySubclass : Dictionary<string, int>;

    private sealed class NestedMutableResult
    {
        public DictionarySubclass Items { get; } = [];
    }

    private sealed class MutableFixtureApi
    {
        public DictionarySubclass GetDirect() => [];

        public NestedMutableResult GetNested() => new();
    }

    private sealed class ReadOnlyFixtureApi
    {
        public ReadOnlyCollection<int> Values { get; } = new([]);

        public ReadOnlyDictionary<string, int> Entries { get; } = new(new Dictionary<string, int>());
    }

    private sealed class AdditionalReadOnlyFixtureApi
    {
        public ReadOnlyDictionarySubclass Entries { get; } = new(new Dictionary<string, int>());

        public ReadOnlySet<int> Values { get; } = new(new HashSet<int>());

        public FrozenDictionary<string, int> FrozenEntries { get; } = new Dictionary<string, int>().ToFrozenDictionary();

        public FrozenSet<int> FrozenValues { get; } = Array.Empty<int>().ToFrozenSet();
    }

    private sealed class ReadOnlyDictionarySubclass(IDictionary<string, int> dictionary)
        : ReadOnlyDictionary<string, int>(dictionary);

    private sealed class CustomDictionaryFixtureApi
    {
        public CustomDictionary Entries { get; } = new();
    }

    private sealed class CustomDictionary : IDictionary<string, int>
    {
        public int this[string key] { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public ICollection<string> Keys => throw new NotSupportedException();

        public ICollection<int> Values => throw new NotSupportedException();

        public int Count => 0;

        public bool IsReadOnly => false;

        public void Add(string key, int value) => throw new NotSupportedException();

        public void Add(KeyValuePair<string, int> item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(KeyValuePair<string, int> item) => false;

        public bool ContainsKey(string key) => false;

        public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotSupportedException();

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, int>>().GetEnumerator();

        public bool Remove(string key) => false;

        public bool Remove(KeyValuePair<string, int> item) => false;

        public bool TryGetValue(string key, out int value)
        {
            value = 0;
            return false;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class TupleFixtureApi
    {
        public ValueTask<IReadOnlyList<(int Id, IReadOnlyList<(string Name, int Count)> Values)>> GetValuesAsync() =>
            ValueTask.FromResult<IReadOnlyList<(int, IReadOnlyList<(string, int)>)>>([]);
    }

    private sealed class SystemTupleFixtureApi
    {
        public IReadOnlyList<Tuple<int, string>> Values { get; } = [];
    }

    private sealed class BinaryArrayFixtureApi
    {
        public byte[] GetPayload() => [];
    }

    private sealed class MutableArrayFixtureApi
    {
        public int[] GetValues() => [];
    }

    private class VisibilityFixtureApi
    {
        protected int[] ProtectedValues() => [];

        protected internal CustomDictionary ProtectedInternalValues { get; } = new();

        internal int[] InternalValues() => [];

        private protected int[] PrivateProtectedValues { get; } = [];
    }

    private sealed class PreciseExceptionFixtureApi
    {
        public (int Id, Dictionary<string, int> Values) GetValue() => default;
    }

    private sealed class RecursiveFixtureApi
    {
        public RecursiveResult GetResult() => new();
    }

    private sealed class RecursiveResult
    {
        public RecursiveResult? Next { get; init; }
    }

    private sealed class GenericRecursiveFixtureApi
    {
        public ExpandingNode<int> GetResult() => new();
    }

    private sealed class ExpandingNode<T>
    {
        public ExpandingNode<ExpandingNode<T>>? Next { get; init; }
    }

    private sealed class OverloadFixtureApi
    {
        public string GetValue(int value) => value.ToString();

        public string GetValue(string value) => value;
    }
}