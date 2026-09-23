package main

import (
	"context"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/json"
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

func TestExecutableResolutionAndMinimumVersions(t *testing.T) {
	for _, test := range []struct {
		output string
		valid  bool
	}{
		{"nuget-sign version 0.0.9\n", false},
		{"nuget-sign version 0.1.0\n", true},
		{"nuget-sign version 1.2.3\n", true},
		{"other version 1.2.3\n", false},
	} {
		t.Run(test.output, func(t *testing.T) {
			err := validateNugetSignVersion([]byte(test.output))
			if (err == nil) != test.valid {
				t.Fatalf("valid=%v err=%v", test.valid, err)
			}
		})
	}
	bin := t.TempDir()
	if err := os.WriteFile(filepath.Join(bin, "nuget-sign"), []byte("nuget-sign binary"), 0o700); err != nil {
		t.Fatal(err)
	}
	t.Setenv("PATH", bin+string(os.PathListSeparator)+os.Getenv("PATH"))
	runner := &fakeRunner{output: []byte("nuget-sign version 0.1.0\n")}
	metadata, err := inspectNugetSign(context.Background(), runner, "nuget-sign")
	if err != nil {
		t.Fatal(err)
	}
	tool := filepath.Join(bin, "nuget-sign")
	if metadata.Path != tool || metadata.Version != "nuget-sign version 0.1.0" || metadata.SHA256 == "" {
		t.Fatalf("metadata %#v", metadata)
	}
	if call := runner.calls[0]; !reflect.DeepEqual(call, []string{tool, "--version"}) {
		t.Fatalf("call %v", call)
	}
}

func TestExactNugetSignAndVerifyCommands(t *testing.T) {
	dir := t.TempDir()
	input := writeZip(t, dir, "input.nupkg", []zipItem{{"a.nuspec", []byte(`<package><metadata><id>A</id><version>1</version></metadata></package>`)}, {"lib/b.dll", minimalPE()}, {"lib/a.dll", minimalPE()}, {"data", []byte("same")}})
	info := packageInfo{Path: input, Name: "A.1.nupkg", Kind: "nupkg"}
	selected := map[string]struct{}{"lib/b.dll": {}, "lib/a.dll": {}}
	output := filepath.Join(dir, "output.nupkg")
	cfg := runConfig{NugetSign: "/tools/nuget-sign", KeyLocation: "yubikey://9c", CertificatePath: "chain.pem", RootPath: "root.pem", TimestampRootPath: "timestamp.pem", Timestamper: "https://timestamp"}
	runner := &fakeRunner{}
	runner.attachedRun = func(call []string) error {
		if call[1] == "sign-assemblies" {
			for _, filename := range call[10:] {
				body, err := os.ReadFile(filename)
				if err != nil {
					return err
				}
				if err := os.WriteFile(filename, fakeSignedPE(t, body), 0o600); err != nil {
					return err
				}
			}
			return nil
		}
		addSignature(t, call[len(call)-1], call[11])
		return nil
	}
	if err := signPackage(context.Background(), runner, cfg, info, output, selected); err != nil {
		t.Fatal(err)
	}
	if len(runner.attached) != 2 {
		t.Fatalf("attached calls %v", runner.attached)
	}
	assemblyCall := runner.attached[0]
	wantAssemblyPrefix := []string{"/tools/nuget-sign", "sign-assemblies", "--key", "yubikey://9c", "--certificate", "chain.pem", "--hash-algorithm", "sha256", "--timestamper", "https://timestamp"}
	if !reflect.DeepEqual(assemblyCall[:10], wantAssemblyPrefix) || !strings.HasSuffix(assemblyCall[10], filepath.FromSlash("lib/a.dll")) || !strings.HasSuffix(assemblyCall[11], filepath.FromSlash("lib/b.dll")) {
		t.Fatalf("assembly command %v", assemblyCall)
	}
	packageCall := runner.attached[1]
	if want := []string{"/tools/nuget-sign", "sign", "--key", "yubikey://9c", "--certificate", "chain.pem", "--hash-algorithm", "sha256", "--timestamper", "https://timestamp", "--output", output, packageCall[12]}; !reflect.DeepEqual(packageCall, want) {
		t.Fatalf("package command %v, want %v", packageCall, want)
	}
	fingerprint := strings.Repeat("A", 64)
	runner.run = func(call []string) ([]byte, error) {
		return []byte("lib/a.dll  SHA-256, signer, digest matches\nlib/b.dll  SHA-256, signer, digest matches\n"), nil
	}
	if err := verifySignedPackage(context.Background(), runner, cfg, info, output, selected, fingerprint); err != nil {
		t.Fatal(err)
	}
	wantVerify := []string{"/tools/nuget-sign", "verify", "--revocation", "none", "--root", "root.pem", "--timestamp-root", "timestamp.pem", "--certificate-fingerprint", fingerprint, "--assemblies", output}
	if !reflect.DeepEqual(runner.calls[0], wantVerify) {
		t.Fatalf("verify command %v", runner.calls[0])
	}
	if len(runner.calls) != 1 {
		t.Fatalf("verification calls %v", runner.calls)
	}
	symbolInput := writeZip(t, dir, "input.snupkg", []zipItem{{"a.nuspec", []byte("same")}, {"a.pdb", []byte("symbols")}})
	symbolOutput := addSignature(t, symbolInput, filepath.Join(dir, "output.snupkg"))
	runner.calls = nil
	runner.output = nil
	runner.run = nil
	if err := verifySignedPackage(context.Background(), runner, cfg, packageInfo{Path: symbolInput, Kind: "snupkg"}, symbolOutput, nil, fingerprint); err != nil {
		t.Fatal(err)
	}
	wantSymbolVerify := []string{"/tools/nuget-sign", "verify", "--revocation", "none", "--root", "root.pem", "--timestamp-root", "timestamp.pem", "--certificate-fingerprint", fingerprint, symbolOutput}
	if !reflect.DeepEqual(runner.calls, [][]string{wantSymbolVerify}) {
		t.Fatalf("symbol verify commands %v", runner.calls)
	}
}

func TestSelectedAssembliesRequireSuccessfulVerification(t *testing.T) {
	for _, test := range []struct {
		name, output string
		valid        bool
	}{
		{"verified", "    lib/a.dll  SHA-256, signer, digest matches\n", true},
		{"missing", "assemblies  1, 1 verified\n", false},
		{"unsigned", "    lib/a.dll  unsigned\n", false},
		{"misleading summary", "assemblies  1, 1 verified\n    lib/a.dll  unsigned\n", false},
		{"modified", "    lib/a.dll  DIGEST DIFFERS\n", false},
		{"different name", "    lib/a.dll-other  SHA-256, signer, digest matches\n", false},
	} {
		t.Run(test.name, func(t *testing.T) {
			err := requireVerifiedAssemblies([]byte(test.output), []string{"lib/a.dll"})
			if (err == nil) != test.valid {
				t.Fatalf("valid=%v err=%v", test.valid, err)
			}
		})
	}
}

func TestAtomicOutputReportOrderingMetadataAndNoSecrets(t *testing.T) {
	dir := t.TempDir()
	stage := filepath.Join(dir, "stage")
	if err := os.Mkdir(stage, 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(stage, "a.nupkg"), []byte("a"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := commitOutputs(filepath.Join(dir, "signed"), stage, []string{"a.nupkg", "missing.nupkg"}, []byte(`{}`), false); err == nil {
		t.Fatal("expected atomic failure")
	}
	if _, err := os.Stat(filepath.Join(dir, "signed")); !errors.Is(err, os.ErrNotExist) {
		t.Fatalf("partial output: %v", err)
	}

	r := report{Schema: 1, KeyLocation: safeKeyLocation("yubikey://9c?pin=123456"), Tool: reportTool{Path: "/tools/nuget-sign", Version: "nuget-sign version 0.1.0", SHA256: "abc"}, Packages: []reportPackage{{ID: "Z"}, {ID: "A"}}, Artifacts: []reportArtifact{{Path: "z"}, {Path: "a"}}}
	b, err := marshalReport(r)
	if err != nil {
		t.Fatal(err)
	}
	if strings.Contains(string(b), "123456") || strings.Contains(string(b), "NUGET_SIGN_PIN") {
		t.Fatal("secret leaked")
	}
	var got report
	if err := json.Unmarshal(b, &got); err != nil {
		t.Fatal(err)
	}
	if got.Packages[0].ID != "A" || got.Artifacts[0].Path != "a" || got.Tool.Path != "/tools/nuget-sign" {
		t.Fatalf("report %s", b)
	}
	if err := os.Mkdir(filepath.Join(dir, "signed"), 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(dir, "signed", "stale"), []byte("keep"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := commitOutputs(filepath.Join(dir, "signed"), stage, []string{"a.nupkg"}, b, false); err == nil {
		t.Fatal("overwrote stale output")
	}
}

type fakeRunner struct {
	output      []byte
	err         error
	calls       [][]string
	attached    [][]string
	run         func([]string) ([]byte, error)
	attachedRun func([]string) error
}

func (f *fakeRunner) Run(_ context.Context, name string, args ...string) ([]byte, error) {
	call := append([]string{name}, args...)
	f.calls = append(f.calls, call)
	if f.run != nil {
		return f.run(call)
	}
	return f.output, f.err
}
func (f *fakeRunner) RunAttached(_ context.Context, name string, args ...string) error {
	call := append([]string{name}, args...)
	f.attached = append(f.attached, call)
	if f.attachedRun != nil {
		return f.attachedRun(call)
	}
	return f.err
}

func testCertificateChain(t *testing.T, dir string) (string, string, *x509.Certificate) {
	t.Helper()
	now := time.Now()
	rootKey, _ := rsa.GenerateKey(rand.Reader, 2048)
	rootTemplate := &x509.Certificate{SerialNumber: big.NewInt(1), Subject: pkix.Name{CommonName: "root"}, NotBefore: now.Add(-time.Hour), NotAfter: now.Add(time.Hour), BasicConstraintsValid: true, IsCA: true, KeyUsage: x509.KeyUsageCertSign}
	rootDER, err := x509.CreateCertificate(rand.Reader, rootTemplate, rootTemplate, &rootKey.PublicKey, rootKey)
	if err != nil {
		t.Fatal(err)
	}
	root, _ := x509.ParseCertificate(rootDER)
	leafKey, _ := rsa.GenerateKey(rand.Reader, 2048)
	leafTemplate := &x509.Certificate{SerialNumber: big.NewInt(2), Subject: pkix.Name{CommonName: "leaf"}, NotBefore: now.Add(-time.Hour), NotAfter: now.Add(time.Hour), KeyUsage: x509.KeyUsageDigitalSignature, ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning}}
	leafDER, err := x509.CreateCertificate(rand.Reader, leafTemplate, root, &leafKey.PublicKey, rootKey)
	if err != nil {
		t.Fatal(err)
	}
	leaf, _ := x509.ParseCertificate(leafDER)
	chainPath, rootPath := filepath.Join(dir, "chain.pem"), filepath.Join(dir, "root.pem")
	if err := os.WriteFile(chainPath, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: leaf.Raw}), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(rootPath, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: root.Raw}), 0o600); err != nil {
		t.Fatal(err)
	}
	return chainPath, rootPath, leaf
}
