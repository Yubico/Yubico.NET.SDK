using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

public class BoundaryScannerWindowsCompletionTests
{
    private const string Fixture = """
        using System.Threading;
        using Bound = System.Threading.ThreadPoolBoundHandle;
        unsafe class Completion {
            delegate NativeOverlapped* Allocator(IOCompletionCallback callback, object state, object pinData);
            static void Callback(uint error, uint bytes, NativeOverlapped* overlapped) { }
            static Bound Bind(System.Runtime.InteropServices.SafeHandle handle) => global::System.Threading.ThreadPoolBoundHandle.BindHandle(handle);
            static NativeOverlapped* Safe(Bound bound) => bound.AllocateNativeOverlapped(Callback, null, null);
            internal static NativeOverlapped* Forward(Bound bound, IOCompletionCallback callback) => bound.UnsafeAllocateNativeOverlapped(pinData: null, state: null, callback: callback);
            static void Free(Bound bound, NativeOverlapped* overlapped) => bound.FreeNativeOverlapped(overlapped);
            static void NullCallback(Bound bound) => bound.UnsafeAllocateNativeOverlapped(null, null, null);
            static void Preallocated(Bound bound, PreAllocatedOverlapped overlapped) => bound.AllocateNativeOverlapped(overlapped);
            static void ReferenceOnly(Bound bound) { Allocator reference = bound.UnsafeAllocateNativeOverlapped; var name = nameof(Bound.UnsafeAllocateNativeOverlapped); }
            static void OtherLifetime(Bound bound) => bound.Dispose();
        }
        unsafe class Custom {
            static void BindHandle(object handle) { }
            static void UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData) { }
            static void AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData) { }
            static void FreeNativeOverlapped(NativeOverlapped* overlapped) { }
            static void Setup(IOCompletionCallback callback) { BindHandle(null); UnsafeAllocateNativeOverlapped(callback, null, null); AllocateNativeOverlapped(callback, null, null); FreeNativeOverlapped(null); }
        }
        """;

    [Fact]
    public void Scan_RealCompletionSetupAndTeardown_ReportsOnlyBoundedResolvedCalls()
    {
        var sites = BoundaryScanner.Scan([("Completion.cs", Fixture)]);
        Assert.Equal(4, sites.Count);
        Assert.Equal(2, sites.Count(site => site.Kind == "callback-registration"));
        Assert.Equal(2, sites.Count(site => site.Kind == "runtime-native-call"));
        Assert.All(sites, site => Assert.StartsWith("global::System.Threading.ThreadPoolBoundHandle.", site.Target));
        Assert.Contains(sites, site => site.Owner == "global::Completion.Forward(global::System.Threading.ThreadPoolBoundHandle, global::System.Threading.IOCompletionCallback)" &&
            site.Target == "global::System.Threading.ThreadPoolBoundHandle.UnsafeAllocateNativeOverlapped(global::System.Threading.IOCompletionCallback, object, object)");
    }

    [Fact]
    public void Scan_CompletionForwarder_RecognizesOneLevelIndependentOfFileOrderAndWhitespace()
    {
        var sources = new[]
        {
            ("Caller.cs", "unsafe class Caller { static void Setup(System.Threading.ThreadPoolBoundHandle bound, System.Threading.IOCompletionCallback callback) => Completion.Forward(bound, callback); }"),
            ("Completion.cs", Fixture)
        };
        var sites = BoundaryScanner.Scan(sources);
        Assert.Contains(sites, site => site.Kind == "callback-registration" && site.Owner.StartsWith("global::Caller.Setup(", StringComparison.Ordinal));
        Assert.Equal(sites, BoundaryScanner.Scan(sources.Reverse()));
        Assert.Equal(sites, BoundaryScanner.Scan(sources.Select(source => (source.Item1, "\n\n" + source.Item2))));
    }

    [Fact]
    public void Scan_SourceTypeUsingBclFullName_DoesNotImpersonateRuntimeBoundary()
    {
        const string source = """
            namespace System.Threading {
                class ThreadPoolBoundHandle {
                    internal static void BindHandle(object handle) { }
                    internal static void UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData) { }
                    internal static void FreeNativeOverlapped(object overlapped) { }
                    void Setup(IOCompletionCallback callback) { BindHandle(null); UnsafeAllocateNativeOverlapped(callback, null, null); FreeNativeOverlapped(null); }
                }
            }
            """;
        Assert.Empty(BoundaryScanner.Scan([("Lookalike.cs", source)]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scan_MetadataTypeUsingBclFullName_DoesNotImpersonateRuntimeBoundary(bool spoofCallback)
    {
        const string source = """
            namespace System.Threading {
                public unsafe class ThreadPoolBoundHandle {
                    public static void BindHandle(object handle) { }
                    public static void AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData) { }
                    public static void UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData) { }
                    public static void FreeNativeOverlapped(object overlapped) { }
                }
            }
            """;
        string metadataSource = source + (spoofCallback
            ? "namespace System.Threading { public delegate void IOCompletionCallback(); }" : "");
        var compilation = CSharpCompilation.Create("CompletionLookalike", [CSharpSyntaxTree.ParseText(metadataSource, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join("; ", emitted.Diagnostics));
        var reference = MetadataReference.CreateFromImage(image.ToArray(),
            new MetadataReferenceProperties(aliases: ["lookalike"]));
        const string caller = """
            extern alias lookalike;
            using Bound = lookalike::System.Threading.ThreadPoolBoundHandle;
            class Caller {
                static void Setup(System.Threading.IOCompletionCallback callback) {
                    Bound.BindHandle(null);
                    Bound.AllocateNativeOverlapped(callback, null, null);
                    Bound.UnsafeAllocateNativeOverlapped(callback, null, null);
                    Bound.FreeNativeOverlapped(null);
                }
                static void Forward(System.Threading.IOCompletionCallback callback) => Setup(callback);
            }
            """;
        string callerSource = spoofCallback
            ? caller.Replace("System.Threading.IOCompletionCallback", "lookalike::System.Threading.IOCompletionCallback", StringComparison.Ordinal)
            : caller;
        Assert.Empty(BoundaryScanner.Scan([("LookalikeCaller.cs", callerSource)], [reference]));
        Assert.Equal(BoundaryScanner.Scan([("Completion.cs", Fixture)]),
            BoundaryScanner.Scan([("Completion.cs", Fixture), ("LookalikeCaller.cs", callerSource)], [reference]));
    }

    [Fact]
    public void Validate_AddedRealInvocationAgainstRetainedManifest_FailsExactSiteGate()
    {
        var sites = BoundaryScanner.Scan([("Completion.cs", Fixture)]);
        var rows = sites.Select(site => BoundaryManifest.Review(site, "Core maintainers", "Fixture source inspection", "Compilation only", "outstanding")).ToArray();
        var changed = BoundaryScanner.Scan([("Completion.cs", Fixture),
            ("Added.cs", "class Added { static void Bind(System.Runtime.InteropServices.SafeHandle handle) => System.Threading.ThreadPoolBoundHandle.BindHandle(handle); }")]);
        var added = Assert.Single(changed.Except(sites));
        Assert.Equal(["unclassified: " + added.Id], BoundaryManifest.Validate(changed, rows));
        Assert.Contains(BoundaryManifest.Validate(BoundaryScanner.Scan([]), rows), error => error.StartsWith("stale:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UnreviewedCompletionRegistration_FailsExactSiteGate()
    {
        var sites = BoundaryScanner.Scan([("Completion.cs", Fixture)]);
        var registration = Assert.Single(sites, site => site.Target.Contains(".UnsafeAllocateNativeOverlapped(", StringComparison.Ordinal));
        var rows = sites.Select(site => BoundaryManifest.Review(site, "Core maintainers", "Fixture source inspection", "Compilation only", "outstanding")).ToArray();
        Assert.Empty(BoundaryManifest.Validate(sites, rows));
        Assert.Contains("unclassified: " + registration.Id, BoundaryManifest.Validate(sites, rows.Where(row => row.Id != registration.Id)));
        Assert.Contains(BoundaryManifest.Validate([], rows), error => error.StartsWith("stale:", StringComparison.Ordinal));
        Assert.Throws<DirectoryNotFoundException>(() => BoundaryScanner.ScanDirectory(Path.Combine(BoundaryScanner.CoreSourceRoot(), "MissingCompletionInput")));
    }
}