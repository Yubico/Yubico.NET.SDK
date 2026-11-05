using var certificate = X509CertificateLoader.LoadCertificateFromFile("authentication.cer");
await session.StoreCertificateAsync(Slot.Authentication, certificate, CertificateCompression.Automatic);
using X509Certificate2? stored = await session.GetCertificateAsync(Slot.Authentication);
if (stored is null)
    throw new InvalidOperationException("The slot has no usable certificate data.");
