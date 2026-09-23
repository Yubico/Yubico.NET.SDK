using System.Security.Cryptography;
using System.Text;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

internal sealed record BoundaryRow(string Id, string Kind, string Owner, string Execution, string ReviewOwner,
    string Justification, string[] Evidence, string[] Status);

internal sealed record BoundaryFamily(string Path, string ReviewOwner, string Rationale, string[] Evidence, string[] Status, string[] Entries);

internal sealed record BoundaryManifest(int Version, BoundaryFamily[] Families)
{
    // The fingerprint is over the full path|resolved owner|kind|resolved target|occurrence grammar.
    // An explicit entry cannot match a new overload or a renamed target just because its file/family matches.
    internal static string Fingerprint(BoundarySite site) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(site.Id)))[..12];

    internal static IReadOnlyList<string> Validate(IEnumerable<BoundarySite> sites, BoundaryManifest manifest)
    {
        var scanned = sites.ToArray();
        var errors = new List<string>();
        if (manifest.Version != 1 || manifest.Families is null)
        {
            return ["invalid manifest version or families"];
        }
        var rows = new List<BoundaryRow>();
        foreach (var family in manifest.Families)
        {
            if (family.Entries is null || family.Evidence is null || family.Status is null ||
                string.IsNullOrWhiteSpace(family.ReviewOwner) ||
                (string.IsNullOrWhiteSpace(family.Rationale) && family.Entries.Any(entry => entry.Split('#').Length == 2)) ||
                family.Evidence.Length == 0 || family.Evidence.Any(string.IsNullOrWhiteSpace) ||
                family.Status.Length == 0 || family.Status.Any(status => status is not ("documented" or "implemented" or "verified" or "outstanding")) ||
                family.Status.Contains("verified") && family.Status.Contains("outstanding"))
            {
                errors.Add("invalid family: " + family.Path);
                continue;
            }
            foreach (var encoded in family.Entries)
            {
                var entry = encoded.Split('#', 4);
                if (entry.Length is not (2 or 4) || entry[0].Length == 0 || entry[1].Length != 12 ||
                    !entry[1].All(Uri.IsHexDigit))
                {
                    errors.Add("invalid entry: " + encoded);
                    continue;
                }
                var matches = scanned.Where(site => site.Path == family.Path &&
                    Fingerprint(site) == entry[1] && (site.Owner.Contains("." + entry[0] + "(", StringComparison.Ordinal) ||
                        site.Owner.Contains("." + entry[0] + "()", StringComparison.Ordinal))).ToArray();
                if (matches.Length != 1)
                {
                    errors.Add("stale: " + family.Path + "|" + encoded);
                    continue;
                }
                var site = matches[0];
                if (entry.Length == 2 && site.Kind != "native-import")
                {
                    errors.Add("non-native site requires site-specific execution and justification: " + site.Id);
                    continue;
                }
                rows.Add(new BoundaryRow(site.Id, site.Kind, site.Owner, entry.Length == 4 ? entry[2] : "sync-boundary",
                    family.ReviewOwner, entry.Length == 4 ? entry[3] : family.Rationale + " Resolved entry point: " + site.Target,
                    family.Evidence, family.Status));
            }
        }
        errors.AddRange(Validate(scanned, rows));
        return errors;
    }

    internal static BoundaryRow Review(BoundarySite site, string reviewOwner, string justification, string evidence, string status) =>
        new(site.Id, site.Kind, site.Owner, "sync-boundary", reviewOwner, justification, [evidence], [status]);

    internal static IReadOnlyList<string> Validate(IEnumerable<BoundarySite> sites, IEnumerable<BoundaryRow> rows)
    {
        var scanned = sites.ToDictionary(site => site.Id);
        var reviewed = rows.ToArray();
        var reviewedIds = reviewed.Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
        var errors = new List<string>();
        foreach (var site in scanned.Values.Where(site => !reviewedIds.Contains(site.Id)))
        {
            errors.Add("unclassified: " + site.Id);
        }
        foreach (var row in reviewed)
        {
            if (!scanned.TryGetValue(row.Id, out var site) || row.Owner != site.Owner || row.Kind != site.Kind)
            {
                errors.Add("stale: " + row.Id);
            }
            if (string.IsNullOrWhiteSpace(row.ReviewOwner) || string.IsNullOrWhiteSpace(row.Justification) ||
                row.Evidence is not { Length: > 0 } || row.Evidence.Any(string.IsNullOrWhiteSpace) ||
                row.Status is not { Length: > 0 } || row.Status.Any(status => status is not ("outstanding" or "documented" or "implemented" or "verified")) ||
                row.Execution is not ("sync-boundary" or "async-blocked" or "worker" or "callback") ||
                row.Status.Contains("verified") && row.Status.Contains("outstanding"))
            {
                errors.Add("invalid review: " + row.Id);
            }
        }
        foreach (var duplicate in reviewed.GroupBy(row => row.Id).Where(group => group.Count() > 1))
        {
            errors.Add("duplicate: " + duplicate.Key);
        }
        // A scheduling wrapper is not evidence that its callee is nonblocking. Surface this
        // syntactic hazard for human review; virtual/interface calls cannot be proven safe here.
        foreach (var site in scanned.Values.Where(site => site.UnsafeAsync && reviewed.Any(row =>
                     row.Id == site.Id && row.Status?.Contains("verified") == true)))
        {
            errors.Add("unsafe async wrapper: " + site.Id);
        }
        return errors;
    }
}