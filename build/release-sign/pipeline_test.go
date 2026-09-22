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

func TestValidateRunValidatesSourceDigestAndRetainsResolvedOsslsigncode(t *testing.T) {
	cfg, runner := validRunConfig(t)
	cfg.SourceDigest = "not-a-commit"
	if _, _, _, _, _, err := validateRun(context.Background(), &cfg, runner); err == nil || !strings.Contains(err.Error(), "source-digest") {
		t.Fatalf("expected source digest error, got %v", err)
	}
	cfg.SourceDigest = strings.Repeat("A", 40)
	if _, _, _, _, _, err := validateRun(context.Background(), &cfg, runner); err != nil {
		t.Fatal(err)
	}
	if !filepath.IsAbs(cfg.Osslsigncode) {
		t.Fatalf("osslsigncode path was not resolved: %q", cfg.Osslsigncode)
	}
	if got := runner.calls[len(runner.calls)-1]; !reflect.DeepEqual(got, []string{cfg.Osslsigncode, "--version"}) {
		t.Fatalf("version command %v", got)
	}
}

func TestOsslsigncodeMinimumVersion(t *testing.T) {
	for _, test := range []struct {
		version string
		valid   bool
	}{
		{"osslsigncode 2.12\n", false},
		{"osslsigncode 2.13\n", true},
		{"osslsigncode 2.14.0\nOpenSSL details\n", true},
		{"osslsigncode 2.14, using:\n\tOpenSSL 3.6.3\n", true},
		{"osslsigncode two.thirteen\n", false},
		{"other 2.14\n", false},
	} {
		t.Run(strings.TrimSpace(test.version), func(t *testing.T) {
			err := validateOsslsigncodeVersion([]byte(test.version))
			if (err == nil) != test.valid {
				t.Fatalf("valid=%v, error=%v", test.valid, err)
			}
		})
	}
}

func TestDefaultOsslsigncodeUsesEnvironmentOrPathName(t *testing.T) {
	t.Setenv("RELEASE_SIGN_OSSLSIGNCODE", "")
	if got := defaultOsslsigncode(); got != "osslsigncode" {
		t.Fatalf("default %q", got)
	}
	t.Setenv("RELEASE_SIGN_OSSLSIGNCODE", "/tools/osslsigncode")
	if got := defaultOsslsigncode(); got != "/tools/osslsigncode" {
		t.Fatalf("environment value %q", got)
	}
}

func TestValidateRunRejectsAnyExistingSignedDirectoryWithoutClean(t *testing.T) {
	cfg, runner := validRunConfig(t)
	if err := os.Mkdir(filepath.Join(cfg.WorkingDirectory, "signed"), 0o700); err != nil {
		t.Fatal(err)
	}
	if _, _, _, _, _, err := validateRun(context.Background(), &cfg, runner); err == nil || !strings.Contains(err.Error(), "--clean") {
		t.Fatalf("expected existing signed directory error, got %v", err)
	}
	cfg.Clean = true
	if _, _, _, _, _, err := validateRun(context.Background(), &cfg, runner); err != nil {
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

func TestRunRejectsOldOsslsigncodeBeforeSignerAcquisition(t *testing.T) {
	cfg, runner := validRunConfig(t)
	runner.versionOutput = []byte("osslsigncode 2.12\n")
	called := false
	err := run(context.Background(), cfg, runner, func(context.Context, string, []*x509.Certificate, *x509.CertPool) (*signerHandle, error) {
		called = true
		return nil, errors.New("must not be reached")
	})
	if err == nil || !strings.Contains(err.Error(), "2.13") || called {
		t.Fatalf("run err=%v signer called=%v", err, called)
	}
}

func TestOsslsigncodeVerifiesSortedSelectedAssemblyBytesWithExactArguments(t *testing.T) {
	_, certificate := testIdentity(t)
	packagePath := writeZip(t, t.TempDir(), "signed package.nupkg", []zipItem{{"lib/b.dll", []byte("second assembly")}, {"lib/a.dll", []byte("first assembly")}})
	var extracted [][]byte
	runner := &fakeRunner{run: func(call []string) ([]byte, error) {
		body, err := os.ReadFile(call[3])
		extracted = append(extracted, body)
		return []byte(validOsslsigncodeEvidence), err
	}}
	cfg := runConfig{Osslsigncode: "/absolute/osslsigncode", RootPath: "root with spaces.pem", TimestampRootPath: "timestamp root.pem"}
	err := verifyAssembliesWithOsslsigncode(context.Background(), runner, cfg, packagePath, map[string]struct{}{"lib/b.dll": {}, "lib/a.dll": {}}, certificate)
	if err != nil {
		t.Fatal(err)
	}
	if len(runner.calls) != 2 {
		t.Fatalf("commands %v, want exactly two", runner.calls)
	}
	for i, body := range [][]byte{[]byte("first assembly"), []byte("second assembly")} {
		call := runner.calls[i]
		want := []string{"/absolute/osslsigncode", "verify", "-in", call[3], "-CAfile", "root with spaces.pem", "-TSA-CAfile", "timestamp root.pem", "-require-leaf-hash", "sha256:" + certificateFingerprint(certificate), "-ignore-cdp", "-ignore-crl"}
		if !reflect.DeepEqual(call, want) {
			t.Fatalf("command %v, want %v", call, want)
		}
		if string(extracted[i]) != string(body) {
			t.Fatalf("extracted %q, want %q", extracted[i], body)
		}
		if _, err := os.Stat(call[3]); !errors.Is(err, os.ErrNotExist) {
			t.Fatalf("temporary assembly remains: %v", err)
		}
		if filepath.Dir(filepath.Dir(call[3])) != filepath.Dir(packagePath) {
			t.Fatalf("temporary directory %q is not beside staged package %q", filepath.Dir(call[3]), packagePath)
		}
	}
}

func TestOsslsigncodeRequiresSuccessfulCompleteEvidence(t *testing.T) {
	_, certificate := testIdentity(t)
	packagePath := writeZip(t, t.TempDir(), "signed.nupkg", []zipItem{{"lib/a.dll", []byte("assembly")}})
	cfg := runConfig{Osslsigncode: "/absolute/osslsigncode", RootPath: "root.pem", TimestampRootPath: "timestamp.pem"}
	selected := map[string]struct{}{"lib/a.dll": {}}

	for name, test := range map[string]struct {
		output string
		err    error
	}{
		"empty":             {},
		"missing digest":    {output: strings.Replace(validOsslsigncodeEvidence, "Message digest algorithm  : SHA256\n", "", 1)},
		"missing leaf":      {output: strings.Replace(validOsslsigncodeEvidence, "Leaf hash match: ok\n", "", 1)},
		"missing timestamp": {output: strings.Replace(validOsslsigncodeEvidence, "Timestamp Server Signature verification: ok\n", "", 1)},
		"missing signature": {output: strings.Replace(validOsslsigncodeEvidence, "Signature verification: ok\n", "", 1)},
		"missing count":     {output: strings.Replace(validOsslsigncodeEvidence, "Number of verified signatures: 1\n", "", 1)},
		"duplicate evidence": {output: validOsslsigncodeEvidence +
			"Signature verification: ok\n"},
		"duplicate count": {output: validOsslsigncodeEvidence +
			"Number of verified signatures: 1\n"},
		"nonzero": {output: validOsslsigncodeEvidence, err: errors.New("exit status 1")},
	} {
		t.Run(name, func(t *testing.T) {
			runner := &fakeRunner{output: []byte(test.output), err: test.err}
			if err := verifyAssembliesWithOsslsigncode(context.Background(), runner, cfg, packagePath, selected, certificate); err == nil {
				t.Fatal("accepted failed or incomplete osslsigncode verification")
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
	packagePath := writeZip(t, stage, "signed.nupkg", []zipItem{{"lib/a.dll", []byte("assembly")}})
	runner := &fakeRunner{err: errors.New("verification failed")}
	if err := verifyAssembliesWithOsslsigncode(context.Background(), runner, runConfig{Osslsigncode: "/osslsigncode"}, packagePath, map[string]struct{}{"lib/a.dll": {}}, &x509.Certificate{}); err == nil {
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
	for _, name := range []string{"gh", "osslsigncode"} {
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
	manifestBody := `{"schema":1,"attestationRepo":"Yubico/Yubico.NET.SDK","signerWorkflow":"Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml","packages":{"Yubico.NativeShims":{"symbols":"absent","authenticode":{"include":["runtimes/win-x64/native/Yubico.NativeShims.dll"],"firstParty":["Yubico.NativeShims.dll"]}}}}`
	if err := os.WriteFile(manifestPath, []byte(manifestBody), 0o600); err != nil {
		t.Fatal(err)
	}
	artifact := writeZip(t, work, "artifact.zip", []zipItem{{"a.nupkg", []byte("package")}})
	cfg := runConfig{Component: "nativeshims", WorkingDirectory: work, Artifacts: []string{artifact}, ManifestPath: manifestPath, KeyLocation: "test://key", CertificatePath: certificatePath, RootPath: rootPath, TimestampRootPath: rootPath, Osslsigncode: "osslsigncode", Timestamper: defaultTimestamper, SourceDigest: strings.Repeat("a", 40)}
	return cfg, &fakeRunner{versionOutput: []byte("osslsigncode 2.14\n")}
}

const validOsslsigncodeEvidence = "Message digest algorithm  : SHA256\nLeaf hash match: ok\nTimestamp Server Signature verification: ok\nSignature verification: ok\nNumber of verified signatures: 1\n"

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
