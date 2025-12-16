using System.Security.Cryptography.X509Certificates;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

public class ScpCertificates
{
    private ScpCertificates(X509Certificate2? ca, IReadOnlyList<X509Certificate2> bundle, X509Certificate2? leaf)
    {
        Ca = ca;
        Bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
        Leaf = leaf;
    }

    public X509Certificate2? Ca { get; }
    public IReadOnlyList<X509Certificate2> Bundle { get; }
    public X509Certificate2? Leaf { get; }

    public static ScpCertificates From(IEnumerable<X509Certificate2>? certificates)
    {
        var certList = certificates?.ToList() ?? [];
        if (certList.Count == 0) return new ScpCertificates(null, [], null);

        // Order the certificates: root CA first, then intermediates, then leaf
        var orderedCerts = OrderCertificates(certList);

        // Identify CA: the first cert if it's self-signed
        X509Certificate2? ca = null;
        if (orderedCerts.Count > 0 && IsSelfSigned(orderedCerts[0]))
        {
            ca = orderedCerts[0];
            orderedCerts.RemoveAt(0);
        }

        // Identify leaf: the last cert if it has DigitalSignature key usage
        X509Certificate2? leaf = null;
        if (orderedCerts.Count > 0 && HasDigitalSignature(orderedCerts[^1]))
        {
            leaf = orderedCerts[^1];
            orderedCerts.RemoveAt(orderedCerts.Count - 1);
        }

        // Remaining certs are the bundle (intermediates)
        return new ScpCertificates(ca, orderedCerts, leaf);
    }

    private static List<X509Certificate2> OrderCertificates(List<X509Certificate2> certList)
    {
        if (certList.Count == 0) return [];

        // Find the root certificate (self-signed)
        var root = certList.FirstOrDefault(IsSelfSigned);
        if (root == null) throw new InvalidOperationException("No root certificate found in the collection");

        var ordered = new List<X509Certificate2> { root };
        certList.Remove(root);

        // Build the chain by following issuer-subject links
        while (certList.Count > 0)
        {
            var next = certList.FirstOrDefault(c => IsIssuedBy(c, ordered[^1]));
            if (next == null) break; // No more certificates in the chain

            ordered.Add(next);
            certList.Remove(next);
        }

        // If there are leftover certificates, they are not part of the chain
        if (certList.Count > 0)
            throw new InvalidOperationException(
                $"Certificates not part of the chain: {string.Join(", ", certList.Select(c => c.Subject))}");

        return ordered;
    }

    private static bool IsIssuedBy(X509Certificate2 subjectCert, X509Certificate2 issuerCert) =>
        subjectCert.IssuerName.RawData.SequenceEqual(issuerCert.SubjectName.RawData);

    private static bool IsSelfSigned(X509Certificate2 cert) => IsIssuedBy(cert, cert);

    private static bool HasDigitalSignature(X509Certificate2 cert)
    {
        var keyUsage = cert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages ?? default;
        return keyUsage.HasFlag(X509KeyUsageFlags.DigitalSignature);
    }
}