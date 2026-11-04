# Context7 query record — 2026-11-03

Resolved library `.NET` with query:
`System.Formats.Cbor CborWriter Ctap2Canonical map key automatic sorting CborReader ReadEndMap BytesRemaining AllowMultipleRootLevelValues System.Guid ReadOnlySpan byte constructor ToByteArray endianness`.
Resolver returned `/dotnet/docs`, `/websites/learn_microsoft_en-us_dotnet`, API browser, and other .NET references.
Selected `/websites/learn_microsoft_en-us_dotnet`; language C# for all queries.

Initial query 1:
`System.Formats.Cbor CborWriter Ctap2Canonical WriteEndMap automatic key sorting and CborReader ReadEndMap BytesRemaining allowMultipleRootLevelValues default`.
Returned CborReader constructors pointing to net11 preview and .NET11 CBOR max-depth compatibility
material. Not used to infer .NET10 sorting, trailing-data rejection or depth policy.

Initial query 2:
`System.Guid constructor ReadOnlySpan<byte> bigEndian default little endian and ToByteArray byte order first four next two bytes TryWriteBytes`.
Returned relevant net10 Guid TryWriteBytes(Span<Byte>,Boolean,Int32) and ToByteArray(Boolean)
summaries and parameters from https://learn.microsoft.com/en-us/dotnet/api/system.guid?view=net-10.0.
Also returned BitConverter examples; rejected as evidence for Guid byte conventions.

Focused follow-up:
`Guid(ReadOnlySpan<Byte>) constructor byte array first four bytes little endian and CborWriter.WriteEndMap sorts keys Ctap2Canonical`.
Returned net10 Guid constructor list including default and explicit-endian span overloads, but
no definitive default byte-convention or writer sorting details. BitConverter snippets rejected.
No further Context7 queries. Original net10 API pages and pinned runtime source supplied the
missing exact details, with executed pure-framework fixtures as independent behavioral evidence.
