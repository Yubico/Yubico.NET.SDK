// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Security.Cryptography.X509Certificates;

// namespace Yubico.YubiKey.Scp
// {
//     public class ScpCertificates
//     {
//         public X509Certificate2? Ca { get; }
//         public IReadOnlyList<X509Certificate2> Bundle { get; }
//         public X509Certificate2? Leaf { get; }

//         private ScpCertificates(X509Certificate2? ca, IReadOnlyList<X509Certificate2> bundle, X509Certificate2? leaf)
//         {
//             Ca = ca;
//             Bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
//             Leaf = leaf;
//         }

//         public static ScpCertificates From(IEnumerable<X509Certificate2>? certificates)
//         {
//             if (certificates == null || !certificates.Any())
//             {
//                 return new ScpCertificates(null, Array.Empty<X509Certificate2>(), null);
//             }

//             var certList = new List<X509Certificate2>(certificates);
//             X509Certificate2? ca = null;
//             byte[]? seenSerial = null;

//             // Order certificates with the Root CA on top
//             var ordered = new List<X509Certificate2> { certList[0] };
//             certList.RemoveAt(0);

//             while (certList.Count > 0)
//             {
//                 var head = ordered[0];
//                 var tail = ordered[^1];
//                 var cert = certList[0];
//                 certList.RemoveAt(0);

//                 if (IsIssuedBy(cert, cert))
//                 {
//                     ordered.Insert(0, cert);
//                     ca = ordered[0];
//                     continue;
//                 }

//                 if (IsIssuedBy(cert, tail))
//                 {
//                     ordered.Add(cert);
//                     continue;
//                 }

//                 if (IsIssuedBy(head, cert))
//                 {
//                     ordered.Insert(0, cert);
//                     continue;
//                 }

//                 if (seenSerial != null && cert.GetSerialNumber().SequenceEqual(seenSerial))
//                 {
//                     throw new InvalidOperationException($"Cannot decide the order of {cert} in {string.Join(", ", ordered)}");
//                 }

//                 // This cert could not be ordered, try to process rest of certificates
//                 // but if you see this cert again fail because the cert chain is not complete
//                 certList.Add(cert);
//                 seenSerial = cert.GetSerialNumber();
//             }

//             // Find ca and leaf
//             if (ca != null)
//             {
//                 ordered.RemoveAt(0);
//             }

//             X509Certificate2? leaf = null;
//             if (ordered.Count > 0)
//             {
//                 var lastCert = ordered[^1];
//                 var keyUsage = lastCert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages ?? Array.Empty<bool>();
//                 if (keyUsage.Length > 4 && keyUsage[4])
//                 {
//                     leaf = lastCert;
//                     ordered.RemoveAt(ordered.Count - 1);
//                 }
//             }

//             return new ScpCertificates(ca, ordered, leaf);
//         }

//         private static bool IsIssuedBy(X509Certificate2 subjectCert, X509Certificate2 issuerCert)
//         {
//             return subjectCert.IssuerName.RawData.SequenceEqual(issuerCert.SubjectName.RawData);
//         }
//     }
// }
