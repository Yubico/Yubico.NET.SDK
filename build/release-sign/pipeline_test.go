package main

import (
	"context"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/pem"
	"errors"
	"math/big"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
	"time"
)

func TestValidateRunValidatesSourceDigestAndRetainsResolvedVerifier(t *testing.T) {
	cfg, _ := validRunConfig(t)
	cfg.SourceDigest = "not-a-commit"
	if _, _, _, _, _, err := validateRun(&cfg); err == nil || !strings.Contains(err.Error(), "source-digest") {
		t.Fatalf("expected source digest error, got %v", err)
	}
	cfg.SourceDigest = strings.Repeat("A", 40)
	if _, _, _, _, _, err := validateRun(&cfg); err != nil {
		t.Fatal(err)
	}
	if !filepath.IsAbs(cfg.IndependentVerifier) {
		t.Fatalf("verifier path was not resolved: %q", cfg.IndependentVerifier)
	}
}

func TestValidateRunRejectsAnyExistingSignedDirectoryWithoutClean(t *testing.T) {
	cfg, _ := validRunConfig(t)
	if err := os.Mkdir(filepath.Join(cfg.WorkingDirectory, "signed"), 0o700); err != nil {
		t.Fatal(err)
	}
	if _, _, _, _, _, err := validateRun(&cfg); err == nil || !strings.Contains(err.Error(), "--clean") {
		t.Fatalf("expected existing signed directory error, got %v", err)
	}
	cfg.Clean = true
	if _, _, _, _, _, err := validateRun(&cfg); err != nil {
		t.Fatalf("--clean should permit existing signed directory: %v", err)
	}
	if _, err := os.Stat(filepath.Join(cfg.WorkingDirectory, "signed")); err != nil {
		t.Fatalf("validation removed prior output: %v", err)
	}
}

func TestTrustAnchorPoolRequiresSelfSignedCA(t *testing.T) {
	valid := testTrustAnchor(t)
	validPath := writeCertificateFile(t, valid)
	if _, err := trustAnchorPool(validPath); err != nil {
		t.Fatalf("valid POC-style root rejected: %v", err)
	}

	_, codeSigner := testIdentity(t)
	notSelfIssued := testIssuedCA(t)
	badSelfSignature := testBadSelfSignatureCA(t)
	for _, test := range []struct {
		name string
		cert *x509.Certificate
		want string
	}{
		{"not CA", codeSigner, "CA"},
		{"not self-issued", notSelfIssued, "self-issued"},
		{"bad self-signature", badSelfSignature, "self-signature"},
	} {
		t.Run(test.name, func(t *testing.T) {
			if _, err := trustAnchorPool(writeCertificateFile(t, test.cert)); err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func TestRunAttestationDigestFailurePrecedesSignerAcquisition(t *testing.T) {
	cfg, runner := validRunConfig(t)
	runner.output = []byte(`[{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"}}]}}}]`)
	called := false
	err := run(context.Background(), cfg, runner, func(context.Context, string, []*x509.Certificate, *x509.CertPool) (*signerHandle, error) {
		called = true
		return nil, errors.New("must not be reached")
	})
	if err == nil || called {
		t.Fatalf("run err=%v signer called=%v", err, called)
	}
}

func TestIndependentVerifierGetsResolvedPathAndSignerFingerprint(t *testing.T) {
	_, certificate := testIdentity(t)
	runner := &fakeRunner{output: []byte("PASS lib/a.dll image=SHA-256 timestamp=2026-09-22T00:00:00Z signer=" + certificateFingerprint(certificate) + " tsa=ABC subject=CN=test\n")}
	cfg := runConfig{IndependentVerifier: "/absolute/verifier", RootPath: "root.pem", TimestampRootPath: "timestamp.pem"}
	err := runIndependentVerifier(context.Background(), runner, cfg, "signed.nupkg", map[string]struct{}{"lib/a.dll": {}}, certificate)
	if err != nil {
		t.Fatal(err)
	}
	want := []string{"/absolute/verifier", "--package", "signed.nupkg", "--root", "root.pem", "--timestamp-root", "timestamp.pem", "--signer-fingerprint", certificateFingerprint(certificate), "--assembly", "lib/a.dll"}
	if !reflect.DeepEqual(runner.calls[0], want) {
		t.Fatalf("command %v, want %v", runner.calls[0], want)
	}
}

func TestIndependentVerifierMustConfirmEveryAssembly(t *testing.T) {
	_, certificate := testIdentity(t)
	cfg := runConfig{IndependentVerifier: "/absolute/verifier", RootPath: "root.pem", TimestampRootPath: "timestamp.pem"}
	selected := map[string]struct{}{"lib/a.dll": {}, "lib/b.dll": {}}

	for name, output := range map[string]string{
		"empty":        "",
		"missing":      "PASS lib/a.dll image=SHA-256 signer=" + certificateFingerprint(certificate) + " tsa=ABC\n",
		"wrong signer": "PASS lib/a.dll image=SHA-256 signer=" + strings.Repeat("A", 64) + " tsa=ABC\nPASS lib/b.dll image=SHA-256 signer=" + certificateFingerprint(certificate) + " tsa=ABC\n",
		"unexpected":   "PASS lib/c.dll image=SHA-256 signer=" + certificateFingerprint(certificate) + " tsa=ABC\n",
	} {
		t.Run(name, func(t *testing.T) {
			runner := &fakeRunner{output: []byte(output)}
			if err := runIndependentVerifier(context.Background(), runner, cfg, "signed.nupkg", selected, certificate); err == nil {
				t.Fatal("accepted incomplete independent-verifier evidence")
			}
		})
	}
}

func TestTransientStagingCleanupAfterVerifierFailure(t *testing.T) {
	work := t.TempDir()
	stage, cleanup, err := createRunStaging(work)
	if err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(stage, "signed.nupkg"), []byte("signed"), 0o600); err != nil {
		t.Fatal(err)
	}
	runner := &fakeRunner{err: errors.New("verification failed")}
	if err := runIndependentVerifier(context.Background(), runner, runConfig{IndependentVerifier: "/verifier"}, filepath.Join(stage, "signed.nupkg"), nil, &x509.Certificate{}); err == nil {
		t.Fatal("expected verifier failure")
	}
	cleanup()
	if _, err := os.Stat(stage); !errors.Is(err, os.ErrNotExist) {
		t.Fatalf("transient staging remains: %v", err)
	}
	entries, err := os.ReadDir(filepath.Join(work, "signed", "packages"))
	if err == nil && len(entries) != 0 {
		t.Fatalf("published packages after verifier failure: %v", entries)
	}
}

func TestReadCertificatesRejectsNonCertificatePEMBlock(t *testing.T) {
	filename := filepath.Join(t.TempDir(), "chain.pem")
	if err := os.WriteFile(filename, pem.EncodeToMemory(&pem.Block{Type: "PRIVATE KEY", Bytes: []byte("secret")}), 0o600); err != nil {
		t.Fatal(err)
	}
	if _, err := readCertificates(filename); err == nil || !strings.Contains(err.Error(), "CERTIFICATE") {
		t.Fatalf("expected strict PEM error, got %v", err)
	}
}

func validRunConfig(t *testing.T) (runConfig, *fakeRunner) {
	t.Helper()
	work := t.TempDir()
	bin := t.TempDir()
	for _, name := range []string{"gh", "verifier"} {
		if err := os.WriteFile(filepath.Join(bin, name), []byte("#!/bin/sh\nexit 0\n"), 0o700); err != nil {
			t.Fatal(err)
		}
	}
	t.Setenv("PATH", bin+string(os.PathListSeparator)+os.Getenv("PATH"))
	_, certificate := testIdentity(t)
	root := testTrustAnchor(t)
	certificatePath := filepath.Join(work, "certificate.pem")
	if err := os.WriteFile(certificatePath, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: certificate.Raw}), 0o600); err != nil {
		t.Fatal(err)
	}
	rootPath := writeCertificateFileAt(t, filepath.Join(work, "root.pem"), root)
	manifestPath := filepath.Join(work, "manifest.json")
	manifestBody := `{"schema":1,"attestationRepo":"Yubico/Yubico.NET.SDK","signerWorkflow":"Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml","packages":{"Yubico.NativeShims":{"symbols":"absent","authenticode":{"include":["runtimes/win-x64/native/Yubico.NativeShims.dll"],"firstParty":["Yubico.NativeShims.dll"],"alreadySigned":"reject"}}}}`
	if err := os.WriteFile(manifestPath, []byte(manifestBody), 0o600); err != nil {
		t.Fatal(err)
	}
	artifact := writeZip(t, work, "artifact.zip", []zipItem{{"a.nupkg", []byte("package")}})
	cfg := runConfig{Component: "nativeshims", WorkingDirectory: work, Artifacts: []string{artifact}, ManifestPath: manifestPath, KeyLocation: "test://key", CertificatePath: certificatePath, RootPath: rootPath, TimestampRootPath: rootPath, IndependentVerifier: "verifier", Timestamper: defaultTimestamper, SourceDigest: strings.Repeat("a", 40)}
	return cfg, &fakeRunner{}
}

func testTrustAnchor(t *testing.T) *x509.Certificate {
	t.Helper()
	key, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	template := &x509.Certificate{SerialNumber: big.NewInt(100), Subject: pkix.Name{CommonName: "test root"}, NotBefore: time.Now().Add(-time.Hour), NotAfter: time.Now().Add(time.Hour), BasicConstraintsValid: true, IsCA: true, KeyUsage: x509.KeyUsageCertSign}
	der, err := x509.CreateCertificate(rand.Reader, template, template, &key.PublicKey, key)
	if err != nil {
		t.Fatal(err)
	}
	certificate, err := x509.ParseCertificate(der)
	if err != nil {
		t.Fatal(err)
	}
	return certificate
}

func testIssuedCA(t *testing.T) *x509.Certificate {
	t.Helper()
	issuerKey, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	issuerTemplate := &x509.Certificate{SerialNumber: big.NewInt(103), Subject: pkix.Name{CommonName: "issuer"}, NotBefore: time.Now().Add(-time.Hour), NotAfter: time.Now().Add(time.Hour), BasicConstraintsValid: true, IsCA: true, KeyUsage: x509.KeyUsageCertSign}
	issuerDER, err := x509.CreateCertificate(rand.Reader, issuerTemplate, issuerTemplate, &issuerKey.PublicKey, issuerKey)
	if err != nil {
		t.Fatal(err)
	}
	issuer, err := x509.ParseCertificate(issuerDER)
	if err != nil {
		t.Fatal(err)
	}
	key, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	template := &x509.Certificate{SerialNumber: big.NewInt(101), Subject: pkix.Name{CommonName: "issued CA"}, NotBefore: time.Now().Add(-time.Hour), NotAfter: time.Now().Add(time.Hour), BasicConstraintsValid: true, IsCA: true, KeyUsage: x509.KeyUsageCertSign}
	der, err := x509.CreateCertificate(rand.Reader, template, issuer, &key.PublicKey, issuerKey)
	if err != nil {
		t.Fatal(err)
	}
	certificate, err := x509.ParseCertificate(der)
	if err != nil {
		t.Fatal(err)
	}
	return certificate
}

func testBadSelfSignatureCA(t *testing.T) *x509.Certificate {
	t.Helper()
	publicKey, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	signingKey, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	template := &x509.Certificate{SerialNumber: big.NewInt(102), Subject: pkix.Name{CommonName: "bad root"}, NotBefore: time.Now().Add(-time.Hour), NotAfter: time.Now().Add(time.Hour), BasicConstraintsValid: true, IsCA: true, KeyUsage: x509.KeyUsageCertSign}
	der, err := x509.CreateCertificate(rand.Reader, template, template, &publicKey.PublicKey, signingKey)
	if err != nil {
		t.Fatal(err)
	}
	certificate, err := x509.ParseCertificate(der)
	if err != nil {
		t.Fatal(err)
	}
	return certificate
}

func writeCertificateFile(t *testing.T, certificate *x509.Certificate) string {
	t.Helper()
	return writeCertificateFileAt(t, filepath.Join(t.TempDir(), "certificate.pem"), certificate)
}

func writeCertificateFileAt(t *testing.T, filename string, certificate *x509.Certificate) string {
	t.Helper()
	if err := os.WriteFile(filename, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: certificate.Raw}), 0o600); err != nil {
		t.Fatal(err)
	}
	return filename
}
