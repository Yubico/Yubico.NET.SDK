# PivTool SDK/CLI Separation Refactor (Ralph Loop)

**Goal:** Separate pure SDK example code from CLI/UI code in PivTool so developers can easily find PIV SDK usage patterns without reading Spectre.Console code.

**Architecture:** Extract SDK logic from `Features/*.cs` into `PivExamples/*.cs` (static classes, no Spectre dependencies, result types). Move UI code to `Cli/` directory. Thin menu wrappers call SDK examples and display results.

**Tech Stack:** .NET 10, C# 14, Spectre.Console (CLI only), YubiKit PIV SDK

**Completion Promise:** PIVTOOL_REFACTOR_COMPLETE

---

## Pre-Flight State Verification (MANDATORY)

Before starting ANY work:

```bash
# Check for existing commits matching this feature
git log --oneline -5 --grep="pivtool\|PivTool\|SDK examples"

# Check current directory structure
ls -la Yubico.YubiKit.Piv/examples/PivTool/

# Establish build baseline
dotnet build.cs build 2>&1 | grep -E "error (CS|MSB)" | sort > /tmp/baseline-errors.txt || true
```

**Rule:** If evidence shows work partially done, continue from that state. Do NOT redo completed work.

---

## Phase 1: Create Directory Structure and Result Types (Priority: P0)

**Goal:** Create the new directory structure and define result types that establish the contract between SDK and CLI layers.

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Results/*.cs`

### Tasks

- [ ] 1.1: **Create directory structure**
  ```bash
  cd Yubico.YubiKit.Piv/examples/PivTool
  mkdir -p PivExamples/Results
  mkdir -p Cli/Menus Cli/Prompts Cli/Output
  ```

- [ ] 1.2: **Create SigningResult.cs**
  ```csharp
  // PivExamples/Results/SigningResult.cs
  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a PIV signing operation.
  /// </summary>
  public sealed record SigningResult
  {
      public bool Success { get; init; }
      public ReadOnlyMemory<byte> Signature { get; init; }
      public string? ErrorMessage { get; init; }
      public long ElapsedMilliseconds { get; init; }

      public static SigningResult Succeeded(ReadOnlyMemory<byte> signature, long elapsedMs) =>
          new() { Success = true, Signature = signature, ElapsedMilliseconds = elapsedMs };

      public static SigningResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.3: **Create DecryptionResult.cs**
  ```csharp
  // PivExamples/Results/DecryptionResult.cs
  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a PIV RSA decryption operation.
  /// </summary>
  public sealed record DecryptionResult
  {
      public bool Success { get; init; }
      public ReadOnlyMemory<byte> DecryptedData { get; init; }
      public string? ErrorMessage { get; init; }
      public long ElapsedMilliseconds { get; init; }

      public static DecryptionResult Succeeded(ReadOnlyMemory<byte> data, long elapsedMs) =>
          new() { Success = true, DecryptedData = data, ElapsedMilliseconds = elapsedMs };

      public static DecryptionResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.4: **Create VerificationResult.cs**
  ```csharp
  // PivExamples/Results/VerificationResult.cs
  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a signature verification operation.
  /// </summary>
  public sealed record VerificationResult
  {
      public bool Success { get; init; }
      public bool IsValid { get; init; }
      public string? ErrorMessage { get; init; }
      public long ElapsedMilliseconds { get; init; }

      public static VerificationResult Valid(long elapsedMs) =>
          new() { Success = true, IsValid = true, ElapsedMilliseconds = elapsedMs };

      public static VerificationResult Invalid(long elapsedMs) =>
          new() { Success = true, IsValid = false, ElapsedMilliseconds = elapsedMs };

      public static VerificationResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.5: **Create CertificateResult.cs**
  ```csharp
  // PivExamples/Results/CertificateResult.cs
  using System.Security.Cryptography.X509Certificates;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a certificate operation.
  /// </summary>
  public sealed record CertificateResult
  {
      public bool Success { get; init; }
      public X509Certificate2? Certificate { get; init; }
      public string? CsrPem { get; init; }
      public string? ErrorMessage { get; init; }

      public static CertificateResult Succeeded(X509Certificate2 cert) =>
          new() { Success = true, Certificate = cert };

      public static CertificateResult CsrGenerated(string csrPem) =>
          new() { Success = true, CsrPem = csrPem };

      public static CertificateResult Stored() =>
          new() { Success = true };

      public static CertificateResult Deleted() =>
          new() { Success = true };

      public static CertificateResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.6: **Create KeyGenerationResult.cs**
  ```csharp
  // PivExamples/Results/KeyGenerationResult.cs
  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a PIV key generation operation.
  /// </summary>
  public sealed record KeyGenerationResult
  {
      public bool Success { get; init; }
      public PivSlot Slot { get; init; }
      public PivAlgorithm Algorithm { get; init; }
      public ReadOnlyMemory<byte> PublicKey { get; init; }
      public string? ErrorMessage { get; init; }

      public static KeyGenerationResult Succeeded(PivSlot slot, PivAlgorithm algorithm, ReadOnlyMemory<byte> publicKey) =>
          new() { Success = true, Slot = slot, Algorithm = algorithm, PublicKey = publicKey };

      public static KeyGenerationResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.7: **Create AttestationResult.cs**
  ```csharp
  // PivExamples/Results/AttestationResult.cs
  using System.Security.Cryptography.X509Certificates;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a PIV attestation operation.
  /// </summary>
  public sealed record AttestationResult
  {
      public bool Success { get; init; }
      public X509Certificate2? AttestationCertificate { get; init; }
      public X509Certificate2? IntermediateCertificate { get; init; }
      public string? ErrorMessage { get; init; }

      public static AttestationResult Succeeded(X509Certificate2 attestation, X509Certificate2? intermediate = null) =>
          new() { Success = true, AttestationCertificate = attestation, IntermediateCertificate = intermediate };

      public static AttestationResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.8: **Create DeviceInfoResult.cs**
  ```csharp
  // PivExamples/Results/DeviceInfoResult.cs
  using Yubico.YubiKit.Management;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a device info query.
  /// </summary>
  public sealed record DeviceInfoResult
  {
      public bool Success { get; init; }
      public DeviceInfo? DeviceInfo { get; init; }
      public int? PinRetriesRemaining { get; init; }
      public int? PukRetriesRemaining { get; init; }
      public string? ErrorMessage { get; init; }

      public static DeviceInfoResult Succeeded(DeviceInfo info, int? pinRetries = null, int? pukRetries = null) =>
          new() { Success = true, DeviceInfo = info, PinRetriesRemaining = pinRetries, PukRetriesRemaining = pukRetries };

      public static DeviceInfoResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.9: **Create SlotInfoResult.cs**
  ```csharp
  // PivExamples/Results/SlotInfoResult.cs
  using System.Security.Cryptography.X509Certificates;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Information about a single PIV slot.
  /// </summary>
  public sealed record SlotInfo
  {
      public required PivSlot Slot { get; init; }
      public required string Name { get; init; }
      public PivSlotMetadata? Metadata { get; init; }
      public X509Certificate2? Certificate { get; init; }
      public bool HasKey => Metadata is not null;
      public bool HasCertificate => Certificate is not null;
  }

  /// <summary>
  /// Result of a slot info query.
  /// </summary>
  public sealed record SlotInfoResult
  {
      public bool Success { get; init; }
      public IReadOnlyList<SlotInfo> Slots { get; init; } = [];
      public string? ErrorMessage { get; init; }

      public static SlotInfoResult Succeeded(IReadOnlyList<SlotInfo> slots) =>
          new() { Success = true, Slots = slots };

      public static SlotInfoResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.10: **Create PinOperationResult.cs**
  ```csharp
  // PivExamples/Results/PinOperationResult.cs
  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a PIN/PUK operation.
  /// </summary>
  public sealed record PinOperationResult
  {
      public bool Success { get; init; }
      public int? RetriesRemaining { get; init; }
      public string? ErrorMessage { get; init; }

      public static PinOperationResult Succeeded(int? retriesRemaining = null) =>
          new() { Success = true, RetriesRemaining = retriesRemaining };

      public static PinOperationResult Failed(string error, int? retriesRemaining = null) =>
          new() { Success = false, ErrorMessage = error, RetriesRemaining = retriesRemaining };
  }
  ```

- [ ] 1.11: **Create ResetResult.cs**
  ```csharp
  // PivExamples/Results/ResetResult.cs
  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  /// <summary>
  /// Result of a PIV application reset.
  /// </summary>
  public sealed record ResetResult
  {
      public bool Success { get; init; }
      public string? ErrorMessage { get; init; }

      public static ResetResult Succeeded() =>
          new() { Success = true };

      public static ResetResult Failed(string error) =>
          new() { Success = false, ErrorMessage = error };
  }
  ```

- [ ] 1.12: **Build verification**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```
  Must exit 0.

- [ ] 1.13: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/
  git add Yubico.YubiKit.Piv/examples/PivTool/Cli/
  git commit -m "feat(PivTool): add result types and directory structure

  Create PivExamples/Results/ with immutable result records:
  - SigningResult, DecryptionResult, VerificationResult
  - CertificateResult, KeyGenerationResult, AttestationResult
  - DeviceInfoResult, SlotInfoResult, PinOperationResult, ResetResult

  Create Cli/ directory structure for upcoming UI code migration."
  ```

→ After Phase 1 commit, continue to Phase 2 if time permits.

---

## Phase 2: Create SDK Example Classes - Crypto Operations (Priority: P0)

**Goal:** Extract SDK signing, decryption, and verification logic into pure SDK example classes.

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Signing.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Decryption.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Verification.cs`

### Tasks

- [ ] 2.1: **Create Signing.cs**
  Extract signing logic from `Features/Crypto.cs:SignDataAsync`. NO Spectre.Console dependencies.
  
  ```csharp
  // PivExamples/Signing.cs
  using System.Diagnostics;
  using System.Security.Cryptography;
  using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

  /// <summary>
  /// Demonstrates PIV signing operations using the YubiKey.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides examples of signing data with private keys stored in PIV slots.
  /// Keys must be generated or imported into the slot before signing.
  /// </para>
  /// <para>
  /// PIN verification may be required depending on the slot's PIN policy.
  /// Touch may be required depending on the slot's touch policy.
  /// </para>
  /// </remarks>
  public static class Signing
  {
      /// <summary>
      /// Signs data using the private key in a PIV slot.
      /// </summary>
      /// <param name="session">An authenticated PIV session with PIN already verified if required.</param>
      /// <param name="slot">The slot containing the signing key.</param>
      /// <param name="dataToSign">The data to sign (will be hashed).</param>
      /// <param name="hashAlgorithm">Hash algorithm to use (SHA256, SHA384, or SHA512).</param>
      /// <param name="onTouchRequired">Optional callback invoked when touch is needed.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Result containing signature or error information.</returns>
      /// <example>
      /// <code>
      /// await using var session = await device.CreatePivSessionAsync(ct);
      /// await session.VerifyPinAsync(pin, ct);
      /// 
      /// var result = await Signing.SignDataAsync(
      ///     session, 
      ///     PivSlot.Signature, 
      ///     dataBytes,
      ///     HashAlgorithmName.SHA256,
      ///     () => Console.WriteLine("Touch required"),
      ///     ct);
      /// 
      /// if (result.Success)
      /// {
      ///     var signature = result.Signature;
      /// }
      /// </code>
      /// </example>
      public static async Task<SigningResult> SignDataAsync(
          IPivSession session,
          PivSlot slot,
          ReadOnlyMemory<byte> dataToSign,
          HashAlgorithmName hashAlgorithm,
          Action? onTouchRequired = null,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              // Check if slot has a key
              var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
              if (metadata is null)
              {
                  return SigningResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
              }

              // Set up touch callback
              if (onTouchRequired is not null)
              {
                  session.OnTouchRequired = onTouchRequired;
              }

              var stopwatch = Stopwatch.StartNew();

              // Hash the data
              byte[] hash = hashAlgorithm.Name switch
              {
                  "SHA256" => SHA256.HashData(dataToSign.Span),
                  "SHA384" => SHA384.HashData(dataToSign.Span),
                  "SHA512" => SHA512.HashData(dataToSign.Span),
                  _ => SHA256.HashData(dataToSign.Span)
              };

              // Sign using simplified API (auto-detects algorithm from slot metadata)
              var signature = await session.SignOrDecryptAsync(slot, hash, cancellationToken);

              stopwatch.Stop();
              return SigningResult.Succeeded(signature, stopwatch.ElapsedMilliseconds);
          }
          catch (OperationCanceledException)
          {
              return SigningResult.Failed("Touch timeout. Please touch the YubiKey when prompted.");
          }
          catch (Exception ex)
          {
              return SigningResult.Failed($"Signing failed: {ex.Message}");
          }
      }
  }
  ```

- [ ] 2.2: **Create Decryption.cs**
  Extract decryption logic from `Features/Crypto.cs:DecryptDataAsync`. NO Spectre.Console dependencies.

  ```csharp
  // PivExamples/Decryption.cs
  using System.Diagnostics;
  using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

  /// <summary>
  /// Demonstrates PIV RSA decryption operations using the YubiKey.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides examples of decrypting data with RSA private keys stored in PIV slots.
  /// Only RSA keys support decryption; ECC keys cannot be used for this operation.
  /// </para>
  /// </remarks>
  public static class Decryption
  {
      /// <summary>
      /// Decrypts data using the RSA private key in a PIV slot.
      /// </summary>
      /// <param name="session">An authenticated PIV session with PIN already verified if required.</param>
      /// <param name="slot">The slot containing the RSA decryption key.</param>
      /// <param name="encryptedData">The data encrypted with the corresponding public key.</param>
      /// <param name="onTouchRequired">Optional callback invoked when touch is needed.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Result containing decrypted data or error information.</returns>
      /// <example>
      /// <code>
      /// await using var session = await device.CreatePivSessionAsync(ct);
      /// await session.VerifyPinAsync(pin, ct);
      /// 
      /// var result = await Decryption.DecryptDataAsync(
      ///     session,
      ///     PivSlot.KeyManagement,
      ///     encryptedBytes,
      ///     () => Console.WriteLine("Touch required"),
      ///     ct);
      /// 
      /// if (result.Success)
      /// {
      ///     var plaintext = result.DecryptedData;
      /// }
      /// </code>
      /// </example>
      public static async Task<DecryptionResult> DecryptDataAsync(
          IPivSession session,
          PivSlot slot,
          ReadOnlyMemory<byte> encryptedData,
          Action? onTouchRequired = null,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              // Check if slot has an RSA key
              var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
              if (metadata is null)
              {
                  return DecryptionResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
              }

              if (!metadata.Value.Algorithm.IsRsa())
              {
                  return DecryptionResult.Failed("Decryption requires an RSA key. Selected slot has an ECC key.");
              }

              // Set up touch callback
              if (onTouchRequired is not null)
              {
                  session.OnTouchRequired = onTouchRequired;
              }

              var stopwatch = Stopwatch.StartNew();

              // Decrypt using simplified API
              var decrypted = await session.SignOrDecryptAsync(slot, encryptedData.ToArray(), cancellationToken);

              stopwatch.Stop();
              return DecryptionResult.Succeeded(decrypted, stopwatch.ElapsedMilliseconds);
          }
          catch (OperationCanceledException)
          {
              return DecryptionResult.Failed("Touch timeout. Please touch the YubiKey when prompted.");
          }
          catch (Exception ex)
          {
              return DecryptionResult.Failed($"Decryption failed: {ex.Message}");
          }
      }
  }
  ```

- [ ] 2.3: **Create Verification.cs**
  Extract verification logic from `Features/Crypto.cs:VerifySignatureAsync`. NO Spectre.Console dependencies.

  ```csharp
  // PivExamples/Verification.cs
  using System.Diagnostics;
  using System.Security.Cryptography;
  using System.Security.Cryptography.X509Certificates;
  using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

  /// <summary>
  /// Demonstrates signature verification using certificates from PIV slots.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Signature verification uses the public key from a certificate stored in a PIV slot.
  /// This operation does not require PIN or touch as it only uses the public key.
  /// </para>
  /// </remarks>
  public static class Verification
  {
      /// <summary>
      /// Verifies a signature using the certificate in a PIV slot.
      /// </summary>
      /// <param name="session">A PIV session (does not need PIN verification).</param>
      /// <param name="slot">The slot containing the certificate with public key.</param>
      /// <param name="originalData">The original data that was signed.</param>
      /// <param name="signature">The signature to verify.</param>
      /// <param name="hashAlgorithm">Hash algorithm used during signing.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Result indicating whether signature is valid.</returns>
      /// <example>
      /// <code>
      /// await using var session = await device.CreatePivSessionAsync(ct);
      /// 
      /// var result = await Verification.VerifySignatureAsync(
      ///     session,
      ///     PivSlot.Signature,
      ///     originalData,
      ///     signatureBytes,
      ///     HashAlgorithmName.SHA256,
      ///     ct);
      /// 
      /// if (result.Success &amp;&amp; result.IsValid)
      /// {
      ///     Console.WriteLine("Signature is valid!");
      /// }
      /// </code>
      /// </example>
      public static async Task<VerificationResult> VerifySignatureAsync(
          IPivSession session,
          PivSlot slot,
          ReadOnlyMemory<byte> originalData,
          ReadOnlyMemory<byte> signature,
          HashAlgorithmName hashAlgorithm,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              var cert = await session.GetCertificateAsync(slot, cancellationToken);
              if (cert is null)
              {
                  return VerificationResult.Failed($"Slot {slot} has no certificate.");
              }

              var stopwatch = Stopwatch.StartNew();
              var isValid = false;

              var rsaKey = cert.GetRSAPublicKey();
              if (rsaKey is not null)
              {
                  isValid = rsaKey.VerifyData(
                      originalData.Span,
                      signature.Span,
                      hashAlgorithm,
                      RSASignaturePadding.Pkcs1);
              }
              else
              {
                  var ecdsaKey = cert.GetECDsaPublicKey();
                  if (ecdsaKey is not null)
                  {
                      isValid = ecdsaKey.VerifyData(
                          originalData.Span,
                          signature.Span,
                          hashAlgorithm);
                  }
              }

              stopwatch.Stop();
              return isValid
                  ? VerificationResult.Valid(stopwatch.ElapsedMilliseconds)
                  : VerificationResult.Invalid(stopwatch.ElapsedMilliseconds);
          }
          catch (Exception ex)
          {
              return VerificationResult.Failed($"Verification failed: {ex.Message}");
          }
      }
  }
  ```

- [ ] 2.4: **Build verification**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```
  Must exit 0.

- [ ] 2.5: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Signing.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Decryption.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Verification.cs
  git commit -m "feat(PivTool): add SDK crypto operation examples

  Extract pure SDK code from Features/Crypto.cs:
  - Signing.cs: SignDataAsync with hash algorithm support
  - Decryption.cs: DecryptDataAsync for RSA keys
  - Verification.cs: VerifySignatureAsync using slot certificates

  All classes are static, have no Spectre.Console dependencies,
  return result types, and include XML documentation with examples."
  ```

→ After Phase 2 commit, continue to Phase 3 if time permits.

---

## Phase 3: Create SDK Example Classes - Certificate Operations (Priority: P0)

**Goal:** Extract certificate management SDK logic.

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Certificates.cs`

### Tasks

- [ ] 3.1: **Create Certificates.cs**
  Extract certificate operations from `Features/Certificates.cs`. NO Spectre.Console dependencies.

  ```csharp
  // PivExamples/Certificates.cs
  using System.Security.Cryptography;
  using System.Security.Cryptography.X509Certificates;
  using System.Text;
  using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

  namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

  /// <summary>
  /// Demonstrates PIV certificate operations using the YubiKey.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class provides examples for managing certificates in PIV slots:
  /// reading, importing, exporting, deleting, and generating self-signed certificates.
  /// </para>
  /// <para>
  /// Most write operations require management key authentication.
  /// Certificate generation requires PIN if the slot's PIN policy requires it.
  /// </para>
  /// </remarks>
  public static class Certificates
  {
      /// <summary>
      /// Gets the certificate from a PIV slot.
      /// </summary>
      public static async Task<CertificateResult> GetCertificateAsync(
          IPivSession session,
          PivSlot slot,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              var cert = await session.GetCertificateAsync(slot, cancellationToken);
              return cert is not null
                  ? CertificateResult.Succeeded(cert)
                  : CertificateResult.Failed($"Slot {slot} is empty.");
          }
          catch (Exception ex)
          {
              return CertificateResult.Failed($"Failed to read certificate: {ex.Message}");
          }
      }

      /// <summary>
      /// Imports a certificate from PEM or DER format.
      /// </summary>
      /// <param name="session">An authenticated PIV session (management key verified).</param>
      /// <param name="slot">The target slot.</param>
      /// <param name="certificateData">Certificate data in PEM or DER format.</param>
      /// <param name="compress">Whether to compress the certificate data.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      public static async Task<CertificateResult> ImportCertificateAsync(
          IPivSession session,
          PivSlot slot,
          ReadOnlyMemory<byte> certificateData,
          bool compress = false,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              X509Certificate2? cert = null;
              var text = Encoding.UTF8.GetString(certificateData.Span);

              if (text.Contains("-----BEGIN CERTIFICATE-----"))
              {
                  cert = X509Certificate2.CreateFromPem(text);
              }
              else
              {
                  cert = X509CertificateLoader.LoadCertificate(certificateData.Span);
              }

              if (cert is null)
              {
                  return CertificateResult.Failed("Certificate format not recognized. Expected PEM or DER.");
              }

              await session.StoreCertificateAsync(slot, cert, compress, cancellationToken);
              return CertificateResult.Stored();
          }
          catch (CryptographicException)
          {
              return CertificateResult.Failed("Certificate format not recognized. Expected PEM or DER.");
          }
          catch (Exception ex)
          {
              return CertificateResult.Failed($"Import failed: {ex.Message}");
          }
      }

      /// <summary>
      /// Exports a certificate in PEM or DER format.
      /// </summary>
      public static async Task<(CertificateResult Result, byte[]? Data, string? Pem)> ExportCertificateAsync(
          IPivSession session,
          PivSlot slot,
          bool asPem,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              var cert = await session.GetCertificateAsync(slot, cancellationToken);
              if (cert is null)
              {
                  return (CertificateResult.Failed($"Slot {slot} is empty."), null, null);
              }

              if (asPem)
              {
                  var pem = cert.ExportCertificatePem();
                  return (CertificateResult.Succeeded(cert), null, pem);
              }
              else
              {
                  return (CertificateResult.Succeeded(cert), cert.RawData, null);
              }
          }
          catch (Exception ex)
          {
              return (CertificateResult.Failed($"Export failed: {ex.Message}"), null, null);
          }
      }

      /// <summary>
      /// Deletes a certificate from a slot.
      /// </summary>
      /// <param name="session">An authenticated PIV session (management key verified).</param>
      public static async Task<CertificateResult> DeleteCertificateAsync(
          IPivSession session,
          PivSlot slot,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              await session.DeleteCertificateAsync(slot, cancellationToken);
              return CertificateResult.Deleted();
          }
          catch (Exception ex)
          {
              return CertificateResult.Failed($"Delete failed: {ex.Message}");
          }
      }

      /// <summary>
      /// Generates a self-signed certificate for an existing key.
      /// </summary>
      /// <param name="session">An authenticated PIV session (management key + PIN if required).</param>
      /// <param name="slot">The slot containing the key.</param>
      /// <param name="subject">Certificate subject (e.g., "CN=Test User").</param>
      /// <param name="validityDays">Number of days the certificate is valid.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      public static async Task<CertificateResult> GenerateSelfSignedAsync(
          IPivSession session,
          PivSlot slot,
          string subject,
          int validityDays,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
              if (metadata is null)
              {
                  return CertificateResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
              }

              var slotMetadata = metadata.Value;
              using var publicKey = slotMetadata.Algorithm.IsRsa()
                  ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                  : slotMetadata.GetECDsaPublicKey();

              if (publicKey is null)
              {
                  return CertificateResult.Failed("Failed to extract public key from slot metadata.");
              }

              var subjectName = new X500DistinguishedName(subject);
              var hashAlgorithm = slotMetadata.Algorithm == PivAlgorithm.EccP384
                  ? HashAlgorithmName.SHA384
                  : HashAlgorithmName.SHA256;

              X509Certificate2? newCert = null;

              if (publicKey is RSA rsa)
              {
                  var request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                  newCert = request.CreateSelfSigned(
                      DateTimeOffset.UtcNow,
                      DateTimeOffset.UtcNow.AddDays(validityDays));
              }
              else if (publicKey is ECDsa ecdsa)
              {
                  var request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                  newCert = request.CreateSelfSigned(
                      DateTimeOffset.UtcNow,
                      DateTimeOffset.UtcNow.AddDays(validityDays));
              }

              if (newCert is null)
              {
                  return CertificateResult.Failed("Failed to generate certificate.");
              }

              await session.StoreCertificateAsync(slot, newCert, false, cancellationToken);
              return CertificateResult.Succeeded(newCert);
          }
          catch (Exception ex)
          {
              return CertificateResult.Failed($"Generation failed: {ex.Message}");
          }
      }

      /// <summary>
      /// Generates a Certificate Signing Request (CSR).
      /// </summary>
      public static async Task<CertificateResult> GenerateCsrAsync(
          IPivSession session,
          PivSlot slot,
          string subject,
          CancellationToken cancellationToken = default)
      {
          ArgumentNullException.ThrowIfNull(session);

          try
          {
              var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
              if (metadata is null)
              {
                  return CertificateResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
              }

              var slotMetadata = metadata.Value;
              using var publicKey = slotMetadata.Algorithm.IsRsa()
                  ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                  : slotMetadata.GetECDsaPublicKey();

              if (publicKey is null)
              {
                  return CertificateResult.Failed("Failed to extract public key from slot metadata.");
              }

              var subjectName = new X500DistinguishedName(subject);
              var hashAlgorithm = slotMetadata.Algorithm == PivAlgorithm.EccP384
                  ? HashAlgorithmName.SHA384
                  : HashAlgorithmName.SHA256;

              string? csr = null;

              if (publicKey is RSA rsa)
              {
                  var request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                  csr = request.CreateSigningRequestPem();
              }
              else if (publicKey is ECDsa ecdsa)
              {
                  var request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                  csr = request.CreateSigningRequestPem();
              }

              return csr is not null
                  ? CertificateResult.CsrGenerated(csr)
                  : CertificateResult.Failed("Failed to generate CSR.");
          }
          catch (Exception ex)
          {
              return CertificateResult.Failed($"CSR generation failed: {ex.Message}");
          }
      }
  }
  ```

- [ ] 3.2: **Build verification**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```

- [ ] 3.3: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Certificates.cs
  git commit -m "feat(PivTool): add SDK certificate operation examples

  Extract pure SDK code from Features/Certificates.cs:
  - GetCertificateAsync: read certificate from slot
  - ImportCertificateAsync: import PEM/DER certificate
  - ExportCertificateAsync: export in PEM or DER format
  - DeleteCertificateAsync: remove certificate from slot
  - GenerateSelfSignedAsync: create self-signed certificate
  - GenerateCsrAsync: generate Certificate Signing Request

  No Spectre.Console dependencies, rich XML documentation."
  ```

→ After Phase 3 commit, continue to Phase 4 if time permits.

---

## Phase 4: Create Remaining SDK Example Classes (Priority: P0)

**Goal:** Create KeyGeneration, PinManagement, Attestation, DeviceInfo, SlotInfo, and Reset SDK examples.

**Files:**
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/KeyGeneration.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/PinManagement.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Attestation.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/DeviceInfo.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/SlotInfo.cs`
- Create: `Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Reset.cs`

### Tasks

- [ ] 4.1: **Create KeyGeneration.cs**
  Extract from `Features/KeyGeneration.cs`. Static class with `GenerateKeyAsync` method.

- [ ] 4.2: **Create PinManagement.cs**
  Extract from `Features/PinManagement.cs`. Methods for:
  - `VerifyPinAsync`
  - `ChangePinAsync`
  - `ChangePukAsync`
  - `ResetPinWithPukAsync`
  - `SetManagementKeyAsync`

- [ ] 4.3: **Create Attestation.cs**
  Extract from `Features/Attestation.cs`. Methods for:
  - `GetAttestationAsync`
  - `GetAttestationIntermediateAsync`

- [ ] 4.4: **Create DeviceInfo.cs**
  Extract from `Features/DeviceInfo.cs`. Method for:
  - `GetDeviceInfoAsync`

- [ ] 4.5: **Create SlotInfo.cs**
  Extract from `Features/SlotOverview.cs`. Method for:
  - `GetAllSlotsInfoAsync`

- [ ] 4.6: **Create Reset.cs**
  Extract from `Features/Reset.cs`. Method for:
  - `ResetPivApplicationAsync`

- [ ] 4.7: **Build verification**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```

- [ ] 4.8: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/KeyGeneration.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/PinManagement.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Attestation.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/DeviceInfo.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/SlotInfo.cs
  git add Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Reset.cs
  git commit -m "feat(PivTool): add remaining SDK example classes

  - KeyGeneration.cs: key generation with algorithm/policy selection
  - PinManagement.cs: PIN/PUK/management key operations
  - Attestation.cs: key attestation with certificate chain
  - DeviceInfo.cs: device information and retry counters
  - SlotInfo.cs: slot overview with metadata and certificates
  - Reset.cs: PIV application reset"
  ```

---

## Phase 5: Move Shared/ to Cli/ and Create Menu Classes (Priority: P1)

**Goal:** Reorganize CLI code into Cli/ directory with thin menu wrappers.

**Files:**
- Move: `Shared/*.cs` → `Cli/Prompts/` and `Cli/Output/`
- Create: `Cli/Menus/*.cs` (thin wrappers calling PivExamples)

### Tasks

- [ ] 5.1: **Move OutputHelpers.cs to Cli/Output/**
  ```bash
  git mv Yubico.YubiKit.Piv/examples/PivTool/Shared/OutputHelpers.cs \
         Yubico.YubiKit.Piv/examples/PivTool/Cli/Output/OutputHelpers.cs
  ```
  Update namespace to `Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output`.

- [ ] 5.2: **Move DeviceSelector.cs to Cli/Prompts/**
  ```bash
  git mv Yubico.YubiKit.Piv/examples/PivTool/Shared/DeviceSelector.cs \
         Yubico.YubiKit.Piv/examples/PivTool/Cli/Prompts/DeviceSelector.cs
  ```
  Update namespace.

- [ ] 5.3: **Move PinPrompt.cs to Cli/Prompts/**
  ```bash
  git mv Yubico.YubiKit.Piv/examples/PivTool/Shared/PinPrompt.cs \
         Yubico.YubiKit.Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs
  ```
  Update namespace.

- [ ] 5.4: **Create SlotSelector.cs**
  Extract slot selection logic duplicated across features into `Cli/Prompts/SlotSelector.cs`.

- [ ] 5.5: **Create CryptoMenu.cs**
  Thin wrapper: prompts → calls `Signing`/`Decryption`/`Verification` → displays results.

- [ ] 5.6: **Create CertificatesMenu.cs**
  Thin wrapper calling `Certificates` SDK examples.

- [ ] 5.7: **Create remaining Menu classes**
  - `KeyGenerationMenu.cs`
  - `PinManagementMenu.cs`
  - `AttestationMenu.cs`
  - `DeviceInfoMenu.cs`
  - `SlotOverviewMenu.cs`
  - `ResetMenu.cs`

- [ ] 5.8: **Build verification**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```

- [ ] 5.9: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/examples/PivTool/Cli/
  git commit -m "feat(PivTool): migrate CLI code to Cli/ directory

  Move Shared/ helpers to Cli/:
  - Cli/Output/OutputHelpers.cs
  - Cli/Prompts/DeviceSelector.cs, PinPrompt.cs, SlotSelector.cs

  Create thin menu wrappers in Cli/Menus/:
  - CryptoMenu, CertificatesMenu, KeyGenerationMenu
  - PinManagementMenu, AttestationMenu, DeviceInfoMenu
  - SlotOverviewMenu, ResetMenu

  All menus delegate to PivExamples SDK classes."
  ```

---

## Phase 6: Update Program.cs and Clean Up (Priority: P1)

**Goal:** Update main entry point and remove old directories.

**Files:**
- Modify: `Yubico.YubiKit.Piv/examples/PivTool/Program.cs`
- Delete: `Yubico.YubiKit.Piv/examples/PivTool/Features/`
- Delete: `Yubico.YubiKit.Piv/examples/PivTool/Shared/`

### Tasks

- [ ] 6.1: **Update Program.cs namespaces**
  Replace `Features` imports with `Cli.Menus` imports.

- [ ] 6.2: **Build verification before cleanup**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```

- [ ] 6.3: **Delete old directories**
  ```bash
  rm -rf Yubico.YubiKit.Piv/examples/PivTool/Features
  rm -rf Yubico.YubiKit.Piv/examples/PivTool/Shared
  ```

- [ ] 6.4: **Final build verification**
  ```bash
  dotnet build Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
  ```

- [ ] 6.5: **Commit**
  ```bash
  git add -u Yubico.YubiKit.Piv/examples/PivTool/
  git commit -m "refactor(PivTool): update Program.cs and remove old directories

  - Update namespace imports in Program.cs
  - Remove Features/ directory (code moved to Cli/Menus/)
  - Remove Shared/ directory (code moved to Cli/Prompts/ and Cli/Output/)"
  ```

---

## Phase 7: Update README.md and Final Verification (Priority: P1)

**Goal:** Update documentation and perform final quality checks.

**Files:**
- Modify: `Yubico.YubiKit.Piv/examples/PivTool/README.md`

### Tasks

- [ ] 7.1: **Update README.md**
  Add section explaining the new structure:
  - `PivExamples/` - Pure SDK usage examples (look here first)
  - `Cli/` - CLI framework code (skip if you just want SDK examples)

- [ ] 7.2: **Quality criteria verification**
  ```bash
  # Verify NO Spectre.Console in PivExamples
  grep -r "Spectre.Console" Yubico.YubiKit.Piv/examples/PivTool/PivExamples/
  # Expected: 0 matches

  # Verify all PivExamples have XML docs
  grep -c "/// <summary>" Yubico.YubiKit.Piv/examples/PivTool/PivExamples/*.cs
  # Expected: Multiple matches per file

  # Verify result types are records
  grep -c "sealed record" Yubico.YubiKit.Piv/examples/PivTool/PivExamples/Results/*.cs
  # Expected: 1 per file
  ```

- [ ] 7.3: **Full solution build**
  ```bash
  dotnet build.cs build
  ```

- [ ] 7.4: **Commit**
  ```bash
  git add Yubico.YubiKit.Piv/examples/PivTool/README.md
  git commit -m "docs(PivTool): update README with new directory structure

  Explain the SDK/CLI separation:
  - PivExamples/: Pure SDK usage examples for developers
  - Cli/: Spectre.Console UI code"
  ```

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:**
   ```bash
   dotnet build.cs build
   ```
   Must exit 0 with no NEW errors.

2. **Spectre isolation:**
   ```bash
   grep -r "Spectre.Console" Yubico.YubiKit.Piv/examples/PivTool/PivExamples/
   ```
   Must return 0 matches.

3. **XML documentation present:**
   ```bash
   grep -c "/// <summary>" Yubico.YubiKit.Piv/examples/PivTool/PivExamples/*.cs | grep -v ":0$"
   ```
   All files should have documentation.

4. **Commit history:**
   ```bash
   git log --oneline -10
   ```
   Should show phase commits with conventional format.

Only after ALL pass, output <promise>PIVTOOL_REFACTOR_COMPLETE</promise>.
If any fail, fix and re-verify.

---

## On Failure

- If build fails: fix errors in the failing phase, re-run build
- If Spectre found in PivExamples: remove the dependency, fix imports
- If tests fail: fix failing tests (this is example code, may not have tests)
- Do NOT output completion until all verification passes

## Time Pressure Protocol

If running low on context or time:
1. Complete current phase fully (verify + commit)
2. Update this progress file with accurate checkbox state
3. Exit WITHOUT completion promise
4. Next iteration will continue from where you stopped

FORBIDDEN behaviors:
- Skipping phases due to time constraints
- Emitting completion promise with unchecked tasks
- Rushing through multiple phases without verification
